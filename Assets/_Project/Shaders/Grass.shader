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
            float  _RealmStill; // step 10a: 1 = a still realm (Galewick unrestored): only a tiny idle sway
            // Step 10b (GustField): a gust front sweeping across the island. Map space: xy = origin, zw = unit direction;
            // front: x = distance along it, y = band width, z = strength (0 = none); dir: world direction it blows.
            float4 _DioramaGust;
            float4 _DioramaGustFront;
            float4 _DioramaGustDir;
            // Nov 3: a second band (a sweep back the other way while the first still crosses).
            float4 _DioramaGust2;
            float4 _DioramaGustFront2;
            float4 _DioramaGustDir2;

            // How much a gust band bends the blade at mapXZ (0..strength).
            float GustAt(float2 mapXZ, float4 gust, float4 front)
            {
                if (front.z <= 0.001) return 0.0;
                float along = dot(mapXZ - gust.xy, gust.zw) - front.x;
                float behind = saturate(-along / max(1e-4, front.y * 3.0)); // a short wake behind the band
                float band = exp(-along * along / max(1e-6, front.y * front.y));
                return front.z * max(band, behind * exp(-behind * 2.0) * 0.6 * step(along, 0.0));
            }

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
                float  gust       : TEXCOORD5;
                float3 islandPos  : TEXCOORD3;
                float2 mapXZ      : TEXCOORD4;
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
                sway *= 1.0 - saturate(_RealmStill);
                world.x += sway * 0.22 * k;
                world.z += sway * 0.1 * k;

                float2 d = base.xz - _DioramaGrassPush.xz;
                float push = _DioramaGrassPush.w * smoothstep(_PushRadius * scale, 0.0, length(d));
                world.xz += normalize(d + 0.0001) * push * 0.7 * k;
                world.y -= push * 0.35 * k;

                // Step 10b: the gust front bends blades the way it blows and flattens them as it passes.
                float gust1 = GustAt(mapBase.xz, _DioramaGust, _DioramaGustFront);
                float gust2 = GustAt(mapBase.xz, _DioramaGust2, _DioramaGustFront2);
                float3 bend = _DioramaGustDir.xyz * gust1 + _DioramaGustDir2.xyz * gust2;
                float bendLen = length(bend);
                if (bendLen > 1.0) bend /= bendLen; // two bands crossing don't bend a blade twice as far
                float gust = saturate(gust1 + gust2);
                world.xyz += bend * 0.95 * k;
                world.y -= gust * 0.45 * k;

                // Battle clearing: blades bend away from its centre and shrink to a fifth of their height.
                float clear = _WindowClip > 0.5 ? DioramaClearing(mapBase.xz) : 0.0;
                if (clear > 0.0)
                {
                    float2 away = base.xz - _DioramaClearingWorld.xz;
                    world = base + (world - base) * lerp(1.0, 0.2, clear);
                    world.xz += normalize(away + 0.0001) * clear * 0.5 * k;
                }

                // Step 14k: always see the explorer in tall grass: blades within about a tile of it, on the player's
                // side (and at its feet), lie down to a fifth of their height, leaning away from it.
                float see = _WindowClip > 0.5 ? DioramaGrassSee(base) : 0.0;
                if (see > 0.0)
                {
                    float2 away = base.xz - _DioramaGrassSee.xz;
                    world = base + (world - base) * lerp(1.0, 0.2, see);
                    world.xz += normalize(away + 0.0001) * see * 0.35 * k;
                }

                world = DioramaWindowVertex(world, _WindowClip, UNITY_MATRIX_M._m03_m13_m23, o.islandPos);
                o.positionCS = TransformWorldToHClip(world);
                o.h = h;
                o.tall = step(_TallThreshold * scale, height) * (1.0 - clear);
                o.mapXZ = mapBase.xz;
                o.variation = DioramaStableHash(mapBase.xz);
                o.gust = gust;
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
                c += i.gust * smoothstep(0.2, 1.0, i.h) * float3(0.32, 0.3, 0.22); // the bright ripple band
                if (_WindowClip > 0.5) c = DioramaFocus(c, i.mapXZ);
                return float4(c, 1.0);
            }
            ENDHLSL
        }
    }
}
