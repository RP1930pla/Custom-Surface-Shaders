#ifndef GANESHA_SHADER_INCLUDED
    #define GANESHA_SHADER_INCLUDED

//PROP DEFINES

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"

    #include "Assets/Shaders/UberShader/UberSurfaceData.hlsl"


    #define CUSTOM_MATERIAL_PROPERTIES\
        half4 _DecalColor;

    #define CUSTOM_MATERIAL_PROPS_DOTS\
        UNITY_DOTS_INSTANCED_PROP(half4, _DecalColor)
    
    #define CUSTOM_MATERIAL_PROPS_DOTS_DECLARE\
        static float unity_DOTS_Sampled_DecalColor;

    #define CUSTOM_MATERIAL_PROPS_DOTS_STATIC\
        unity_DOTS_Sampled_DecalColor = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(half4, _DecalColor);

    #ifdef UNITY_DOTS_INSTANCING_ENABLED
        #define _DecalColor unity_DOTS_Sampled_DecalColor
    #endif

        TEXTURE2D(_DecalTexture); SAMPLER(sampler_DecalTexture);
        TEXTURE2D(_DecalTextureB); SAMPLER(sampler_DecalTextureB);

    #include "Assets/Shaders/UberShader/UberSurfaceInput.hlsl"

//PROP DEFINES

#ifdef SURFACE_SHADER
//SURFACE SHADER
    #include "Assets/Shaders/UberShader/UberLighting.hlsl"
    #include "Assets/Shaders/UberShader/UberBaseInterpolators.hlsl"

    void GaneshaSurfaceShader(in Varyings input, inout SurfaceData surfaceData, inout UberExtraData extraData)
    {
        half decalMask = SAMPLE_TEXTURE2D(_DecalTexture, sampler_DecalTexture, input.uv1).a;
        half decalMaskB = SAMPLE_TEXTURE2D(_DecalTextureB, sampler_DecalTextureB, input.uv2).a;
        surfaceData.albedo = lerp(surfaceData.albedo, _DecalColor, saturate(decalMask + decalMaskB));
    }

    #define MODIFY_SURFACE(input, surfaceData, extraData) GaneshaSurfaceShader(input, surfaceData, extraData)
    //SURFACE SHADER
    #endif
    
    
#ifdef VERTEX_SHADER_MOD
    
    void GaneshaSurfaceVertex(in Attributes IN, inout Varyings OUT)
    {
        OUT.uv1 = IN.staticLightmapUV;
        OUT.uv2 = IN.dynamicLightmapUV;
    }
    
    #define MODIFY_VERTEX_SHADER(input, output) GaneshaSurfaceVertex(input, output);
    
#endif

    #include "Assets/Shaders/UberShader/UberLitPass.hlsl"


#endif