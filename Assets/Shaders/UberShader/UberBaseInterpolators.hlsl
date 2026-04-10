#ifndef UBER_BASE_INTERPOLATORS
    #define CUSTOM_INTERPOLATORS
    #if defined(LOD_FADE_CROSSFADE)
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
    #endif

    #if defined(_PARALLAXMAP)
        #define REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR
    #endif

    #if (defined(_NORMALMAP) || (defined(_PARALLAXMAP) && !defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR))) || defined(_DETAIL)
        #define REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR
    #endif

    struct Attributes
    {
        float4 positionOS : POSITION;
        float3 normalOS : NORMAL;
        float4 tangentOS : TANGENT;
        float2 texcoord : TEXCOORD0;
        float2 staticLightmapUV : TEXCOORD1;
        float2 dynamicLightmapUV : TEXCOORD2;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float2 uv : TEXCOORD0;
        float2 uv1 : UVDECAL;
        float2 uv2 : UVDECAL_B;
        #if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
            float3 positionWS : TEXCOORD1;
        #endif

        float3 normalWS : TEXCOORD2;
        #if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
            half4 tangentWS : TEXCOORD3;    // xyz: tangent, w: sign
        #endif

        #ifdef _ADDITIONAL_LIGHTS_VERTEX
            half4 fogFactorAndVertexLight : TEXCOORD5; // x: fogFactor, yzw: vertex light
        #else
            half fogFactor : TEXCOORD5;
        #endif

        #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
            float4 shadowCoord : TEXCOORD6;
        #endif

        #if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
            half3 viewDirTS : TEXCOORD7;
        #endif

        DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 8);
        #ifdef DYNAMICLIGHTMAP_ON
            float2 dynamicLightmapUV : TEXCOORD9; // Dynamic lightmap UVs
        #endif

        #ifdef USE_APV_PROBE_OCCLUSION
            float4 probeOcclusion : TEXCOORD10;
        #endif

        float4 positionCS : SV_POSITION;
        UNITY_VERTEX_INPUT_INSTANCE_ID
        UNITY_VERTEX_OUTPUT_STEREO
    };
#endif