// The tutorial's ghost hand (step 8b, GhostHands): a translucent pale-blue hand with a bright rim and a faint glow, so it
// reads against passthrough without looking like the gold "this is the target" focus. Premultiplied alpha (writes alpha
// for the passthrough composite). Vertices are pulled toward the eye by _DepthPull (m), so the island's rim and tiles
// never hide it when it reaches in. One pass: one draw per hand. Also used for the gaze dot.
Shader "Diorama/Ghost Hand"
{
    Properties
    {
        _Fill ("Fill Colour", Color) = (0.62, 0.8, 1, 1)
        _Rim ("Rim Colour", Color) = (0.88, 0.96, 1, 1)
        _FillAlpha ("Fill Alpha", Range(0, 1)) = 0.2
        _RimPower ("Rim Power", Range(0.5, 6)) = 2.2
        _Glow ("Glow", Range(0, 2)) = 0.6
        _Alpha ("Alpha", Range(0, 1)) = 1
        _DepthPull ("Depth Pull (m)", Range(0, 0.1)) = 0.03
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent+14" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Ghost"
            Tags { "LightMode" = "UniversalForward" }
            Blend One OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Fill;
                float4 _Rim;
                float _FillAlpha;
                float _RimPower;
                float _Glow;
                float _Alpha;
                float _DepthPull;
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
                float3 normalWS : TEXCOORD0;
                float3 viewWS : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 posWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 view = GetWorldSpaceNormalizeViewDir(posWS);
                o.viewWS = view;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.positionCS = TransformWorldToHClip(posWS + view * _DepthPull);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 n = normalize(i.normalWS);
                float3 v = normalize(i.viewWS);
                float fres = pow(1.0 - saturate(dot(n, v)), _RimPower);
                float a = saturate(_FillAlpha + fres * (1.0 - _FillAlpha)) * _Alpha;
                float3 col = lerp(_Fill.rgb, _Rim.rgb, fres) * (1.0 + _Glow * fres);
                return half4(col * a, a);
            }
            ENDHLSL
        }
    }
}
