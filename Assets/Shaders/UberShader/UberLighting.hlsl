#ifndef UBER_LIGHTING_INCLUDED
    #define UBER_LIGHTING_INCLUDED

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/BRDF.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging3D.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GlobalIllumination.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/AmbientOcclusion.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"

    #if defined(LIGHTMAP_ON)
        #define DECLARE_LIGHTMAP_OR_SH(lmName, shName, index) float2 lmName : TEXCOORD##index
        #define OUTPUT_LIGHTMAP_UV(lightmapUV, lightmapScaleOffset, OUT) OUT.xy = lightmapUV.xy * lightmapScaleOffset.xy + lightmapScaleOffset.zw;
        #define OUTPUT_SH4(absolutePositionWS, normalWS, viewDir, OUT, OUT_OCCLUSION)
        #define OUTPUT_SH(normalWS, OUT)
    #else
        #define DECLARE_LIGHTMAP_OR_SH(lmName, shName, index) half3 shName : TEXCOORD##index
        #define OUTPUT_LIGHTMAP_UV(lightmapUV, lightmapScaleOffset, OUT)
        #ifdef USE_APV_PROBE_OCCLUSION
            #define OUTPUT_SH4(absolutePositionWS, normalWS, viewDir, OUT, OUT_OCCLUSION) OUT.xyz = SampleProbeSHVertex(absolutePositionWS, normalWS, viewDir, OUT_OCCLUSION)
        #else
            #define OUTPUT_SH4(absolutePositionWS, normalWS, viewDir, OUT, OUT_OCCLUSION) OUT.xyz = SampleProbeSHVertex(absolutePositionWS, normalWS, viewDir)
        #endif
        // Note: This is the legacy function, which does not support APV.
        // Kept to avoid breaking shaders still calling it (UUM-37723)
        #define OUTPUT_SH(normalWS, OUT) OUT.xyz = SampleSHVertex(normalWS)
    #endif

    float3 Saturate(float3 rgb, float adjustment)
    {
        // Algorithm from Chapter 16 of OpenGL Shading Language
        const float3 W = float3(0.2125, 0.7154, 0.0721);
        float3 intensity = dot(rgb, W);
        return lerp(intensity, rgb, adjustment);
    }


    //LAMBERT SHADING AND THEN ADDS THE SPECULAR
    half3 LightingPhysicallyBased(BRDFData brdfData, BRDFData brdfDataClearCoat,
    half3 lightColor, half3 lightDirectionWS, float lightAttenuation,
    half3 normalWS, half3 viewDirectionWS,
    half clearCoatMask, bool specularHighlightsOff, float lightAttenWShadows, UberExtraData extraData)
    {
        half NdotL = saturate(dot(normalWS, lightDirectionWS));
        half3 radiance = lightColor * (lightAttenuation * NdotL);

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
            // Old Method
            float3 H = normalize(lightDirectionWS + normalWS * extraData.sss_distortion);
            float ViewDotH = pow(saturate(dot(viewDirectionWS, -H)), extraData.sss_power) * 1.0;
            half3 SSSIrradianceVD = ViewDotH * (1-extraData.sss_thickness);
            // return (brdf * radiance) + (lightColor * SSSIrradiance * extraData.sss_mask * lightAttenWShadows * extraData.sss_color);

            //Newer Method
            float pNdotL = saturate(NdotL) * lightAttenuation; // positive ndl
            float nNdotL = saturate(-NdotL); // negative ndl
            float thickness = 1-extraData.sss_thickness;
            float3 radius = clamp(thickness - extraData.sss_scale, 0.0, 1.5);
            float3 sss = 1 * pow((float3)1. - pNdotL, 3.0 / (radius + 0.001)) * pow((float3)1.0 - nNdotL, 3. / (radius + 0.001));

            float3 subColor = Saturate(extraData.diffuseTex, 1.5) * extraData.sss_color;
            subColor *= lightColor;
            float3 SSSIrradiance = subColor * radius * (sss + SSSIrradianceVD) * extraData.sss_mask;

            return (brdf * radiance) + SSSIrradiance;
        #else
            return brdf * radiance * radiance;
        #endif

        return brdf * radiance;
    }

    half3 LightingPhysicallyBased(BRDFData brdfData, BRDFData brdfDataClearCoat, Light light, half3 normalWS, half3 viewDirectionWS, half clearCoatMask, bool specularHighlightsOff, UberExtraData extraData)
    {
        return LightingPhysicallyBased(brdfData, brdfDataClearCoat, light.color, light.direction, light.distanceAttenuation * light.shadowAttenuation, normalWS, viewDirectionWS, clearCoatMask, specularHighlightsOff, light.distanceAttenuation, extraData);
    }

    //VERTEX LIGHTING

    half3 VertexLighting(float3 positionWS, half3 normalWS)
    {
        half3 vertexLightColor = half3(0.0, 0.0, 0.0);

        #ifdef _ADDITIONAL_LIGHTS_VERTEX
            uint lightsCount = GetAdditionalLightsCount();
            uint meshRenderingLayers = GetMeshRenderingLayer();

            LIGHT_LOOP_BEGIN(lightsCount)
            Light light = GetAdditionalLight(lightIndex, positionWS);

            #ifdef _LIGHT_LAYERS
                if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
            #endif
            {
                half3 lightColor = light.color * light.distanceAttenuation;
                vertexLightColor += LightingLambert(lightColor, light.direction, normalWS);
            }

            LIGHT_LOOP_END
        #endif

        return vertexLightColor;
    }

    struct LightingData
    {
        half3 giColor;
        half3 mainLightColor;
        half3 additionalLightsColor;
        half3 vertexLightingColor;
        half3 emissionColor;
    };

    //Method to add all stages of lighting

    half3 CalculateLightingColor(LightingData lightingData, half3 albedo)
    {
        half3 lightingColor = 0;

        if (IsOnlyAOLightingFeatureEnabled())
        {
            return lightingData.giColor; // Contains white + AO

        }

        if (IsLightingFeatureEnabled(DEBUGLIGHTINGFEATUREFLAGS_GLOBAL_ILLUMINATION))
        {
            lightingColor += lightingData.giColor;
        }

        if (IsLightingFeatureEnabled(DEBUGLIGHTINGFEATUREFLAGS_MAIN_LIGHT))
        {
            lightingColor += lightingData.mainLightColor;
        }

        if (IsLightingFeatureEnabled(DEBUGLIGHTINGFEATUREFLAGS_ADDITIONAL_LIGHTS))
        {
            lightingColor += lightingData.additionalLightsColor;
        }

        if (IsLightingFeatureEnabled(DEBUGLIGHTINGFEATUREFLAGS_VERTEX_LIGHTING))
        {
            lightingColor += lightingData.vertexLightingColor;
        }

        lightingColor *= albedo;

        if (IsLightingFeatureEnabled(DEBUGLIGHTINGFEATUREFLAGS_EMISSION))
        {
            lightingColor += lightingData.emissionColor;
        }

        return lightingColor;
    }

    half4 CalculateFinalColor(LightingData lightingData, half alpha)
    {
        half3 finalColor = CalculateLightingColor(lightingData, 1);

        return half4(finalColor, alpha);
    }

    half4 CalculateFinalColor(LightingData lightingData, half3 albedo, half alpha, float fogCoord)
    {
        half fogFactor = 0;
        #if defined(_FOG_FRAGMENT)
            bool anyFogEnabled = false;
            
            #if defined(FOG_LINEAR_KEYWORD_DECLARED)
                if (FOG_LINEAR)
                    anyFogEnabled = true;
            #endif
            
            #if defined(FOG_EXP_KEYWORD_DECLARED)
                if (FOG_EXP)
                    anyFogEnabled = true;
            #endif
            
            #if defined(FOG_EXP2_KEYWORD_DECLARED)
                if (FOG_EXP2)
                    anyFogEnabled = true;
            #endif
            
            if (anyFogEnabled)
            {
                float viewZ = -fogCoord;
                float nearToFarZ = max(viewZ - _ProjectionParams.y, 0);
                fogFactor = ComputeFogFactorZ0ToFar(nearToFarZ);
            }
        #else  // #if defined(_FOG_FRAGMENT)
            fogFactor = fogCoord;
        #endif // #if defined(_FOG_FRAGMENT)
        half3 lightingColor = CalculateLightingColor(lightingData, albedo);
        half3 finalColor = MixFog(lightingColor, fogFactor);

        return half4(finalColor, alpha);
    }

    LightingData CreateLightingData(InputData inputData, SurfaceData surfaceData)
    {
        LightingData lightingData;

        lightingData.giColor = inputData.bakedGI;
        lightingData.emissionColor = surfaceData.emission;
        lightingData.vertexLightingColor = 0;
        lightingData.mainLightColor = 0;
        lightingData.additionalLightsColor = 0;

        return lightingData;
    }


    half4 FragmentPBR(InputData inputData, UberSurfaceData surfaceData, UberExtraData extraData)
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
            lightingData.mainLightColor = LightingPhysicallyBased(brdfData, brdfDataClearCoat,
            mainLight,
            inputData.normalWS, inputData.viewDirectionWS,
            surfaceData.clearCoatMask, specularHighlightsOff, extraData);
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
                        lightingData.additionalLightsColor += LightingPhysicallyBased(brdfData, brdfDataClearCoat, light,
                        inputData.normalWS, inputData.viewDirectionWS,
                        surfaceData.clearCoatMask, specularHighlightsOff, extraData);
                    }
                }
            #endif

            LIGHT_LOOP_BEGIN(pixelLightCount)
            Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);

            #ifdef _LIGHT_LAYERS
                if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
            #endif
            {
                lightingData.additionalLightsColor += LightingPhysicallyBased(brdfData, brdfDataClearCoat, light,
                inputData.normalWS, inputData.viewDirectionWS,
                surfaceData.clearCoatMask, specularHighlightsOff, extraData);
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

    //ADD HERE CUSTOM LIGHTING MODELS

    #include "Assets/Shaders/UberShader/Oren Nayar/OrenNayar.hlsl"
    #include "Assets/Shaders/UberShader/Half Lambert/HalfLambert.hlsl"

#endif