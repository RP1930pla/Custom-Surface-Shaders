#ifndef HIGHLIGHT_PROPS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Assets/Shaders/UberShader/UberSurfaceData.hlsl"

            #define CUSTOM_MATERIAL_PROPERTIES\
            half4 _AcessibilityColor;
            half2 _FresnelDebug;\
            half _Tiling;

            #define CUSTOM_MATERIAL_PROPS_DOTS\
            UNITY_DOTS_INSTANCED_PROP(half4, _AcessibilityColor)\
            UNITY_DOTS_INSTANCED_PROP(half2, _FresnelDebug)\
            UNITY_DOTS_INSTANCED_PROP(half, _Tiling)

            #define CUSTOM_MATERIAL_PROPS_DOTS_DECLARE\
            static float4 unity_DOTS_Sampled_AcessibilityColor;\
            static float2 unity_DOTS_Sampled_FresnelDebug;\
            static float unity_DOTS_Sampled_Tiling;

            #define CUSTOM_MATERIAL_PROPS_DOTS_STATIC\
            unity_DOTS_Sampled_AcessibilityColor = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(half4, _AcessibilityColor);\
            unity_DOTS_Sampled_FresnelDebug = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(half2, _FresnelDebug);\
            unity_DOTS_Sampled_Tiling = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(half, _Tiling);

            #ifdef UNITY_DOTS_INSTANCING_ENABLED
                #define _AcessibilityColor unity_DOTS_Sampled_AcessibilityColor
                #define _FresnelDebug unity_DOTS_Sampled_FresnelDebug
                #define _Tiling unity_DOTS_Sampled_Tiling
            #endif
            
            #include "Assets/Shaders/UberShader/UberSurfaceInput.hlsl"
#endif