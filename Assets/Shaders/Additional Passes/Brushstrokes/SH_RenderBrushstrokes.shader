Shader "Hidden/SH_BrushStrokes"
{
    SubShader
    {
        Pass
        {
		    Name "BrushStrokes Pass"
			Tags
			{
				"LightMode" = "BrushStrokesPass"
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
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 pivot : TEXCOORD1;
                float3 posOS : TEXCOORD2;
                float3 normalOS : TEXCOORD3;
                float3 posWS : TEXCOORD4;
                float3 normalWS : TEXCOORD5;
            };

            TEXTURE2D(_BrushStrokeTexture);
            SAMPLER(sampler_BrushStrokeTexture);

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.pivot = TransformObjectToHClip(half3(0,0,0));
                OUT.posOS = IN.positionOS;
                OUT.normalOS = IN.normal;
                OUT.uv = IN.uv;
                OUT.posWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normal);
                return OUT;
            }

            half4 TriplanarSampling(float3 Position, float3 Normal, float Blend, float Tile, TEXTURE2D_PARAM(TextureIN, SamplerIN))
            {
                float3 Node_UV = Position * Tile;
                float3 Node_Blend = pow(abs(Normal), Blend);
                Node_Blend /= dot(Node_Blend, 1.0);
                float4 Node_X = SAMPLE_TEXTURE2D(TextureIN, SamplerIN, Node_UV.zy);
                float4 Node_Y = SAMPLE_TEXTURE2D(TextureIN, SamplerIN, Node_UV.xz);
                float4 Node_Z = SAMPLE_TEXTURE2D(TextureIN, SamplerIN, Node_UV.xy);
                float4 Out = Node_X * Node_Blend.x + Node_Y * Node_Blend.y + Node_Z * Node_Blend.z;
                return Out;
            }

            float RemapX(float In, float2 InMinMax, float2 OutMinMax)
            {
                return OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
            }

            float FresnelEffect(float3 Normal, float3 ViewDir, float Power)
            {
                return pow((1.0 - saturate(dot(normalize(Normal), ViewDir))), Power);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float tiling = RemapX(IN.pivot.z, float2(1,0), float2(20,0.2)) * _Tiling;
                float3 textureSampled = TriplanarSampling(IN.posOS, IN.normalOS, 1.0, tiling, _BrushStrokeTexture, sampler_BrushStrokeTexture);
                
                float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - IN.posWS.xyz);
                float fresnel = FresnelEffect(IN.normalWS, viewDir, 1);
                fresnel = smoothstep(_FresnelDebug.x, _FresnelDebug.y, 1-fresnel);
                //textureSampled = lerp(textureSampled, half3(0.5,0.5,0.5), fresnel);
                //return half4(IN.pivot,1);
                return half4(textureSampled, 1);
            }

            ENDHLSL
        }
    }
}
