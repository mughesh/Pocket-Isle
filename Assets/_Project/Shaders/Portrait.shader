// Step 14h: the Journal's portrait grid, one mesh: atlas x vertex colour (white = caught, grey = seen, black = an unseen
// silhouette). Unlit, alpha blended, no depth write.
// Step 14j: uv1.x = 1 draws a flat silhouette: the vertex colour only, cut out by the portrait's alpha (no shading or details).
Shader "Diorama/Portrait"
{
    Properties
    {
        _MainTex ("Portrait atlas", 2D) = "white" {}
        _Tint ("Tint", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent+20" }

        Pass
        {
            Name "Portrait"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Tint;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float silhouette : TEXCOORD1;
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
                o.silhouette = v.uv1.x;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half3 rgb = lerp(c.rgb * i.color.rgb, i.color.rgb, saturate(i.silhouette));
                // A silhouette is a hard cut-out (no soft painted edges bleeding the colours through).
                half a = lerp(c.a, smoothstep(0.35, 0.6, c.a), saturate(i.silhouette));
                return half4(rgb, a * i.color.a) * _Tint;
            }
            ENDHLSL
        }
    }
}
