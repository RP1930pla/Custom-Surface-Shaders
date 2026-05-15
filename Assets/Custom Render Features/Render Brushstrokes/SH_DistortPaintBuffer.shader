Shader "Custom/SH_DistortPaintBuffer"
{   
    Properties
    {
        _Strength("Strenght", float) = 0.05
    }

    SubShader
    {
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        ENDHLSL

        Tags { "RenderType"="Opaque" }
        LOD 100
        ZWrite Off Cull Off
        Pass
        {
            Name "SH_DistortPaintBuffer"

            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment Frag

            float _Strength;
            TEXTURE2D(_BrushStrokeBuffer); SAMPLER(sampler_BrushStrokeBuffer);

            float4 Frag (Varyings input) : SV_Target
            {
                float paint = SAMPLE_TEXTURE2D(_BrushStrokeBuffer, sampler_BrushStrokeBuffer, input.texcoord).r;
                paint = (paint - 0.5) * 2;
                //return paint.xxxx;
                paint *= _Strength;

                float4 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, saturate(input.texcoord + paint)).rgba;
                return color;
            }
            
            ENDHLSL
        }
    }
}
