#ifndef OREN_NAYER_INCLUDED
    #define OREN_NAYER_INCLUDED

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/BRDF.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging3D.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GlobalIllumination.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/AmbientOcclusion.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"

    // half3 LightingOrenNayer(BRDFData brdfData, half3 lightColor, half3 lightDirectionWS, float lightAttenuation,
    // half3 normalWS, half3 viewDirectionWS)
    // {
    //     float roughness = brdfData.roughness;
    //     float roughnessSqr = brdfData.roughness * brdfData.roughness;
    //     float3 o_n_fraction = roughnessSqr / (roughnessSqr + float3(0.33, 0.13, 0.09));
    //     float3 oren_nayar = float3(1, 0, 0) + float3(-0.5, 0.17, 0.45) * o_n_fraction;

    //     float cos_ndotl = saturate(dot(normalWS, lightDirectionWS));
    //     float cos_ndotv = saturate(dot(normalWS, viewDirectionWS));

    //     float oren_nayar_s = saturate(dot(lightDirectionWS, viewDirectionWS)) - cos_ndotl * cos_ndotv;
    //     oren_nayar_s /= lerp(max(cos_ndotl, cos_ndotv), 1, step(oren_nayar_s, 0));
    //     oren_nayar_s = saturate(oren_nayar_s);

    //     float3 lightingModel = brdfData.diffuse * cos_ndotl * (oren_nayar.x + brdfData.diffuse * oren_nayar.y + oren_nayar.z * oren_nayar_s);
    //     float3 attenColor = lightAttenuation * lightColor.rgb;

    //     float3 finalColor = lightingModel * attenColor;

    //     return finalColor;

    // }

    half3 LightingOrenNayer(BRDFData brdfData, half3 lightColor, half3 lightDirectionWS, float lightAttenuation,
    half3 normalWS, half3 viewDirectionWS, float lightAttenWShadows, UberExtraData extraData)
    {

        half NdotL = saturate(dot(normalWS, lightDirectionWS));
        half NdotV = saturate(dot(normalWS, viewDirectionWS));

        float roughness = brdfData.roughness * brdfData.roughness;
        half roughnessSqr = roughness * roughness;

        half A = 1.0 - 0.5 * roughnessSqr / (roughnessSqr + 0.33);
        half B = 0.45 * roughnessSqr / (roughnessSqr + 0.09);
        half C = saturate(dot(normalize(viewDirectionWS - normalWS * NdotV), normalize(lightDirectionWS - normalWS * NdotL)));
        half angleL = acos(NdotL);
        half angleV = acos(NdotV);
        half alpha  = max(angleL, angleV);
        half beta   = min(angleL, angleV);

        half3 radiance = ((A + B * C * sin(alpha) * tan(beta)) * NdotL * lightAttenuation) * lightColor;

        #if defined(_SUBSURFACE_SCATTER)
            float3 H = normalize(lightDirectionWS + normalWS * extraData.sss_distortion);
            float ViewDotH = pow(saturate(dot(viewDirectionWS, -H)), extraData.sss_power) * extraData.sss_scale;
            half3 SSSIrradiance = ViewDotH * (1-extraData.sss_thickness);
            // return extraData.sss_thickness.xxx;

            return (brdfData.diffuse * radiance) + (lightColor * SSSIrradiance * extraData.sss_mask * lightAttenWShadows * extraData.sss_color);
        #else
            return brdfData.diffuse * radiance;
        #endif

    }

    half3 LightingOrenNayer(BRDFData brdfData, Light light, half3 normalWS, half3 viewDirectionWS, UberExtraData extraData)
    {
        return LightingOrenNayer(brdfData, light.color, light.direction, light.distanceAttenuation * light.shadowAttenuation, normalWS, viewDirectionWS, light.distanceAttenuation,extraData);
    }


    half4 FragmentOrenNayer(InputData inputData, UberSurfaceData surfaceData, UberExtraData extraData)
    {
        BRDFData brdfData;
        InitializeBRDFData(surfaceData, brdfData);

        #if defined(DEBUG_DISPLAY)
            half4 debugColor;

            if (CanDebugOverrideOutputColor(inputData, surfaceData, brdfData, debugColor))
            {
                return debugColor;
            }
        #endif

        BRDFData brdfDataClearCoat = CreateClearCoatBRDFData(surfaceData, brdfData);
        half4 shadowMask = CalculateShadowMask(inputData);
        AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData, surfaceData);
        uint meshRenderingLayers = GetMeshRenderingLayer();

        //INITIALIZE LIGHTING
        Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);
        MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI);

        LightingData lightingData = CreateLightingData(inputData, surfaceData);

        lightingData.giColor = GlobalIllumination(brdfData, brdfDataClearCoat, surfaceData.clearCoatMask,
        inputData.bakedGI, aoFactor.indirectAmbientOcclusion, inputData.positionWS,
        inputData.normalWS, inputData.viewDirectionWS, inputData.normalizedScreenSpaceUV);

        //MAIN LIGHT
        #ifdef _LIGHT_LAYERS
            if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
        #endif
        {
            lightingData.mainLightColor = LightingOrenNayer(brdfData, mainLight, inputData.normalWS, inputData.viewDirectionWS, extraData);
        }
        //---------------------------------


        #if defined(_ADDITIONAL_LIGHTS)
            uint pixelLightCount = GetAdditionalLightsCount();

            //FORWARD+
            #if USE_CLUSTER_LIGHT_LOOP
                [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                {
                    CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK

                    Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);

                    #ifdef _LIGHT_LAYERS
                        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
                    #endif
                    {
                        lightingData.additionalLightsColor += LightingOrenNayer(brdfData, light, inputData.normalWS, inputData.viewDirectionWS, extraData);
                    }
                }
            #endif

            LIGHT_LOOP_BEGIN(pixelLightCount)
            Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);

            #ifdef _LIGHT_LAYERS
                if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
            #endif
            {
                lightingData.additionalLightsColor += LightingOrenNayer(brdfData, light, inputData.normalWS, inputData.viewDirectionWS, extraData);
            }
            LIGHT_LOOP_END
        #endif


        //VERTEX LIGHTS
        #if defined(_ADDITIONAL_LIGHTS_VERTEX)
            lightingData.vertexLightingColor += inputData.vertexLighting * brdfData.diffuse;
        #endif

        //FINAL LIGHT COLOR
        #if REAL_IS_HALF
            // Clamp any half.inf+ to HALF_MAX
            return min(CalculateFinalColor(lightingData, surfaceData.alpha), HALF_MAX);
        #else
            return CalculateFinalColor(lightingData, surfaceData.alpha);
        #endif

    }

#endif