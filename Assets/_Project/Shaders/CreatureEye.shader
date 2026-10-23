// Creature eye: dark sclera, iris with a vertical gradient, pupil and two highlights, painted on the front of a
// flattened sphere. _Look (-1..1) slides iris and pupil toward the player; blinking scales the eye in code.
// Port of eyeMat() in Docs/Reference/creature-diorama-v2.html. Colours are linear (CreatureView sets them).
Shader "Diorama/Creature Eye"
{
    Properties
    {
        _IrisColor ("Iris", Color) = (0.35, 0.2, 0.1, 1)
        _Look ("Look (xy, -1..1)", Vector) = (0, 0, 0, 0)
        [HideInInspector] _WindowClip ("Island Window Clip", Float) = 0
        [HideInInspector] _WindowOrigin ("Island Window Origin", Vector) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "DioramaWindow.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _IrisColor;
            float4 _Look;
            float  _WindowClip;
            float4 _WindowOrigin; // w = 1: sink with the creature root instead of the eye's own origin
        CBUFFER_END

        struct Attributes
        {
            float4 positionOS : POSITION;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionOS : TEXCOORD0;
            float3 islandPos  : TEXCOORD1;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
        };

        Varyings vert(Attributes v)
        {
            Varyings o = (Varyings)0;
            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_TRANSFER_INSTANCE_ID(v, o);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
            o.positionOS = v.positionOS.xyz;
            float3 origin = _WindowOrigin.w > 0.5 ? _WindowOrigin.xyz : UNITY_MATRIX_M._m03_m13_m23;
            float3 ws = DioramaWindowVertex(TransformObjectToWorld(v.positionOS.xyz), _WindowClip, origin, o.islandPos);
            o.positionCS = TransformWorldToHClip(ws);
            return o;
        }
        ENDHLSL

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

            float4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                DioramaWindowClip(i.islandPos, _WindowClip);

                float2 q = i.positionOS.xy;
                float2 lq = q - _Look.xy * 0.28;
                float d = length(lq);
                float3 c = float3(0.006, 0.005, 0.01);
                float3 iris = lerp(_IrisColor.rgb * 0.3, _IrisColor.rgb * 1.8, smoothstep(0.45, -0.65, lq.y));
                c = lerp(c, iris, 1.0 - smoothstep(0.64, 0.69, d));
                c = lerp(c, float3(0.002, 0.001, 0.004), 1.0 - smoothstep(0.3, 0.35, d));
                // Highlights stay put (light from the upper left as you look at it); x is mirrored from three.js.
                float2 hq = float2(-q.x, q.y);
                float h1 = 1.0 - smoothstep(0.2, 0.235, length(hq - float2(-0.3, 0.36)));
                float h2 = 1.0 - smoothstep(0.085, 0.115, length(hq - float2(0.27, -0.3)));
                c = lerp(c, float3(1.0, 1.0, 1.0), max(h1, h2));
                return float4(c, 1.0);
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

            half frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                DioramaWindowClip(i.islandPos, _WindowClip);
                return 0;
            }
            ENDHLSL
        }
    }
}
