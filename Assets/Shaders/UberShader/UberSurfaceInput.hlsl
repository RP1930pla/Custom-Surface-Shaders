#ifndef UBER_SURFACE_INPUT_INCLUDED
    #define UBER_SURFACE_INPUT_INCLUDED

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"

    //ADD CUSTOM SURFACE PARAMS HERE AS INCLUDE FILES
#if defined(_DETAIL_MULX2) || defined(_DETAIL_SCALED)
#define _DETAIL
#endif

    #ifndef INITIALIZE_CUSTOM_SURFACE_PARAMS
        #define INITIALIZE_CUSTOM_SURFACE_PARAMS(SurfaceData)
    #endif

    #ifndef CUSTOM_MATERIAL_PROPERTIES
        #define CUSTOM_MATERIAL_PROPERTIES
    #endif

    #ifndef CUSTOM_MATERIAL_PROPS_DOTS
        #define CUSTOM_MATERIAL_PROPS_DOTS
    #endif

    #ifndef CUSTOM_MATERIAL_PROPS_DOTS_DECLARE
        #define CUSTOM_MATERIAL_PROPS_DOTS_DECLARE
    #endif

    #ifndef CUSTOM_MATERIAL_PROPS_DOTS_STATIC
        #define CUSTOM_MATERIAL_PROPS_DOTS_STATIC
    #endif

    #ifndef CUSTOM_MATERIAL_PROPS_DOTS_MACRO
        #define CUSTOM_MATERIAL_PROPS_DOTS_MACRO
    #endif

    #ifndef CUSTOM_MATERIAL_TEXTURES
        #define CUSTOM_MATERIAL_TEXTURES
    #endif

    #include "Assets/Shaders/UberShader/UberSurfaceData.hlsl"

    ///////////////////////////////////////////////////////////////////////////////
    //                            Material Properties                            //
    ///////////////////////////////////////////////////////////////////////////////


    CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST;
        float4 _BaseMap_TexelSize;
        float4 _DetailAlbedoMap_ST;
        half4 _BaseColor;
        half4 _SpecColor;
        half4 _EmissionColor;
        half _Cutoff;
        half _Smoothness;
        half _Metallic;
        half _BumpScale;
        half _OcclusionStrength;
        half _DetailAlbedoMapScale;
        half _DetailNormalMapScale;
        half _Surface;

        half _SSS_Power;
        half _SSS_Distortion;
        half _SSS_Scale;
        half3 _SSS_Color;
        float2 _Debug;
        CUSTOM_MATERIAL_PROPERTIES

        UNITY_TEXTURE_STREAMING_DEBUG_VARS;
    CBUFFER_END


    #ifdef UNITY_DOTS_INSTANCING_ENABLED
        UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
        UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
        UNITY_DOTS_INSTANCED_PROP(float4, _SpecColor)
        UNITY_DOTS_INSTANCED_PROP(float4, _EmissionColor)
        UNITY_DOTS_INSTANCED_PROP(float, _Cutoff)
        UNITY_DOTS_INSTANCED_PROP(float, _Smoothness)
        UNITY_DOTS_INSTANCED_PROP(float, _Metallic)
        UNITY_DOTS_INSTANCED_PROP(float, _BumpScale)
        UNITY_DOTS_INSTANCED_PROP(float, _OcclusionStrength)
        UNITY_DOTS_INSTANCED_PROP(float, _DetailAlbedoMapScale)
        UNITY_DOTS_INSTANCED_PROP(float, _DetailNormalMapScale)
        UNITY_DOTS_INSTANCED_PROP(float, _Surface)
        UNITY_DOTS_INSTANCED_PROP(float, _SSS_Power)
        UNITY_DOTS_INSTANCED_PROP(float, _SSS_Distortion)
        UNITY_DOTS_INSTANCED_PROP(float, _SSS_Scale)
        UNITY_DOTS_INSTANCED_PROP(half3, _SSS_Color)
        CUSTOM_MATERIAL_PROPS_DOTS
        UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

        static float4 unity_DOTS_Sampled_BaseColor;
        static float4 unity_DOTS_Sampled_SpecColor;
        static float4 unity_DOTS_Sampled_EmissionColor;
        static float unity_DOTS_Sampled_Cutoff;
        static float unity_DOTS_Sampled_Smoothness;
        static float unity_DOTS_Sampled_Metallic;
        static float unity_DOTS_Sampled_BumpScale;
        static float unity_DOTS_Sampled_OcclusionStrength;
        static float unity_DOTS_Sampled_DetailAlbedoMapScale;
        static float unity_DOTS_Sampled_DetailNormalMapScale;
        static float unity_DOTS_Sampled_Surface;
        static float unity_DOTS_Sampled_SSS_Power;
        static float unity_DOTS_Sampled_SSS_Distortion;
        static float unity_DOTS_Sampled_SSS_Scale;
        static float unity_DOTS_Sampled_SSS_Color;
        CUSTOM_MATERIAL_PROPS_DOTS_DECLARE

        void SetupDOTSSimpleLitMaterialPropertyCaches()
        {
            unity_DOTS_Sampled_BaseColor = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BaseColor);
            unity_DOTS_Sampled_SpecColor = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _SpecColor);
            unity_DOTS_Sampled_EmissionColor = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _EmissionColor);
            unity_DOTS_Sampled_Cutoff = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _Cutoff);
            unity_DOTS_Sampled_Smoothness = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _Smoothness);
            unity_DOTS_Sampled_Metallic = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _Metallic);
            unity_DOTS_Sampled_BumpScale = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _BumpScale);
            unity_DOTS_Sampled_OcclusionStrength = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _OcclusionStrength);
            unity_DOTS_Sampled_DetailAlbedoMapScale = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _DetailAlbedoMapScale);
            unity_DOTS_Sampled_DetailNormalMapScale = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _DetailNormalMapScale);
            unity_DOTS_Sampled_Surface = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _Surface);
            unity_DOTS_Sampled_SSS_Power = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _SSS_Power);
            unity_DOTS_Sampled_SSS_Distortion = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _SSS_Distortion);
            unity_DOTS_Sampled_SSS_Scale = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _SSS_Scale);
            unity_DOTS_Sampled_SSS_Color = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _SSS_Color);
            CUSTOM_MATERIAL_PROPS_DOTS_STATIC
        }

        #undef UNITY_SETUP_DOTS_MATERIAL_PROPERTY_CACHES
        #define UNITY_SETUP_DOTS_MATERIAL_PROPERTY_CACHES() SetupDOTSSimpleLitMaterialPropertyCaches()

        #define _BaseColor              unity_DOTS_Sampled_BaseColor
        #define _SpecColor              unity_DOTS_Sampled_SpecColor
        #define _EmissionColor          unity_DOTS_Sampled_EmissionColor
        #define _Cutoff                 unity_DOTS_Sampled_Cutoff
        #define _Smoothness             unity_DOTS_Sampled_Smoothness
        #define _Metallic               unity_DOTS_Sampled_Metallic
        #define _BumpScale              unity_DOTS_Sampled_BumpScale
        #define _OcclusionStrength      unity_DOTS_Sampled_OcclusionStrength
        #define _DetailAlbedoMapScale   unity_DOTS_Sampled_DetailAlbedoMapScale
        #define _DetailNormalMapScale   unity_DOTS_Sampled_DetailNormalMapScale
        #define _Surface                unity_DOTS_Sampled_Surface
        #define _SSS_Power              unity_DOTS_Sampled_SSS_Power
        #define _SSS_Distortion         unity_DOTS_Sampled_SSS_Distortion
        #define _SSS_Scale              unity_DOTS_Sampled_SSS_Scale
        #define _SSS_Color              unity_DOTS_Sampled_SSS_Color
        CUSTOM_MATERIAL_PROPS_DOTS_MACRO

    #endif


    ///////////////////////////////////////////////////////////////////////////////
    //                      Material Properties Textures                         //
    ///////////////////////////////////////////////////////////////////////////////
    
        TEXTURE2D(_SpecGlossMap); SAMPLER(sampler_SpecGlossMap);
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        TEXTURE2D(_SSSMap); SAMPLER(sampler_SSSMap);
        UNITY_TEXTURE_STREAMING_DEBUG_VARS_FOR_TEX(_BaseMap);
        TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
        TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);

        TEXTURE2D(_ParallaxMap);        SAMPLER(sampler_ParallaxMap);
        TEXTURE2D(_OcclusionMap);       SAMPLER(sampler_OcclusionMap);
        TEXTURE2D(_DetailMask);         SAMPLER(sampler_DetailMask);
        TEXTURE2D(_DetailAlbedoMap);    SAMPLER(sampler_DetailAlbedoMap);
        TEXTURE2D(_DetailNormalMap);    SAMPLER(sampler_DetailNormalMap);
        TEXTURE2D(_MetallicGlossMap);   SAMPLER(sampler_MetallicGlossMap);

        CUSTOM_MATERIAL_TEXTURES
        // TEXTURE2D(_SpecGlossMap);       SAMPLER(sampler_SpecGlossMap);

    ///////////////////////////////////////////////////////////////////////////////
    //                      Material Property Helpers                            //
    ///////////////////////////////////////////////////////////////////////////////
    
    #ifdef _SPECULAR_SETUP
        #define SAMPLE_METALLICSPECULAR(uv) SAMPLE_TEXTURE2D(_SpecGlossMap, sampler_SpecGlossMap, uv)
    #else
        #define SAMPLE_METALLICSPECULAR(uv) SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, uv)
    #endif

    half4 SampleMetallicSpecGloss(float2 uv, half albedoAlpha)
    {
        half4 specGloss;

        #ifdef _METALLICSPECGLOSSMAP
            specGloss = half4(SAMPLE_METALLICSPECULAR(uv));
            #ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
                specGloss.a = albedoAlpha * _Smoothness;
            #else
                specGloss.a *= _Smoothness;
            #endif
        #else // _METALLICSPECGLOSSMAP
            #if _SPECULAR_SETUP
                specGloss.rgb = _SpecColor.rgb;
            #else
                specGloss.rgb = _Metallic.rrr;
            #endif

            #ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
                specGloss.a = albedoAlpha * _Smoothness;
            #else
                specGloss.a = _Smoothness;
            #endif
        #endif

        return specGloss;
    }

    half Alpha(half albedoAlpha, half4 color, half cutoff)
    {
        #if !defined(_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A) && !defined(_GLOSSINESS_FROM_BASE_ALPHA)
            half alpha = albedoAlpha * color.a;
        #else
            half alpha = color.a;
        #endif

        alpha = AlphaDiscard(alpha, cutoff);

        return alpha;
    }

    half4 SampleAlbedoAlpha(float2 uv, TEXTURE2D_PARAM(albedoAlphaMap, sampler_albedoAlphaMap))
    {
        return half4(SAMPLE_TEXTURE2D(albedoAlphaMap, sampler_albedoAlphaMap, uv));
    }

    half3 SampleNormal(float2 uv, TEXTURE2D_PARAM(bumpMap, sampler_bumpMap), half scale = half(1.0))
    {
        #ifdef _NORMALMAP
            half4 n = SAMPLE_TEXTURE2D(bumpMap, sampler_bumpMap, uv);
            #if BUMP_SCALE_NOT_SUPPORTED
                return UnpackNormal(n);
            #else
                return UnpackNormalScale(n, scale);
            #endif
        #else
            return half3(0.0h, 0.0h, 1.0h);
        #endif
    }

    half3 SampleEmission(float2 uv, half3 emissionColor, TEXTURE2D_PARAM(emissionMap, sampler_emissionMap))
    {
        #ifndef _EMISSION
            return 0;
        #else
            return SAMPLE_TEXTURE2D(emissionMap, sampler_emissionMap, uv).rgb * emissionColor;
        #endif
    }


    half4 SampleSpecularSmoothness(float2 uv, half alpha, half4 specColor, TEXTURE2D_PARAM(specMap, sampler_specMap))
    {
        half4 specularSmoothness = half4(0, 0, 0, 1);
        #ifdef _SPECGLOSSMAP
            specularSmoothness = SAMPLE_TEXTURE2D(specMap, sampler_specMap, uv) * specColor;
        #elif defined(_SPECULAR_COLOR)
            specularSmoothness = specColor;
        #endif

        #ifdef _GLOSSINESS_FROM_BASE_ALPHA
            specularSmoothness.a = alpha;
        #endif

        return specularSmoothness;
    }

    half3 ScaleDetailAlbedo(half3 detailAlbedo, half scale)
{
    // detailAlbedo = detailAlbedo * 2.0h - 1.0h;
    // detailAlbedo *= _DetailAlbedoMapScale;
    // detailAlbedo = detailAlbedo * 0.5h + 0.5h;
    // return detailAlbedo * 2.0f;

    // A bit more optimized
    return half(2.0) * detailAlbedo * scale - scale + half(1.0);
}

    half3 ApplyDetailAlbedo(float2 detailUv, half3 albedo, half detailMask)
    {
        #if defined(_DETAIL)
            half3 detailAlbedo = SAMPLE_TEXTURE2D(_DetailAlbedoMap, sampler_DetailAlbedoMap, detailUv).rgb;

            // In order to have same performance as builtin, we do scaling only if scale is not 1.0 (Scaled version has 6 additional instructions)
            #if defined(_DETAIL_SCALED)
                detailAlbedo = ScaleDetailAlbedo(detailAlbedo, _DetailAlbedoMapScale);
            #else
                detailAlbedo = half(2.0) * detailAlbedo;
            #endif

            return albedo * LerpWhiteTo(detailAlbedo, detailMask);
        #else
            return albedo;
        #endif
    }

    half3 ApplyDetailNormal(float2 detailUv, half3 normalTS, half detailMask)
    {
        #if defined(_DETAIL)
            #if BUMP_SCALE_NOT_SUPPORTED
                half3 detailNormalTS = UnpackNormal(SAMPLE_TEXTURE2D(_DetailNormalMap, sampler_DetailNormalMap, detailUv));
            #else
                half3 detailNormalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_DetailNormalMap, sampler_DetailNormalMap, detailUv), _DetailNormalMapScale);
            #endif

            // With UNITY_NO_DXT5nm unpacked vector is not normalized for BlendNormalRNM
            // For visual consistancy we going to do in all cases
            detailNormalTS = normalize(detailNormalTS);

            return lerp(normalTS, BlendNormalRNM(normalTS, detailNormalTS), detailMask); // todo: detailMask should lerp the angle of the quaternion rotation, not the normals
        #else
            return normalTS;
        #endif
    }

    half SampleOcclusion(float2 uv)
    {
        #ifdef _OCCLUSIONMAP
            half occ = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, uv).g;
            return LerpWhiteTo(occ, _OcclusionStrength);
        #else
            return half(1.0);
        #endif
    }


    ///////////////////////////////////////////////////////////////////////////////
    //                      Material Initialization                              //
    ///////////////////////////////////////////////////////////////////////////////


