#ifndef HALF_LAMBERT_INCLUDED
    #define HALF_LAMBERT_INCLUDED

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/BRDF.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging3D.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GlobalIllumination.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/AmbientOcclusion.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"

    half3 LightingHalfLambert(BRDFData brdfData, BRDFData brdfDataClearCoat,
    half3 lightColor, half3 lightDirectionWS, float lightAttenuation,
    half3 normalWS, half3 viewDirectionWS,
    half clearCoatMask, bool specularHighlightsOff, float lightAttenWShadows, bool additionalLight, UberExtraData extraData)
    {
        half NdotL = saturate(dot(normalWS, lightDirectionWS));
        NdotL = pow(NdotL * 0.5 + 0.5, 2);

        half3 radiance = half3(0,0,0);
        [branch]if(additionalLight)
        {
            radiance = lightColor *  NdotL * lightAttenuation;
        }
        else
        {
            radiance = lightColor * min(pow(lightAttenuation * 0.5 + 0.5, 2), NdotL);
        }
        // half3 radiance = lightColor * pow(lightAttenuation * 0.5 + 0.5, 2) * NdotL;

        half3 brdf = brdfData.diffuse;
        #ifndef _SPECULARHIGHLIGHTS_OFF
            [branch] if (!specularHighlightsOff)
            {
                //MULTIPLY SPECULAR CALC PER SPECULAR SLIDER
                brdf += brdfData.specular * DirectBRDFSpecular(brdfData, normalWS, lightDirectionWS, viewDirectionWS);

                #if defined(_CLEARCOAT) || defined(_CLEARCOATMAP)
                    // Clear coat evaluates the specular a second timw and has some common terms with the base specular.
                    // We rely on the compiler to merge these and compute them only once.
                    half brdfCoat = kDielectricSpec.r * DirectBRDFSpecular(brdfDataClearCoat, normalWS, lightDirectionWS, viewDirectionWS);

                    // Mix clear coat and base layer using khronos glTF recommended formula
                    // https://github.com/KhronosGroup/glTF/blob/master/extensions/2.0/Khronos/KHR_materials_clearcoat/README.md
                    // Use NoV for direct too instead of LoH as an optimization (NoV is light invariant).
                    half NoV = saturate(dot(normalWS, viewDirectionWS));
                    // Use slightly simpler fresnelTerm (Pow4 vs Pow5) as a small optimization.
                    // It is matching fresnel used in the GI/Env, so should produce a consistent clear coat blend (env vs. direct)
                    half coatFresnel = kDielectricSpec.x + kDielectricSpec.a * Pow4(1.0 - NoV);

                    brdf = brdf * (1.0 - clearCoatMask * coatFresnel) + brdfCoat * clearCoatMask;
                #endif // _CLEARCOAT

            }
        #endif // _SPECULARHIGHLIGHTS_OFF

        #if defined(_SUBSURFACE_SCATTER)
            #define ThicknessModifier 0.15

            // Old Method
            float3 H = normalize(lightDirectionWS + normalWS * extraData.sss_distortion);
            float ViewDotH = pow(saturate(dot(viewDirectionWS, -H)), extraData.sss_power) * 1.0;
            half3 SSSIrradianceVD = ViewDotH * (1-extraData.sss_thickness);
            // return (brdf * radiance) + (lightColor * SSSIrradiance * extraData.sss_mask * lightAttenWShadows * extraData.sss_color);

            //Newer Method
            float pNdotL = saturate(NdotL); // positive ndl
            float nNdotL = saturate(-NdotL); // negative ndl
            float thickness = 1-extraData.sss_thickness;
            float3 radius = clamp(thickness - extraData.sss_scale, 0.0, 1.5);
            float3 sss = 1 * pow((float3)1. - pNdotL, 3.0 / (radius + 0.001)) * pow((float3)1.0 - nNdotL, 3. / (radius + 0.001));

            float3 subColor = Saturate(extraData.diffuseTex, 1.5) * extraData.sss_color;
            subColor *= lightColor * lightAttenWShadows;
            float3 SSSIrradiance = subColor * radius * (sss + SSSIrradianceVD) * extraData.sss_mask;

            return (brdf * radiance) + SSSIrradiance;
            // return (brdf * radiance);


            // return extraData.sss_thickness.xxx;
        #endif

        return brdf * radiance;
    }

    half3 LightingHalfLambert(BRDFData brdfData, BRDFData brdfDataClearCoat, Light light, half3 normalWS, half3 viewDirectionWS, half clearCoatMask, bool specularHighlightsOff, bool additionalLight, UberExtraData extraData)
    {
        return LightingHalfLambert(brdfData, brdfDataClearCoat, light.color, light.direction, light.distanceAttenuation * light.shadowAttenuation, normalWS, viewDirectionWS, clearCoatMask, specularHighlightsOff, light.distanceAttenuation, additionalLight,extraData);
    }


    half4 FragmentPBR_HalfLambert(InputData inputData, UberSurfaceData surfaceData, UberExtraData extraData)
    {
        #if defined(_SPECULARHIGHLIGHTS_OFF)
            bool specularHighlightsOff = true;
        #else
            bool specularHighlightsOff = false;
        #endif
        BRDFData brdfData;

        // NOTE: can modify "surfaceData"...
        InitializeBRDFData(surfaceData, brdfData);

        #if defined(DEBUG_DISPLAY)
            half4 debugColor;

            if (CanDebugOverrideOutputColor(inputData, surfaceData, brdfData, debugColor))
            {
                return debugColor;
            }
        #endif

        // Clear-coat calculation...
        BRDFData brdfDataClearCoat = CreateClearCoatBRDFData(surfaceData, brdfData);
        half4 shadowMask = CalculateShadowMask(inputData);
        AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData, surfaceData);
        uint meshRenderingLayers = GetMeshRenderingLayer();
        Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);

        // NOTE: We don't apply AO to the GI here because it's done in the lighting calculation below...
        MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI);

        LightingData lightingData = CreateLightingData(inputData, surfaceData);

        lightingData.giColor = GlobalIllumination(brdfData, brdfDataClearCoat, surfaceData.clearCoatMask,
        inputData.bakedGI, aoFactor.indirectAmbientOcclusion, inputData.positionWS,
        inputData.normalWS, inputData.viewDirectionWS, inputData.normalizedScreenSpaceUV);
        #ifdef _LIGHT_LAYERS
            if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
        #endif
        {
            lightingData.mainLightColor = LightingHalfLambert(brdfData, brdfDataClearCoat,
            mainLight,
            inputData.normalWS, inputData.viewDirectionWS,
            surfaceData.clearCoatMask, specularHighlightsOff, false, extraData);
        }

        #if defined(_ADDITIONAL_LIGHTS)
            uint pixelLightCount = GetAdditionalLightsCount();

            #if USE_CLUSTER_LIGHT_LOOP
                [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                {
                    CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK

                    Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);

                    #ifdef _LIGHT_LAYERS
                        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
                    #endif
                    {
                        lightingData.additionalLightsColor += LightingHalfLambert(brdfData, brdfDataClearCoat, light,
                        inputData.normalWS, inputData.viewDirectionWS,
                        surfaceData.clearCoatMask, specularHighlightsOff, true, extraData);
                    }
                }
            #endif

            LIGHT_LOOP_BEGIN(pixelLightCount)
            Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);

            #ifdef _LIGHT_LAYERS
                if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
            #endif
            {
                lightingData.additionalLightsColor += LightingHalfLambert(brdfData, brdfDataClearCoat, light,
                inputData.normalWS, inputData.viewDirectionWS,
                surfaceData.clearCoatMask, specularHighlightsOff, true, extraData);
            }
            LIGHT_LOOP_END
        #endif

        #if defined(_ADDITIONAL_LIGHTS_VERTEX)
            lightingData.vertexLightingColor += inputData.vertexLighting * brdfData.diffuse;
        #endif

        #if REAL_IS_HALF
            // Clamp any half.inf+ to HALF_MAX
            return min(CalculateFinalColor(lightingData, surfaceData.alpha), HALF_MAX);
        #else
            return CalculateFinalColor(lightingData, surfaceData.alpha);
        #endif

    }



#endif