Shader "Custom/SH_NormalBlur"
{   
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
            Name "SH_NormalBlur"

            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment Frag

            float InterleavedGradientNoiseFunc(half2 uv, half offset)
            {
                uv += (frac(_Time.y * 100 + offset)) * (half2(47, 17) * 0.695f);
                half3 magic = half3(0.06711056f, 0.00583715f, 52.9829189f);
                return frac(magic.z * frac(dot(uv, magic.xy)));
            }

            float2 Random2DFrom2D(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
                
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.xx + p3.yz) * p3.zy);
            }

            #define diff3(f, x, y) (((f((x) + (y))) + (f((x) - (y))) - 2. * (f((x)))) / dot(y, y))

            float hash21(float2 p)
            {
                float3 q = frac(float3(p.xyx) * .1031);
                q += dot(q, q.yzx + 19.19);
                return frac((q.x + q.y) * q.z);
            }

            float bluenoise(float2 p)
            {
                return 1. / 4. * (
                    diff3(hash21, p, float2(1, 0))
                + diff3(hash21, p, float2(0, 1))
                ) + .5;
            }

            float4 Frag (Varyings input) : SV_Target
            {

                float2 res = (input.texcoord * 2) * (1/_BlitTexture_TexelSize);
                //res *= 2;
                float blueNoise = bluenoise(res + frac(_Time.y * 1));
                float2 randomNoise = Random2DFrom2D(half2(blueNoise, 1-blueNoise));
                randomNoise = (randomNoise - 0.5)*2;
                randomNoise *= 0.01;

                float4 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, (input.texcoord * 2) + randomNoise).rgba;
                return color;
            }
            
            ENDHLSL
        }


        Pass
        {
            Name "SH_NormalBlur"

            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment Frag

            float InterleavedGradientNoiseFunc(half2 uv, half offset)
            {
                uv += (frac(_Time.y * 100 + offset)) * (half2(47, 17) * 0.695f);
                half3 magic = half3(0.06711056f, 0.00583715f, 52.9829189f);
                return frac(magic.z * frac(dot(uv, magic.xy)));
            }

            float2 Random2DFrom2D(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
                
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.xx + p3.yz) * p3.zy);
            }

            #define diff3(f, x, y) (((f((x) + (y))) + (f((x) - (y))) - 2. * (f((x)))) / dot(y, y))

            float hash21(float2 p)
            {
                float3 q = frac(float3(p.xyx) * .1031);
                q += dot(q, q.yzx + 19.19);
                return frac((q.x + q.y) * q.z);
            }

            float bluenoise(float2 p)
            {
                return 1. / 4. * (
                    diff3(hash21, p, float2(1, 0))
                + diff3(hash21, p, float2(0, 1))
                ) + .5;
            }

            #define radius 5

            float4 Frag (Varyings input) : SV_Target
            {
                half4 result = half4(0,0,0,1);
                float numberOfSamples = 0;
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        half2 pixelOffset = half2(x, y);
                        half distanceToPixel = length(pixelOffset);

                        if (distanceToPixel > float(radius))
                        {
                            continue;
                        }

                        half2 uvOffset = pixelOffset * (_BlitTexture_TexelSize);

                        result += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, (input.texcoord * 2) + uvOffset);
                        numberOfSamples++;
                    }
                }

                result /= numberOfSamples;
                return half4(result.rgb,1);
            }
            
            ENDHLSL
        }
    }
}
