// Soft smoke puffs for particle billboards (step 9e: a Keeper's frightened aura, its snorts and stomp dust).
// Alpha blended so dark smoke reads over passthrough (writes alpha as coverage), colour and alpha from the particle.
// A round puff with a slowly churning noisy edge; no textures.
Shader "Diorama/Smoke"
{
    Properties
    {
        _Softness ("Edge Softness", Range(0.05, 1)) = 0.6
        _Churn ("Edge Churn", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" }

        Pass
        {
            Name "Smoke"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Softness;
                float _Churn;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float seed : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float Hash(float2 p) { return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453); }

            float Noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), f.x), lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), f.x), f.y);
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 ws = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(ws);
                o.color = v.color;
                o.uv = v.uv;
                o.seed = frac(dot(ws, float3(3.1, 7.7, 5.3)) * 0.37);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float2 p = i.uv * 2.0 - 1.0;
                float t = _Time.y * 0.6;
                float n = Noise(p * 2.5 + float2(t, -t * 0.7) + i.seed * 9.0) * 0.6 + Noise(p * 5.0 - t + i.seed * 3.0) * 0.4;
                float r = length(p) + (n - 0.5) * _Churn;
                float a = 1.0 - smoothstep(1.0 - _Softness, 1.0, r);
                a *= 0.75 + 0.25 * n;
                a *= i.color.a;
                // Slightly darker in the churning folds.
                float3 col = i.color.rgb * (0.8 + 0.3 * n);
                return half4(col, a);
            }
            ENDHLSL
        }
    }
}
