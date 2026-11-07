// Step 19: the credits' paper sky lanterns (and the portrait's wooden frame). Unlit toon: flat vertex colours, the paper
// glowing from inside (brighter at the bottom, a soft flicker), an inverted-hull outline. No lights, no textures.
//   uv.x: 0 = plain (bamboo, wood, gold), 1 = paper (glows), 2 = the lit opening at the bottom, 3 = the flame,
//         4 = the painting (_MainTex at uv1, over the vertex colour as its backing; property block per lantern)
//   uv.y: height on the lantern, 0 bottom .. 1 top (the paper's glow falls off upward)
//   color.a: outline weight (0 = none)
// _Alpha fades the whole lantern (property block); _Phase desyncs the flicker between lanterns.
Shader "Diorama/Sky Lantern"
{
    Properties
    {
        _MainTex ("Painting", 2D) = "white" {}
        _Alpha ("Alpha", Range(0, 1)) = 1
        _Phase ("Flicker Phase", Float) = 0
        _Glow ("Glow", Range(0, 2)) = 1
        _GlowColor ("Glow Colour", Color) = (1, 0.72, 0.38, 1)
        _OutlineWidth ("Outline Width (object space)", Float) = 0.0022
        _OutlineColor ("Outline Colour", Color) = (0.2, 0.11, 0.06, 1)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent-10" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float _Alpha;
            float _Phase;
            float _Glow;
            float4 _GlowColor;
            float _OutlineWidth;
            float4 _OutlineColor;
        CBUFFER_END

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float2 uv : TEXCOORD0;
            float2 uv1 : TEXCOORD1;
            float4 color : COLOR;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv : TEXCOORD0;
            float2 uv1 : TEXCOORD1;
            float4 color : COLOR;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        float Flicker()
        {
            float t = _Time.y;
            return 1.0 + 0.05 * sin(t * 11.0 + _Phase) + 0.03 * sin(t * 17.3 + _Phase * 2.1) + 0.02 * sin(t * 4.7 + _Phase * 0.7);
        }
        ENDHLSL

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite On
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 p = v.positionOS.xyz + normalize(v.normalOS) * (_OutlineWidth * v.color.a);
                o.positionCS = TransformObjectToHClip(p);
                o.color = v.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                clip(i.color.a - 0.01);
                return half4(_OutlineColor.rgb, _Alpha);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Lantern"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.uv1 = v.uv1;
                o.color = v.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float kind = i.uv.x, h = saturate(i.uv.y);
                float f = Flicker();
                float3 rgb = i.color.rgb;
                if (kind > 0.5 && kind < 1.5)
                {
                    // Paper lit from inside: brighter and warmer toward the flame at the bottom, two soft bands (toon).
                    float inner = lerp(1.0, 0.35, smoothstep(0.0, 1.0, h));
                    inner = floor(inner * 3.0 + 0.5) / 3.0 * 0.6 + inner * 0.4;
                    rgb = rgb * (0.92 + 0.22 * inner * f * _Glow) + _GlowColor.rgb * (0.22 * inner * f * _Glow);
                }
                else if (kind >= 1.5 && kind < 2.5)
                    rgb = lerp(_GlowColor.rgb, float3(1.0, 0.93, 0.7), 0.5) * (1.15 * f);
                else if (kind >= 2.5 && kind < 3.5)
                    rgb = float3(1.0, 0.95, 0.65) * (1.3 * f);
                else if (kind >= 3.5)
                {
                    half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv1);
                    rgb = lerp(rgb, c.rgb, c.a);
                }
                return half4(rgb, _Alpha);
            }
            ENDHLSL
        }
    }
}
