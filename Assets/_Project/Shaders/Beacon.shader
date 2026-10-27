// Hearth Shrine flame beacon (step 7b), additive, four modes:
// 0 = a soft flame column on a camera-facing quad (fades upward, flickers, slow pulse), seen from across the window;
// 1 = a flat glow ring above the bowl whose arc fills with _Fill (the palm-hold progress);
// 2 = a calm thin light pillar (step 9e: the gaze tile); 3 = lava cracks spreading on a flat quad (a Keeper's stomp);
// step 11d: 4 = a warm portal swirl standing in an open doorway; 5 = a flat arrow along +v (an objective outside the
// island window).
Shader "Diorama/Beacon"
{
    Properties
    {
        _Color ("Edge Colour", Color) = (1, 0.55, 0.2, 1)
        _Core ("Core Colour", Color) = (1, 0.9, 0.6, 1)
        _Intensity ("Intensity", Range(0, 3)) = 1
        _Mode ("Mode (0 column, 1 ring, 2 pillar, 3 cracks, 4 portal, 5 arrow)", Float) = 0
        _Fill ("Ring Fill", Range(0, 1)) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Depth Test", Float) = 4
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent+10" }

        Pass
        {
            Name "Beacon"
            Tags { "LightMode" = "UniversalForward" }
            Blend One One
            ZWrite Off
            ZTest [_ZTest]
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _Core;
                float _Intensity;
                float _Mode;
                float _Fill;
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

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float t = _Time.y;
                float3 col;
                float a;
                if (_Mode < 0.5)
                {
                    // Column: soft across, fades with height, licks of flame scrolling up, a slow breathing pulse.
                    float x = abs(i.uv.x * 2.0 - 1.0);
                    float y = i.uv.y;
                    float wobble = sin(y * 9.0 - t * 3.1 + sin(t * 1.3) * 2.0) * 0.08 * y;
                    float across = saturate(1.0 - abs(x + wobble) * (1.0 + y * 1.5));
                    across *= across;
                    float rise = smoothstep(0.0, 0.08, y) * pow(saturate(1.0 - y), 1.4);
                    float licks = 0.75 + 0.25 * sin(y * 22.0 - t * 6.0 + x * 4.0);
                    float pulse = 0.8 + 0.2 * sin(t * 1.6);
                    a = across * rise * licks * pulse;
                    col = lerp(_Color.rgb, _Core.rgb, saturate(across * (1.0 - y) * 1.5));
                }
                else if (_Mode > 4.5)
                {
                    // Arrow (step 11d): a head and a stem along +v, soft edged, nudging forward now and then.
                    float2 p = i.uv * 2.0 - 1.0;
                    p.y -= 0.12 * pow(saturate(sin(t * 3.2)), 4.0);
                    float head = max(-p.y, abs(p.x) * 0.8 + p.y * 0.6 - 0.48);
                    float stem = max(abs(p.x) - 0.17, max(-p.y - 0.62, p.y - 0.05));
                    float d = min(head, stem);
                    a = saturate(1.0 - d / 0.07) * (d > 0.0 ? 0.5 : 1.0);
                    col = lerp(_Color.rgb, _Core.rgb, saturate(-d * 5.0));
                    a *= 0.8 + 0.2 * sin(t * 5.0);
                }
                else if (_Mode > 3.5)
                {
                    // Portal (step 11d): a warm soft oval in the doorway with a slow swirl turning through it.
                    float2 p = i.uv * 2.0 - 1.0;
                    float r = length(p * float2(1.0, 0.8));
                    float ang = atan2(p.y, p.x);
                    float swirl = 0.5 + 0.5 * sin(ang * 3.0 + r * 9.0 - t * 2.2);
                    float disk = saturate(1.0 - r);
                    disk = disk * disk * (3.0 - 2.0 * disk);
                    a = disk * (0.5 + 0.5 * swirl) * (0.85 + 0.15 * sin(t * 1.7));
                    col = lerp(_Color.rgb, _Core.rgb, saturate(disk * 1.4));
                }
                else if (_Mode > 2.5)
                {
                    // Lava cracks (step 9e, a Keeper's stomp), on a flat quad: jagged glowing lines radiate from the
                    // middle out to _Fill (0..1), a hot core at the centre.
                    float2 p = i.uv * 2.0 - 1.0;
                    float r = length(p);
                    const float n = 7.0;
                    float k = atan2(p.y, p.x) / 6.28318 * n;
                    float cell = floor(k + 0.5);
                    float seed = frac(sin(cell * 12.9898 + 4.1) * 43758.5453);
                    float wob = sin(r * 17.0 + seed * 20.0) * 0.14 + sin(r * 43.0 + seed * 7.0) * 0.05;
                    float arc = abs(k - cell + wob) * r * 6.28318 / n;
                    float crack = saturate(1.0 - arc / (0.045 * (1.2 - r)));
                    float reach = _Fill * (0.55 + 0.45 * seed);
                    crack *= (1.0 - smoothstep(reach - 0.08, reach, r)) * smoothstep(0.05, 0.18, r);
                    float core = saturate(1.0 - r / (0.3 * _Fill + 1e-3)) * 0.6;
                    a = max(crack, core) * (0.85 + 0.15 * sin(t * 6.0 + r * 12.0));
                    col = lerp(_Color.rgb, _Core.rgb, saturate(crack * (1.0 - r) + core));
                }
                else if (_Mode > 1.5)
                {
                    // Pillar (step 9e, the gaze tile): a calm thin light, soft across, fading upward, no flame.
                    float x = abs(i.uv.x * 2.0 - 1.0);
                    float y = i.uv.y;
                    float across = saturate(1.0 - x);
                    across *= across;
                    a = across * smoothstep(0.0, 0.05, y) * pow(saturate(1.0 - y), 1.8) * (0.85 + 0.15 * sin(t * 4.0));
                    col = lerp(_Color.rgb, _Core.rgb, across * 0.5);
                }
                else
                {
                    // Ring: a thin band; the filled arc (clockwise from the front) is bright, the rest faint.
                    float2 p = i.uv * 2.0 - 1.0;
                    float r = length(p);
                    float band = saturate(1.0 - abs(r - 0.82) / 0.1);
                    band *= band;
                    float angle = frac(atan2(p.x, -p.y) / 6.28318 + 1.0);
                    float lit = angle <= _Fill ? 1.0 : 0.25 + 0.1 * sin(t * 3.0);
                    a = band * lit;
                    col = lerp(_Color.rgb, _Core.rgb, lit * 0.6);
                }
                a *= _Intensity;
                return half4(col * a, a);
            }
            ENDHLSL
        }
    }
}
