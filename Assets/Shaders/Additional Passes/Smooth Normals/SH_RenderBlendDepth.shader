Shader "Hidden/SH_RenderBlendDepth"
{
    SubShader
    {
        Pass
        {
            Name "Smooth Normals Depth"
            Tags
            {
                "LightMode" = "SmoothNormalsDepthPass"
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

            half frag(Varyings i) : SV_TARGET
            {
                return i.positionHCS.z;
            }

            ENDHLSL

        }
    }
}
