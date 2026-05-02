#ifndef BLEND_SHADER_INCLUDED
    #define BLEND_SHADER_INCLUDED

//PROP DEFINES

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"

    #include "Assets/Shaders/UberShader/UberSurfaceData.hlsl"


    #define CUSTOM_MATERIAL_PROPERTIES\
        half _Distance;

    #define CUSTOM_MATERIAL_PROPS_DOTS\
        UNITY_DOTS_INSTANCED_PROP(half, _Distance)
    
    #define CUSTOM_MATERIAL_PROPS_DOTS_DECLARE\
        static float unity_DOTS_Sampled_Distance;

    #define CUSTOM_MATERIAL_PROPS_DOTS_STATIC\
        unity_DOTS_Sampled_Distance = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(half, _Distance);

    #ifdef UNITY_DOTS_INSTANCING_ENABLED
        #define _Distance unity_DOTS_Sampled_Distance
    #endif

        TEXTURE2D(_NormalBlended); SAMPLER(sampler_NormalBlended);
        TEXTURE2D(_DepthBlended); SAMPLER(sampler_DepthBlended);

    #include "Assets/Shaders/UberShader/UberSurfaceInput.hlsl"

//PROP DEFINES

#ifdef SURFACE_SHADER
//SURFACE SHADER
    #include "Assets/Shaders/UberShader/UberLighting.hlsl"
    #include "Assets/Shaders/UberShader/UberBaseInterpolators.hlsl"

    void InputSurfaceModify(in Varyings input, inout InputData inputData)
    {
        
        inputData.normalWS = SAMPLE_TEXTURE2D(_NormalBlended, sampler_LinearClamp, inputData.normalizedScreenSpaceUV);
    }

    #define MODIFY_INPUTDATA(input, inputData) InputSurfaceModify(input, inputData)
    //SURFACE SHADER
#endif
    
    
// #ifdef VERTEX_SHADER_MOD
    
//     void GaneshaSurfaceVertex(in Attributes IN, inout Varyings OUT)
//     {
//         OUT.uv1 = IN.staticLightmapUV;
//         OUT.uv2 = IN.dynamicLightmapUV;
//     }
    
//     #define MODIFY_VERTEX_SHADER(input, output) GaneshaSurfaceVertex(input, output);
    
// #endif

    #include "Assets/Shaders/UberShader/UberLitPass.hlsl"


#endif