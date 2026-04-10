Shader "Hidden/SH_HighlightRender"
{
    SubShader
    {
        Pass
        {
            Name "HighlightPass"
            Tags
            {
                "LightMode" = "HighlightPass"
				"RenderType" = "Opaque" 
				"RenderPipeline" = "UniversalPipeline"
            }

			HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Shaders/Additional Passes/AdditionalPassesProps.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return _AcessibilityColor;
            }

            ENDHLSL
        }
    }
}
