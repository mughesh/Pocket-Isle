// The island's grab frame (IslandGrabFrame): a soft glowing ribbon around the rocky base, like the grab frame on
// Meta's panels. uv.y runs across the ribbon (0..1, soft both edges); the vertex colour carries the per-point glow
// (rgb tint, a brightness) so the side nearest the hand is brighter.
Shader "Diorama/Grab Frame"
{
    Properties
    {
        _Intensity ("Intensity", Range(0, 3)) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent+10" }

        Pass
        {
            Name "Frame"
            Tags { "LightMode" = "UniversalForward" }
            Blend One One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Intensity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                // Bright thin core, soft glow to both edges.
                float x = abs(i.uv.y * 2.0 - 1.0);
                float core = 1.0 - smoothstep(0.0, 0.35, x);
                float glow = 1.0 - x;
                glow *= glow;
                float a = (core * 0.7 + glow * 0.5) * i.color.a * _Intensity;
                return half4(i.color.rgb * a, a);
            }
            ENDHLSL
        }
    }
}