inline void InitializeStandardLitSurfaceData(float2 uv, out UberSurfaceData outSurfaceData, out UberExtraData outExtraData)
{
    half4 albedoAlpha = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
    outSurfaceData.alpha = Alpha(albedoAlpha.a, _BaseColor, _Cutoff);

    half4 specGloss = SampleMetallicSpecGloss(uv, albedoAlpha.a);
    outSurfaceData.albedo = albedoAlpha.rgb * _BaseColor.rgb;
    outSurfaceData.albedo = AlphaModulate(outSurfaceData.albedo, outSurfaceData.alpha);

#if _SPECULAR_SETUP
    outSurfaceData.metallic = half(1.0);
    outSurfaceData.specular = specGloss.rgb;
#else
    outSurfaceData.metallic = specGloss.r;
    outSurfaceData.specular = half3(0.0, 0.0, 0.0);
#endif

    outSurfaceData.smoothness = specGloss.a;
    outSurfaceData.normalTS = SampleNormal(uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);
    outSurfaceData.occlusion = SampleOcclusion(uv);
    outSurfaceData.emission = SampleEmission(uv, _EmissionColor.rgb, TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap));

    outSurfaceData.clearCoatMask       = half(0.0);
    outSurfaceData.clearCoatSmoothness = half(0.0);

