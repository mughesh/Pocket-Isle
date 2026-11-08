// Step 21: the swipe demo's stroke arrow (GhostHands): an unlit glowing ribbon. Vertex colour carries the gradient (a
// transparent tail brightening to the head, a soft wide glow under it); _Color tints it, _Alpha fades it. Premultiplied,
// no depth write, drawn over the ghost hand's translucency.
Shader "Diorama/Stroke Arrow"
{
    Properties
    {
        _Color ("Colour", Color) = (1, 0.82, 0.35, 1)
        _Glow ("Glow", Range(0, 3)) = 1.4
        _Alpha ("Alpha", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+20" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Blend One OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Glow;
                float _Alpha;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.color = v.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // rgb = brightness toward white (the core), a = coverage.
                float a = saturate(i.color.a * _Alpha);
                float3 col = lerp(_Color.rgb, float3(1, 0.97, 0.85), i.color.r) * (1.0 + _Glow * i.color.g);
                return half4(col * a, a);
            }
            ENDHLSL
        }
    }
}
