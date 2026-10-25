// The Lantern Spirit's light (FocusHighlight). Step 8c replaced the long beam to the target (it crossed the scene) with
// two additive pieces sharing this shader:
//   _Radial 0: a short, faint cone just below the lantern. uv.x across (0..1), uv.y along (0 = lantern), fading out.
//   _Radial 1: a soft gold halo / light pool on the hint target: a camera-facing disc, bright ring, soft falloff.
Shader "Diorama/Lantern Beam"
{
    Properties
    {
        _Color ("Edge Colour", Color) = (1, 0.72, 0.28, 1)
        _Core ("Core Colour", Color) = (1, 0.93, 0.7, 1)
        _Intensity ("Intensity", Range(0, 3)) = 1
        _Radial ("Halo (1) or cone (0)", Range(0, 1)) = 0
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
                float _Radial;
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
                if (_Radial > 0.5)
                {
                    // Halo: soft disc, a brighter ring around the target's edge, a slow breathing pulse.
                    float d = length(i.uv * 2.0 - 1.0);
                    float fall = saturate(1.0 - d);
                    fall = fall * fall;
                    float ring = exp(-pow((d - 0.62) / 0.16, 2.0));
                    float pulse = 0.85 + 0.15 * sin(t * 3.0);
                    float h = (fall * 0.55 + ring * 0.45) * pulse * _Intensity * 0.6;
                    float3 hc = lerp(_Color.rgb, _Core.rgb, fall);
                    return half4(hc * h, h);
                }
                float x = abs(i.uv.x * 2.0 - 1.0);
                float y = i.uv.y;
                float across = saturate(1.0 - x);
                across = across * across * (3.0 - 2.0 * across);
                // Bright at the lantern, gone by the far end: never reaches anything.
                float along = smoothstep(0.0, 0.1, y) * (1.0 - smoothstep(0.15, 1.0, y));
                float shimmer = 0.8 + 0.2 * sin(y * 20.0 - t * 4.0);
                float a = across * along * shimmer * _Intensity * 0.4;
                float3 col = lerp(_Color.rgb, _Core.rgb, across * across);
                return half4(col * a, a);
            }
            ENDHLSL
        }
    }
}
