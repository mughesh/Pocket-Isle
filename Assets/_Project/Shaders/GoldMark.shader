// Step 12a: gold guidance marks lying on the island (premultiplied alpha, so a thin dark outline can darken sand, wheat
// or the cracked bed and the gold never blends into pale ground). Modes: 0 = a glowing tile (rounded square), 1 = a pair
// of footprints walking along +v, 2 = a soft ring (the stop tile). _Phase (0..1 along a trail) and _Pulse (0/1) make a
// pulse travel toward the goal; _Alpha fades a mark in and out.
Shader "Diorama/GoldMark"
{
    Properties
    {
        _Color ("Gold", Color) = (1, 0.74, 0.12, 1)
        _Core ("Core", Color) = (1, 0.95, 0.62, 1)
        _Outline ("Outline", Color) = (0.16, 0.08, 0.0, 1)
        _Intensity ("Intensity", Range(0, 3)) = 1.4
        _Mode ("Mode (0 tile, 1 footprints, 2 ring)", Float) = 0
        _Phase ("Trail Phase", Range(0, 1)) = 0
        _Pulse ("Travelling Pulse", Range(0, 1)) = 0
        _Alpha ("Alpha", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent+9" }

        Pass
        {
            Name "GoldMark"
            Tags { "LightMode" = "UniversalForward" }
            Blend One OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _Core;
                float4 _Outline;
                float _Intensity;
                float _Mode;
                float _Phase;
                float _Pulse;
                float _Alpha;
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
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            float RoundBox(float2 p, float2 b, float r)
            {
                float2 q = abs(p) - b + r;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
            }

            float Ellipse(float2 p, float2 c, float2 r)
            {
                float2 q = (p - c) / r;
                return (length(q) - 1.0) * min(r.x, r.y);
            }

            // One foot pointing +v, as a distance (negative inside): a small heel behind, a broad forefoot and three toes
            // ahead (Oct 28 notes: the old sole + one pad read as a heel in front, "reversed").
            float Foot(float2 p, float2 c)
            {
                float heel = Ellipse(p, c + float2(0.0, -0.26), float2(0.12, 0.14));
                float ball = Ellipse(p, c + float2(0.0, 0.08), float2(0.17, 0.22));
                float toes = min(Ellipse(p, c + float2(-0.11, 0.39), float2(0.055, 0.06)),
                             min(Ellipse(p, c + float2(0.0, 0.43), float2(0.06, 0.065)), Ellipse(p, c + float2(0.11, 0.39), float2(0.055, 0.06))));
                return min(min(heel, ball), toes);
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float t = _Time.y;
                float2 p = i.uv * 2.0 - 1.0;
                float d;
                float glow;
                if (_Mode > 1.5)
                {
                    // Ring: a band round the middle.
                    d = abs(length(p) - 0.74) - 0.1;
                    glow = 0.0;
                }
                else if (_Mode > 0.5)
                {
                    // Footprints: left foot behind, right foot ahead, toes toward the walk (+v).
                    d = min(Foot(p, float2(-0.3, -0.4)), Foot(p, float2(0.3, 0.1)));
                    glow = 0.0;
                }
                else
                {
                    d = RoundBox(p, float2(0.78, 0.78), 0.3);
                    // A soft halo round the tile so it lifts off the ground.
                    glow = saturate(1.0 - max(d, 0.0) / 0.2) * 0.35;
                }
                float aa = max(fwidth(d), 1e-3);
                float inside = saturate(0.5 - d / aa);
                float outline = saturate(0.5 - (d - 0.07) / aa) * (1.0 - inside);

                // A pulse travelling along the trail toward the goal, and a gentle breathing for everything else.
                float wave = frac(t * 0.55) * 1.4 - 0.2;
                float pulse = _Pulse > 0.5 ? exp(-pow((wave - _Phase) * 7.0, 2.0)) : 0.0;
                float breathe = 0.85 + 0.15 * sin(t * 3.0 + _Phase * 6.0);
                float k = _Intensity * (breathe + pulse * 0.9);

                float centre = saturate(1.0 - length(p) * 0.9);
                float3 gold = lerp(_Color.rgb, _Core.rgb, saturate(centre * 0.6 + pulse * 0.6));
                float a = inside * 0.88;
                float3 col = gold * k * inside;
                // Outline: dark, premultiplied (darkens what is under it).
                col += _Outline.rgb * outline * 0.75;
                a += outline * 0.75;
                // Halo: additive gold (alpha 0 adds on top).
                col += _Color.rgb * glow * k * (1.0 - inside - outline);
                return half4(col * _Alpha, saturate(a) * _Alpha);
            }
            ENDHLSL
        }
    }
}
