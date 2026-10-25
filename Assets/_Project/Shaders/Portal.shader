// Shard Gate portal surface (step 8), alpha blended on a quad inside the gate prop's opening.
// Shape: 0 = arch (straight sides, round top from _ArchStart up), 1 = oval (the well's water, lying flat).
// Open: a slow swirl of the gate's colour with a bright core and drifting ripples. Sealed (_Sealed 1): a soft stone
// seal (dark grey, carved rings, a faint seam of the gate colour). _Bump flashes the seal when walked into.
Shader "Diorama/Portal"
{
    Properties
    {
        _Color ("Gate Colour", Color) = (1, 0.6, 0.25, 1)
        _Shape ("Shape (0 arch, 1 oval)", Float) = 0
        _ArchStart ("Arch Start (0..1 of height)", Range(0, 1)) = 0.55
        _Sealed ("Sealed", Range(0, 1)) = 0
        _Bump ("Bump Flash", Range(0, 1)) = 0
        _Intensity ("Intensity", Range(0, 3)) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent+5" }

        Pass
        {
            Name "Portal"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Shape;
                float _ArchStart;
                float _Sealed;
                float _Bump;
                float _Intensity;
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

            // Signed inside distance (positive inside, ~0..1) of the opening.
            float Inside(float2 uv)
            {
                float x = uv.x * 2.0 - 1.0;
                if (_Shape > 0.5)
                {
                    float2 p = float2(x, uv.y * 2.0 - 1.0);
                    return 1.0 - length(p);
                }
                float side = 1.0 - abs(x);
                float bottom = uv.y * 8.0;
                if (uv.y < _ArchStart) return min(side, bottom);
                float2 p = float2(x, (uv.y - _ArchStart) / max(1.0 - _ArchStart, 1e-3));
                return 1.0 - length(p);
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float t = _Time.y;
                float inside = Inside(i.uv);
                float mask = smoothstep(0.0, 0.06, inside);
                if (mask <= 0.0) discard;

                float2 c = float2(i.uv.x * 2.0 - 1.0, _Shape > 0.5 ? i.uv.y * 2.0 - 1.0 : (i.uv.y - 0.45) * 2.0);
                float r = length(c);
                float a = atan2(c.y, c.x);

                // Open: swirl bands turning slowly, ripples moving outward, a bright soft core.
                float swirl = sin(a * 3.0 + r * 7.0 - t * 1.6) * 0.5 + 0.5;
                float ripple = sin(r * 16.0 - t * 3.0) * 0.5 + 0.5;
                float core = saturate(1.0 - r * 0.9);
                float3 open = lerp(_Color.rgb * 0.65, _Color.rgb, swirl);
                open = lerp(open, float3(1.0, 1.0, 1.0), core * core * 0.55 + ripple * 0.12);
                float openA = (0.55 + 0.3 * core + 0.1 * swirl) * _Intensity;

                // Sealed: carved stone rings, darker at the edge, a faint seam of the gate colour that breathes.
                float rings = smoothstep(0.42, 0.5, frac(r * 3.0)) * 0.12;
                float3 stone = float3(0.42, 0.41, 0.4) * (0.85 + 0.15 * saturate(inside * 3.0)) - rings;
                float seam = smoothstep(0.03, 0.0, abs(c.x + sin(c.y * 5.0) * 0.06)) * (0.25 + 0.15 * sin(t * 1.2));
                float3 sealedCol = stone + _Color.rgb * seam + _Color.rgb * _Bump * 0.6;
                float sealedA = 0.96;

                float3 col = lerp(open, sealedCol, _Sealed);
                float alpha = lerp(openA, sealedA, _Sealed) * mask;
                return half4(col, saturate(alpha));
            }
            ENDHLSL
        }
    }
}
