// Inverted-hull outline: the mesh is pushed out along its normals and only back faces are drawn.
// Uses the same material layout as Diorama/Toon (ToonCommon.hlsl), so breathe / jiggle match the body.
// For flat-shaded meshes, give this pass a copy with smoothed normals or the hull cracks at the edges.
Shader "Diorama/Toon Outline"
{
    Properties
    {
        _OutlineWidth ("Outline Width (object space)", Float) = 0.035
        _OutlineColor ("Outline Colour", Color) = (0.1, 0.1, 0.1, 1)

        [Header(Motion (match the body material))]
        _Breathe ("Breathe", Float) = 0
        _Jiggle ("Jiggle", Float) = 0

        [HideInInspector] _WindowClip ("Island Window Clip", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite On

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
                float3 islandPos  : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 p = ToonDeform(v.positionOS.xyz, v.normalOS) + v.normalOS * _OutlineWidth;
                o.positionCS = TransformWorldToHClip(DioramaWindowVertex(TransformObjectToWorld(p), _WindowClip, o.islandPos));
                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                DioramaWindowClip(i.islandPos, _WindowClip);
                return float4(_OutlineColor.rgb, 1.0);
            }
            ENDHLSL
        }
    }
}
