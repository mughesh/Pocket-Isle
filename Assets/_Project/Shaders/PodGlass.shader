// Glass bubble of a capture pod: clear in the middle, tinted and bright at the edge (fresnel), a fixed soft highlight,
// a pulsing glow (lead pod, hover), a fainted look (greyed, cloudier, with faint cracks of flickering light) and
// white cracks just before a pod shatters.
// Alpha blended; writes alpha as coverage (separate alpha blend) so it also shows over passthrough.
Shader "Diorama/Pod Glass"
{
    Properties
    {
        _Tint ("Tint", Color) = (0.75, 0.9, 1, 1)
        _Clear ("Centre Alpha", Range(0, 1)) = 0.1
        _Edge ("Edge Alpha", Range(0, 1)) = 0.75
        _Glow ("Glow", Range(0, 2)) = 0
        _Grey ("Fainted (grey)", Range(0, 1)) = 0
        _Crack ("Crack Light", Range(0, 1)) = 0
        _CrackScale ("Crack Scale (1/m)", Float) = 140
        _Shatter ("Shatter Cracks", Range(0, 1)) = 0
        _Gold ("Can Act Now (gold rim)", Range(0, 1)) = 0
        _White ("Hovered (white rim)", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" }

        Pass
        {
            Name "PodGlass"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "DioramaWindow.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Tint;
                float _Clear;
                float _Edge;
                float _Glow;
                float _Grey;
                float _Crack;
                float _CrackScale;
                float _Shatter;
                float _Gold;
                float _White;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 posOS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewWS : TEXCOORD2;
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
                o.posOS = v.positionOS.xyz;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.viewWS = GetWorldSpaceViewDir(posWS);
                return o;
            }

            float3 Hash3(float3 p)
            {
                p = float3(dot(p, float3(127.1, 311.7, 74.7)), dot(p, float3(269.5, 183.3, 246.1)), dot(p, float3(113.5, 271.9, 124.6)));
                return frac(sin(p) * 43758.5453);
            }

            // Distance to the nearest cell border of a 3D Voronoi pattern (thin lines = cracks).
            float CrackLines(float3 p)
            {
                float3 cell = floor(p);
                float d1 = 8.0, d2 = 8.0;
                for (int z = -1; z <= 1; z++)
                for (int y = -1; y <= 1; y++)
                for (int x = -1; x <= 1; x++)
                {
                    float3 c = cell + float3(x, y, z);
                    float d = length(c + Hash3(c) - p);
                    if (d < d1) { d2 = d1; d1 = d; }
                    else if (d < d2) d2 = d;
                }
                return d2 - d1;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 n = normalize(i.normalWS);
                float3 v = normalize(i.viewWS);
                float ndv = saturate(dot(n, v));
                float fres = pow(1.0 - ndv, 2.2);

                float grey = saturate(_Grey);
                float3 tint = lerp(_Tint.rgb, dot(_Tint.rgb, float3(0.3, 0.59, 0.11)).xxx * 0.8, grey);
                float3 col = tint * (0.35 + 0.65 * fres) + fres * 0.35;
                float alpha = lerp(_Clear, _Edge, fres);
                alpha = lerp(alpha, max(alpha, 0.35), grey); // cloudier when fainted

                // Soft highlight from a fixed up-left light in view space.
                float3 lightWS = normalize(mul((float3x3)UNITY_MATRIX_I_V, float3(-0.45, 0.6, 0.65)));
                float3 h = normalize(lightWS + v);
                float spec = pow(saturate(dot(n, h)), 60.0) * (1.0 - 0.6 * grey);
                col += spec * 1.2;
                alpha = saturate(alpha + spec);

                // Pulsing glow from inside.
                float glow = _Glow * (0.75 + 0.25 * sin(_Time.y * 4.0));
                col += tint * glow * (0.5 + fres);
                alpha = saturate(alpha + glow * 0.25);

                // Hint target (gold) or gazed (white): a glowing rim (FocusHighlight globals).
                float2 focus = DioramaFocusAmounts(TransformObjectToWorld(float3(0.0, 0.0, 0.0)));
                // Step 9h: this pod's own focus (every pod the player can act on now is gold, a hovered one white).
                focus.y = max(focus.y, _White);
                focus.x = max(focus.x, _Gold) * (1.0 - _White);
                float rimGlow = smoothstep(0.25, 0.9, fres);
                col = lerp(col, _DioramaHoverColor.rgb * 1.2, rimGlow * focus.y * 0.8);
                col = lerp(col, _DioramaHighlightColor.rgb * 1.4, rimGlow * focus.x);
                alpha = saturate(alpha + rimGlow * (focus.x + 0.5 * focus.y));

                // Fainted: cracks of light that flicker.
                if (_Crack > 0.001)
                {
                    float lines = 1.0 - smoothstep(0.02, 0.07, CrackLines(i.posOS * _CrackScale));
                    float flicker = 0.55 + 0.45 * sin(_Time.y * 9.0 + sin(_Time.y * 2.3) * 3.0);
                    float3 crackCol = float3(1.0, 0.75, 0.45);
                    col = lerp(col, crackCol * 1.4, lines * _Crack * flicker);
                    alpha = saturate(alpha + lines * _Crack * flicker * 0.7);
                }
                // About to shatter: bright white cracks that widen fast.
                if (_Shatter > 0.001)
                {
                    float d = CrackLines(i.posOS * _CrackScale * 0.45);
                    float lines = 1.0 - smoothstep(0.005, 0.01 + 0.09 * _Shatter, d);
                    float k = lines * saturate(_Shatter * 1.6);
                    col = lerp(col, float3(1.6, 1.6, 1.6), k);
                    alpha = saturate(alpha + k);
                }
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}
