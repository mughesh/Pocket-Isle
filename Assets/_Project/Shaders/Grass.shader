// Instanced grass blades: wind sway, push-away from a world point, base-to-tip colour,
// short vs tall colours. Port of grassMat from creature-diorama-v2.html.
// Blade height is read from the instance matrix (Y axis length), so one mesh serves both kinds.
// Globals (set from C#, see IslandBuilder):
//   _DioramaGrassPush  xyz = world point, w = strength
//   _DioramaScale      island scale x zoom, keeps the tall test and push radius in island units
// Island window sinking / clipping and map-space wind: DioramaWindow.hlsl.
Shader "Diorama/Grass"
{
    Properties
    {
        _ShortBase ("Short Base", Color) = (0.247, 0.478, 0.208, 1)
        _ShortTip ("Short Tip", Color) = (0.714, 0.878, 0.478, 1)
        _TallBase ("Tall Base", Color) = (0.173, 0.384, 0.188, 1)
        _TallTip ("Tall Tip", Color) = (0.549, 0.796, 0.361, 1)
        _TallThreshold ("Tall Above Height (m)", Float) = 0.04
        _PushRadius ("Push Radius (m)", Float) = 0.1

        [HideInInspector] _WindowClip ("Island Window Clip", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            Name "Grass"
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
                float4 _ShortBase;
                float4 _ShortTip;
                float4 _TallBase;
                float4 _TallTip;
                float  _TallThreshold;
                float  _PushRadius;
                float  _WindowClip;
            CBUFFER_END

            float4 _DioramaGrassPush;
            float  _DioramaScale;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float  h          : TEXCOORD0;
                float  tall       : TEXCOORD1;
                float  variation  : TEXCOORD2;
                float3 islandPos  : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float h = v.uv.y;
                float3 world = TransformObjectToWorld(v.positionOS.xyz);
                float3 base = UNITY_MATRIX_M._m03_m13_m23;
                float height = length(float3(UNITY_MATRIX_M._m01, UNITY_MATRIX_M._m11, UNITY_MATRIX_M._m21));
                float scale = _DioramaScale > 0.0 ? _DioramaScale : 1.0;
                float3 mapBase = DioramaMapPoint(base, _WindowClip); // wind phase stays with the blade while the map scrolls

                float t = _Time.y;
                float sway = sin(t * 1.7 + mapBase.x * 17.0 + mapBase.z * 11.0) * 0.6
                           + sin(t * 2.9 + mapBase.x * 6.0 - mapBase.z * 9.0) * 0.3;
                float k = h * h * height;
                world.x += sway * 0.22 * k;
                world.z += sway * 0.1 * k;

                float2 d = base.xz - _DioramaGrassPush.xz;
                float push = _DioramaGrassPush.w * smoothstep(_PushRadius * scale, 0.0, length(d));
                world.xz += normalize(d + 0.0001) * push * 0.7 * k;
                world.y -= push * 0.35 * k;

                world = DioramaWindowVertex(world, _WindowClip, o.islandPos);
                o.positionCS = TransformWorldToHClip(world);
                o.h = h;
                o.tall = step(_TallThreshold * scale, height);
                o.variation = frac(sin(dot(mapBase.xz, float2(127.1, 311.7))) * 43758.5453);
                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                DioramaWindowClip(i.islandPos, _WindowClip);
                float3 baseCol = lerp(_ShortBase.rgb, _TallBase.rgb, i.tall);
                float3 tipCol = lerp(_ShortTip.rgb, _TallTip.rgb, i.tall);
                float3 c = lerp(baseCol, tipCol, smoothstep(0.0, 1.0, i.h));
                c *= 0.88 + i.variation * 0.24;
                return float4(c, 1.0);
            }
            ENDHLSL
        }
    }
}
