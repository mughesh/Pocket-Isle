// Battle effects, additive. One shader, three shapes picked by _Mode:
//   0 = orb / bubble on a sphere (fresnel edge, flickering noise, optional ripple bands)
//   1 = flat ring on a quad (shockwaves, ripples, impact pops)
//   2 = soft round dot on a quad, tinted by vertex colour (the pooled particle system)
// Writes alpha as coverage (separate alpha blend) so the glow also shows over passthrough.
Shader "Diorama/Fx"
{
    Properties
    {
        [Enum(Orb,0,Ring,1,Particle,2)] _Mode ("Mode", Float) = 0
        _Color ("Core Colour", Color) = (1, 0.95, 0.66, 1)
        _EdgeColor ("Edge Colour", Color) = (1, 0.29, 0.07, 1)
        _Alpha ("Alpha", Range(0, 1)) = 1
        _Power ("Fresnel Power", Range(0.5, 4)) = 1
        _CoreAlpha ("Core Alpha", Range(0, 1)) = 1
        _Noise ("Noise", Range(0, 1)) = 0.3
        _Ripple ("Ripple", Range(0, 1)) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" }

        Pass
        {
            Name "Fx"
            Tags { "LightMode" = "UniversalForward" }
            Blend One One, One One
            ZWrite Off
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Mode;
                float4 _Color;
                float4 _EdgeColor;
                float _Alpha;
                float _Power;
                float _CoreAlpha;
                float _Noise;
                float _Ripple;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 posOS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 viewWS : TEXCOORD3;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 posWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(posWS);
                o.uv = v.uv;
                o.posOS = v.positionOS.xyz;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.viewWS = GetWorldSpaceViewDir(posWS);
                o.color = v.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 col;
                float a;
                if (_Mode < 0.5)
                {
                    float t = _Time.y;
                    float3 p = i.posOS;
                    float n = sin(p.x * 7.0 + t * 13.0) * sin(p.y * 6.0 - t * 16.0) * sin(p.z * 5.0 + t * 10.0);
                    float f = pow(1.0 - abs(dot(normalize(i.normalWS), normalize(i.viewWS))), _Power);
                    float rip = 0.5 + 0.5 * sin(p.y * 14.0 - t * 5.0);
                    col = lerp(_Color.rgb, _EdgeColor.rgb, saturate(f * 1.2 + n * _Noise));
                    a = saturate(lerp(_CoreAlpha, 1.0, f) + n * _Noise * 0.5 + rip * _Ripple);
                }
                else if (_Mode < 1.5)
                {
                    float r = length(i.uv * 2.0 - 1.0);
                    col = _Color.rgb * 1.3;
                    a = smoothstep(0.6, 0.8, r) * smoothstep(1.0, 0.86, r);
                }
                else
                {
                    float r = length(i.uv * 2.0 - 1.0);
                    col = i.color.rgb;
                    a = (1.0 - smoothstep(0.15, 1.0, r)) * i.color.a;
                }
                a *= _Alpha;
                return half4(col * a, a);
            }
            ENDHLSL
        }
    }
}
