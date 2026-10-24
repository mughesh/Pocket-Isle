// The Lantern Spirit's beam (step 7c, FocusHighlight): a thin additive cone of warm light from the Spirit's lantern to
// the hint target. uv.x runs across the beam (0..1), uv.y along it (0 = lantern, 1 = target). Soft edges, fades in
// from the lantern, a gentle shimmer drifting toward the target.
Shader "Diorama/Lantern Beam"
{
    Properties
    {
        _Color ("Edge Colour", Color) = (1, 0.72, 0.28, 1)
        _Core ("Core Colour", Color) = (1, 0.93, 0.7, 1)
        _Intensity ("Intensity", Range(0, 3)) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent+12" }

        Pass
        {
            Name "Beam"
            Tags { "LightMode" = "UniversalForward" }
            Blend One One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _Core;
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

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float t = _Time.y;
                float x = abs(i.uv.x * 2.0 - 1.0);
                float y = i.uv.y;
                float across = saturate(1.0 - x);
                across = across * across * (3.0 - 2.0 * across);
                float along = smoothstep(0.0, 0.18, y) * (1.0 - 0.35 * smoothstep(0.8, 1.0, y));
                float shimmer = 0.78 + 0.22 * sin(y * 26.0 - t * 4.0) * sin(y * 7.0 + t * 1.7);
                float a = across * along * shimmer * _Intensity * 0.55;
                float3 col = lerp(_Color.rgb, _Core.rgb, across * across);
                return half4(col * a, a);
            }
            ENDHLSL
        }
    }
}
