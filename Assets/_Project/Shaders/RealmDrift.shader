// Small drifting things over the island that show the realm's state (RealmDrift): rising embers when the element
// flows (restored), faint falling leaves when it doesn't. One mesh of camera-facing quads, one per slot of a 16 x 16
// tile grid that follows the view. Every quad is hashed from its WORLD tile, so it stays on its tile while the map
// scrolls and never pops when slots change tiles. A weight texture (one texel per world tile) decides which tiles have
// one; _Amount (set by RealmDrift from the restoration) fades the count. Clipped by the island window.
//   _Mode 0 = ember: rises with a wobble, round glowing dot, additive.
//   _Mode 1 = leaf: falls from canopy height with a sway and a tumble, small oval, alpha blended.
Shader "Diorama/Realm Drift"
{
    Properties
    {
        [Enum(Ember,0,Leaf,1)] _Mode ("Mode", Float) = 0
        _Color ("Colour", Color) = (1, 0.62, 0.22, 1)
        _Core ("Core / Second Colour", Color) = (1, 0.9, 0.55, 1)
        _Size ("Size (m at island scale 1)", Float) = 0.004
        _Travel ("Rise or fall min / max (m at island scale 1)", Vector) = (0.15, 0.25, 0, 0)
        _Period ("Life min / max (s)", Vector) = (3, 5, 0, 0)
        _Opacity ("Opacity", Range(0, 1)) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" }

        Pass
        {
            Name "Drift"
            Tags { "LightMode" = "UniversalForward" }
            Blend [_SrcBlend] [_DstBlend], One OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "DioramaWindow.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Mode;
                float4 _Color;
                float4 _Core;
                float _Size;
                float4 _Travel;
                float4 _Period;
                float _Opacity;
                float _SrcBlend;
                float _DstBlend;
            CBUFFER_END

            // Set by RealmDrift every frame (property block).
            float4 _DriftGrid;   // xy = world tile of slot (0, 0), zw = weight texture size (columns, rows)
            float4 _DriftMap;    // x = tile size, y = cap height (map metres), z = island scale, w = amount 0..1
            float4x4 _DioramaMapToWorld;
            TEXTURE2D(_DriftWeights);
            SAMPLER(sampler_DriftWeights);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 corner : TEXCOORD0; // -1..1
                float2 slot : TEXCOORD1;   // 0..15
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 corner : TEXCOORD0;
                float3 islandPos : TEXCOORD1;
                float alpha : TEXCOORD2;
                float tint : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float Hash(float2 p, float salt)
            {
                return frac(sin(dot(p + salt, float2(12.9898, 78.233))) * 43758.5453);
            }

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float2 tile = _DriftGrid.xy + v.slot;
                bool inside = all(tile >= 0.0) && all(tile < _DriftGrid.zw);
                float weight = inside ? SAMPLE_TEXTURE2D_LOD(_DriftWeights, sampler_DriftWeights, (tile + 0.5) / _DriftGrid.zw, 0).r : 0.0;
                // Low-hash tiles show first, so the count grows smoothly with the amount.
                float on = step(Hash(tile, 0.0), weight * saturate(_DriftMap.w));

                bool leaf = _Mode > 0.5;
                float t = _Time.y;
                float period = lerp(_Period.x, _Period.y, Hash(tile, 3.1));
                float phase = frac(t / period + Hash(tile, 5.7));
                float travel = lerp(_Travel.x, _Travel.y, Hash(tile, 7.3));
                float2 jitter = float2(Hash(tile, 11.0), Hash(tile, 13.0)) - 0.5;
                float seed = Hash(tile, 17.0) * 6.283;
                float height, swayX, swayZ;
                if (leaf)
                {
                    // From the canopy down to the ground, swinging side to side like a falling leaf.
                    height = travel * (1.0 - phase);
                    swayX = sin(t * 1.9 + seed) * 0.014;
                    swayZ = cos(t * 1.3 + seed) * 0.006;
                }
                else
                {
                    height = travel * phase;
                    swayX = sin(t * 2.3 + seed) * 0.012 * phase;
                    swayZ = cos(t * 1.7 + seed * 1.3) * 0.008 * phase;
                }
                // Map space: columns run toward -X, rows toward +Z (IslandBuilder.MapPoint).
                float ts = _DriftMap.x;
                float3 mapPos = float3(-(tile.x + jitter.x * 0.8) * ts + swayX, _DriftMap.y + 0.004 + height, (tile.y + jitter.y * 0.8) * ts + swayZ);
                float3 world = mul(_DioramaMapToWorld, float4(mapPos, 1.0)).xyz;

                float size = _Size * _DriftMap.z * lerp(0.75, 1.25, Hash(tile, 23.0)) * on;
                float2 c = v.corner;
                if (leaf)
                {
                    // An oval that tumbles: spins in the view plane and flips (squashes) as it falls.
                    float a = t * lerp(1.5, 3.0, Hash(tile, 29.0)) + seed;
                    c = float2(c.x * 0.55 * (0.35 + 0.65 * abs(cos(t * 2.1 + seed))), c.y);
                    c = float2(c.x * cos(a) - c.y * sin(a), c.x * sin(a) + c.y * cos(a));
                }
                float3 view = TransformWorldToView(world);
                view.xy += c * size;
                o.positionCS = TransformWViewToHClip(view);
                o.islandPos = mul(_DioramaWorldToIsland, float4(world, 1.0)).xyz;
                o.corner = v.corner;
                o.tint = Hash(tile, 31.0);
                o.alpha = on * smoothstep(0.0, leaf ? 0.15 : 0.12, phase) * (1.0 - smoothstep(leaf ? 0.8 : 0.55, 1.0, phase));
                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                if (i.alpha <= 0.001) discard;
                DioramaWindowClip(i.islandPos, 1.0);
                float d = length(i.corner);
                if (_Mode > 0.5)
                {
                    // Leaf: solid oval with a soft edge, colour picked between the two per leaf.
                    float a = (1.0 - smoothstep(0.75, 1.0, d)) * i.alpha * _Opacity;
                    float3 col = lerp(_Color.rgb, _Core.rgb, i.tint) * (0.85 + 0.15 * i.corner.y);
                    return float4(col, a);
                }
                float g = saturate(1.0 - d);
                g *= g * i.alpha * _Opacity;
                float3 glow = lerp(_Color.rgb, _Core.rgb, saturate(1.0 - d * 2.0)) * g * 1.6;
                return float4(glow, g);
            }
            ENDHLSL
        }
    }
}