#if defined(_DETAIL)
    half detailMask = SAMPLE_TEXTURE2D(_DetailMask, sampler_DetailMask, uv).a;
    float2 detailUv = uv * _DetailAlbedoMap_ST.xy + _DetailAlbedoMap_ST.zw;
    outSurfaceData.albedo = ApplyDetailAlbedo(detailUv, outSurfaceData.albedo, detailMask);
    outSurfaceData.normalTS = ApplyDetailNormal(detailUv, outSurfaceData.normalTS, detailMask);
#endif

#if defined(_SUBSURFACE_SCATTER)
    half2 subsurfaceTexture = SAMPLE_TEXTURE2D(_SSSMap, sampler_SSSMap, uv).xy;
    outExtraData.sss_mask = subsurfaceTexture.x;
    outExtraData.sss_thickness = subsurfaceTexture.y;
    outExtraData.sss_power = _SSS_Power;
    outExtraData.sss_distortion = _SSS_Distortion;
    outExtraData.sss_scale = _SSS_Scale;
    outExtraData.sss_color = _SSS_Color;
    outExtraData.diffuseTex = albedoAlpha.rgb * _BaseColor.rgb;
#else
    outExtraData.sss_mask = 0;
    outExtraData.sss_thickness = 0;
    outExtraData.sss_power = _SSS_Power;
    outExtraData.sss_distortion = _SSS_Distortion;
    outExtraData.sss_scale = _SSS_Scale;
    outExtraData.sss_color = _SSS_Color;
    outExtraData.diffuseTex = albedoAlpha.rgb * _BaseColor.rgb;
#endif
    


}

#endif