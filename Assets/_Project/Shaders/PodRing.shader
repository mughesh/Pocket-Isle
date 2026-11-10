// Step 29 C: the HP ring round a rim pod, always shown. A band round the quad's middle, filled clockwise from the top by
// _Fill in _FillColor over a dark track, with a thin dark outline so it reads on rock and in the room. _Alpha fades it.
Shader "Diorama/Pod Ring"
{
    Properties
    {
        _FillColor ("Fill", Color) = (0.44, 0.82, 0.42, 1)
        _TrackColor ("Track", Color) = (0.1, 0.09, 0.08, 0.75)
        _Outline ("Outline", Color) = (0.05, 0.04, 0.03, 0.9)
        _Fill ("Fill", Range(0, 1)) = 1
        _Inner ("Inner radius (of half size)", Range(0, 1)) = 0.76
        _Outer ("Outer radius (of half size)", Range(0, 1)) = 0.94
        _Alpha ("Alpha", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent+5" }

        Pass
        {
            Name "PodRing"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _FillColor;
                float4 _TrackColor;
                float4 _Outline;
                float _Fill;
                float _Inner;
                float _Outer;
                float _Alpha;
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
                float2 p = i.uv * 2.0 - 1.0;
                float r = length(p);
                float aa = max(fwidth(r), 1e-3);
                float mid = (_Inner + _Outer) * 0.5, half_ = (_Outer - _Inner) * 0.5;
                float d = abs(r - mid) - half_;                       // band distance (negative inside)
                float band = saturate(0.5 - d / aa);
                float outline = saturate(0.5 - (d - 0.05) / aa) * (1.0 - band);
                // Clockwise from the top: 0 at 12 o'clock, 1 all the way round.
                float t = frac(atan2(p.x, p.y) / 6.2831853 + 1.0);
                float filled = saturate((_Fill - t) / max(fwidth(t), 1e-3) + 0.5) * step(0.001, _Fill);
                float4 col = lerp(_TrackColor, _FillColor, filled);
                float a = band * col.a + outline * _Outline.a;
                float3 rgb = (col.rgb * band * col.a + _Outline.rgb * outline * _Outline.a) / max(a, 1e-4);
                return half4(rgb, a * _Alpha);
            }
            ENDHLSL
        }
    }
}
