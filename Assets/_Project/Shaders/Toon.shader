// Banded toon lighting with rim and specular glint, plus shader-painted masks in object space
// (belly, socks, tips, spots). Port of toonVertex / toonFragment from creature-diorama-v2.html.
// Light direction comes from the URP main light; its colour and intensity are ignored on purpose
// so the bands look the same under any passthrough lighting.
Shader "Diorama/Toon"
{
    Properties
    {
        [Header(Colour)]
        _BaseColor ("Base", Color) = (1, 1, 1, 1)
        _TopColor ("Top", Color) = (1, 1, 1, 1)
        [ToggleUI] _Grad ("Blend To Top (bottom to top gradient)", Float) = 0

        [Header(Belly)]
        [ToggleUI] _BellyOn ("Belly", Float) = 0
        _BellyColor ("Belly Colour", Color) = (1, 1, 1, 1)
        _BellyDir ("Belly Direction (object space)", Vector) = (0, 0, 1, 0)
        _BellyAt ("Belly Threshold", Float) = 0.5
        _BellyMaxY ("Belly Max Y", Float) = 9

        [Header(Socks)]
        [ToggleUI] _SockOn ("Socks", Float) = 0
        _SockColor ("Sock Colour", Color) = (0, 0, 0, 1)
        _SockY ("Sock Height", Float) = -9

        [Header(Tip)]
        [ToggleUI] _TipOn ("Tip", Float) = 0
        _TipColor ("Tip Colour", Color) = (0, 0, 0, 1)
        _TipAxis ("Tip Axis (object space)", Vector) = (0, 1, 0, 0)
        _TipAt ("Tip Threshold", Float) = 9

        [Header(Spots)]
        [ToggleUI] _SpotOn ("Spots", Float) = 0
        _SpotColor ("Spot Colour", Color) = (0, 0, 0, 1)
        _SpotScale ("Spot Scale", Float) = 9

        [Header(Lighting)]
        _Spec ("Specular Glint", Range(0, 2)) = 0.3
        _Emissive ("Emissive", Range(0, 2)) = 0
        _Flash ("Flash", Range(0, 1)) = 0
        _OriginJitter ("Per-Instance Shade Jitter", Range(0, 0.5)) = 0

        [Header(Motion)]
        _Breathe ("Breathe", Float) = 0
        _Jiggle ("Jiggle", Float) = 0

        [Header(Outline (read by Diorama Toon Outline))]
        _OutlineWidth ("Outline Width", Float) = 0.035
        _OutlineColor ("Outline Colour", Color) = (0.1, 0.1, 0.1, 1)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "ToonCommon.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 normalOS   : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float3 normalWS   : TEXCOORD3;
                float  shade      : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float3 p = ToonDeform(v.positionOS.xyz, v.normalOS);
                o.positionOS = v.positionOS.xyz;
                o.normalOS = v.normalOS;
                o.positionWS = TransformObjectToWorld(p);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.shade = 1.0 + (ToonOriginHash() - 0.5) * 2.0 * _OriginJitter;
                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float3 pO = i.positionOS;
                float3 nO = normalize(i.normalOS);

                // painted colour, all in object space
                float3 c = lerp(_BaseColor.rgb, _TopColor.rgb, _Grad * smoothstep(-0.4, 0.7, pO.y)) * i.shade;
                float sp = sin(pO.x * _SpotScale) * sin(pO.y * _SpotScale * 1.3 + 1.7) * sin(pO.z * _SpotScale * 0.9 + 0.5);
                c = lerp(c, _SpotColor.rgb, _SpotOn * smoothstep(0.30, 0.36, sp) * smoothstep(-0.1, 0.3, dot(nO, normalize(float3(0.0, 1.0, -0.6)))));
                c = lerp(c, _BellyColor.rgb, _BellyOn * smoothstep(_BellyAt, _BellyAt + 0.12, dot(nO, normalize(_BellyDir.xyz))) * smoothstep(_BellyMaxY + 0.05, _BellyMaxY - 0.05, pO.y));
                c = lerp(c, _SockColor.rgb, _SockOn * smoothstep(_SockY + 0.04, _SockY - 0.04, pO.y));
                c = lerp(c, _TipColor.rgb, _TipOn * smoothstep(_TipAt, _TipAt + 0.06, dot(pO, _TipAxis.xyz)));

                // banded light, rim, glint
                float3 L = _MainLightPosition.xyz;
                if (dot(L, L) < 1e-4) L = normalize(float3(-0.45, 0.85, 0.5));
                float3 n = normalize(i.normalWS);
                float3 v = normalize(GetCameraPositionWS() - i.positionWS);
                float ndl = dot(n, L);
                float band = 0.58 + 0.27 * smoothstep(-0.08, 0.0, ndl) + 0.15 * smoothstep(0.45, 0.52, ndl);
                float3 col = c * band;
                float rim = pow(1.0 - max(dot(n, v), 0.0), 3.0);
                col += rim * 0.32 * lerp(float3(1.0, 1.0, 1.0), c, 0.35) * smoothstep(-0.3, 0.2, ndl);
                float3 h = normalize(L + v);
                col += smoothstep(0.93, 0.95, dot(n, h)) * _Spec * 0.55;
                col += c * _Emissive;
                col = lerp(col, float3(1.0, 1.0, 1.0), _Flash);
                return float4(col, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            Cull Back
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "ToonCommon.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(ToonDeform(v.positionOS.xyz, v.normalOS));
                return o;
            }

            half frag(Varyings i) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
