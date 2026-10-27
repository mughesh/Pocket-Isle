// Step 12c: water held in the hands (Lilymere's scoop) and its pouring stream. Teal, see-through in the middle and
// brighter at the edge (fresnel), a light caustic sparkle that drifts, a soft white highlight. Texture-free.
// Alpha blended; writes alpha as coverage (separate alpha blend) so it reads over passthrough.
Shader "Diorama/Water Sheet"
{
    Properties
    {
        _Tint ("Tint", Color) = (0.36, 0.74, 0.86, 1)
        _Deep ("Deep", Color) = (0.16, 0.46, 0.66, 1)
        _Alpha ("Alpha", Range(0, 1)) = 0.72
        _Edge ("Edge Brightness", Range(0, 2)) = 0.9
        _Sparkle ("Caustic Sparkle", Range(0, 2)) = 0.8
        _SparkleScale ("Sparkle Scale (1/m)", Float) = 260
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" }

        Pass
        {
            Name "WaterSheet"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Tint;
                float4 _Deep;
                float _Alpha;
                float _Edge;
                float _Sparkle;
                float _SparkleScale;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.color = v.color;
                return o;
            }

            float Hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }

            float Noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), f.x), lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), f.x), f.y);
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 n = normalize(i.normalWS);
                float3 view = normalize(GetWorldSpaceViewDir(i.positionWS));
                float facing = abs(dot(n, view));
                float fresnel = pow(1.0 - facing, 2.0);
                float3 col = lerp(_Deep.rgb, _Tint.rgb, 0.45 + 0.55 * facing);
                col += fresnel * _Edge * float3(0.8, 0.95, 1.0) * 0.5;
                // Caustic sparkle: two drifting noise layers, only their bright crossings show.
                float t = _Time.y;
                float2 p = i.positionWS.xz * _SparkleScale;
                float c = Noise(p + float2(t * 1.3, t * 0.7)) * Noise(p * 1.7 - float2(t * 0.9, t * 1.1));
                col += smoothstep(0.42, 0.6, c) * _Sparkle * float3(0.9, 1.0, 1.0);
                // A soft fixed highlight toward the top of the view.
                col += pow(saturate(dot(reflect(-view, n), float3(0, 1, 0))), 24.0) * 0.6;
                // Vertex alpha thins the sheet as it pours and fades the stream's tail.
                float a = saturate((_Alpha + fresnel * 0.25) * i.color.a);
                return half4(col, a);
            }
            ENDHLSL
        }
    }
}
