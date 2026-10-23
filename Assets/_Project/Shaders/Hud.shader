// Flat rounded panel on a quad, for HP bars and move runes. No textures.
// _Aspect = quad width / height. A bar is the panel plus an inset fill (_Fill 0..1 from the left).
// Writes alpha as coverage (separate alpha blend) so it reads over passthrough.
// Queued just before other transparents, so text (TextMeshPro) on a panel always draws on top of it.
Shader "Diorama/Hud"
{
    Properties
    {
        _BackColor ("Back Colour", Color) = (0.08, 0.11, 0.09, 0.85)
        _FillColor ("Fill Colour", Color) = (0.44, 0.82, 0.42, 1)
        _Fill ("Fill", Range(0, 1)) = 1
        _Inset ("Fill Inset (of height)", Range(0, 0.5)) = 0.2
        _BorderColor ("Border Colour", Color) = (1, 1, 1, 0)
        _Border ("Border Width (of height)", Range(0, 0.3)) = 0.06
        _Aspect ("Aspect", Float) = 5
        _Radius ("Corner Radius (of height)", Range(0, 0.5)) = 0.45
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent-5" }

        Pass
        {
            Name "Hud"
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
                float4 _BackColor;
                float4 _FillColor;
                float _Fill;
                float _Inset;
                float4 _BorderColor;
                float _Border;
                float _Aspect;
                float _Radius;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            // Signed distance to a rounded box of half size h with corner radius r.
            float RoundBox(float2 p, float2 h, float r)
            {
                float2 q = abs(p) - h + r;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float2 p = (i.uv - 0.5) * float2(_Aspect, 1.0);
                float2 h = float2(_Aspect * 0.5, 0.5);
                float d = RoundBox(p, h, _Radius);
                float aa = max(fwidth(d), 1e-4);
                float4 col = _BackColor;

                float2 hi = h - _Inset;
                float di = RoundBox(p, hi, max(0.0, _Radius - _Inset));
                float edge = lerp(-hi.x, hi.x, _Fill);
                float fill = (1.0 - smoothstep(-aa, aa, di)) * (1.0 - smoothstep(-aa, aa, p.x - edge)) * step(0.001, _Fill);
                col = lerp(col, _FillColor, fill * _FillColor.a);

                float border = smoothstep(-_Border - aa, -_Border + aa, d) * _BorderColor.a;
                col.rgb = lerp(col.rgb, _BorderColor.rgb, border);
                col.a = max(col.a, border);

                col.a *= 1.0 - smoothstep(-aa, aa, d);
                return half4(col.rgb, col.a);
            }
            ENDHLSL
        }
    }
}
