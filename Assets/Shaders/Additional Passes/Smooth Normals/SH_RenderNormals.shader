Shader "Hidden/SH_RenderNormals"
{
    SubShader
    {
        Pass
        {
            Name "Smooth Normals"
            Tags
            {
                "LightMode" = "SmoothNormalsPass"
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
                float4 position : POSITION;
                float3 normal : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : TEXCOORD5;
            };

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(i.position.xyz);
                o.normalWS = TransformObjectToWorldNormal(i.normal);
                return o;
            }

            half4 frag(Varyings i) : SV_TARGET
            {
                return half4(i.normalWS, 1);
            }

            ENDHLSL

        }
    }
}
