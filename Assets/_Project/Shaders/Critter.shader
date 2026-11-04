// Overnight Nov 5: the little winged things of AmbientLife (butterflies, dragonflies, birds). One mesh (a body and two
// wings), drawn instanced in one call; each instance has its own wing colour and flap (rate, depth, phase, wing shape).
// The wings flap in the vertex shader (uv.x = wing side -1 / 0 body / +1, uv.y = how far out on the wing). Unlit
// toon-ish: the colour, a little darker underneath and toward the body. Clipped by the island window like the props.
Shader "Diorama/Critter"
{
    Properties
    {
        _BodyColor ("Body", Color) = (0.22, 0.18, 0.16, 1)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry+10" }

        Pass
        {
            Name "Critter"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "DioramaWindow.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BodyColor;
            CBUFFER_END

            UNITY_INSTANCING_BUFFER_START(Critter)
                UNITY_DEFINE_INSTANCED_PROP(float4, _WingColor)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Flap) // x rate (Hz), y depth (rad), z phase, w wing length scale
            UNITY_INSTANCING_BUFFER_END(Critter)

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 wing : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 islandPos : TEXCOORD0;
                float2 wing : TEXCOORD1;
                float4 color : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float4 flap = UNITY_ACCESS_INSTANCED_PROP(Critter, _Flap);
                float3 p = v.positionOS.xyz;
                float side = v.wing.x;
                if (abs(side) > 0.5)
                {
                    // Longer / shorter wings per kind, then up and down about the body's long axis (z).
                    p.x *= flap.w;
                    float a = sin(_Time.y * flap.x * 6.2832 + flap.z) * flap.y + 0.25;
                    float s = sin(a), c = cos(a);
                    float x = p.x;
                    p.x = x * c;
                    p.y += abs(x) * s;
                }
                float3 world = TransformObjectToWorld(p);
                o.positionCS = TransformWorldToHClip(world);
                o.islandPos = mul(_DioramaWorldToIsland, float4(world, 1.0)).xyz;
                o.wing = v.wing;
                o.color = abs(side) > 0.5 ? UNITY_ACCESS_INSTANCED_PROP(Critter, _WingColor) : _BodyColor;
                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                DioramaWindowClip(i.islandPos, 1.0);
                // Wings lighter at the tips, a thin dark edge band near the body.
                float out_ = saturate(i.wing.y);
                float3 col = i.color.rgb * lerp(0.75, 1.1, out_);
                return float4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
