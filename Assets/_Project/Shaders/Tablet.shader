// Step 14f: mossy carved stone tablets, every button in the game (rune boards, battle runes). One shared procedural
// mesh (TabletKit) stretched per instance: each vertex is an anchor (uv1, -1..1 across the slab) plus a fixed offset
// in metres, so corners, bevels, moss and the inlay keep their size on any tablet (_Size, board-local metres).
// Vertex colour alpha picks the part: 0 stone, 0.25 moss, 0.5 inlay groove, 0.75 rim (the outline, gold when looked
// at), 1 halo (dark shadow + a faint warm glow round it, so the slab never blends into a white wall or a dark room).
// Toon banded with its own light (board space): looks the same in any room. Per instance: _Size, _State (hover, dim,
// fill, press glow), _Inlay (rgb, a = how much the inlay shows its colour: primary gold, element, a lit toggle).
// Queued with the HUD before other transparents (labels draw on top); writes depth so the slab hides its own back.
Shader "Diorama/Tablet"
{
    Properties
    {
        _Stone ("Stone", Color) = (0.30, 0.33, 0.29, 1)
        _StoneLight ("Stone Lit Edge", Color) = (0.62, 0.64, 0.55, 1)
        _Moss ("Moss", Color) = (0.36, 0.55, 0.22, 1)
        _Groove ("Inlay Groove", Color) = (0.17, 0.18, 0.16, 1)
        _Rim ("Rim", Color) = (0.06, 0.05, 0.04, 1)
        _Gold ("Gold (focus, fill)", Color) = (1, 0.82, 0.4, 1)
        _Halo ("Halo Glow", Color) = (1, 0.93, 0.75, 1)
        _HaloDark ("Halo Shadow Alpha", Range(0, 1)) = 0.5
        _HaloLight ("Halo Glow Alpha", Range(0, 1)) = 0.42
        _Corner ("Corner Radius (m)", Float) = 0.007
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent-5" }

        Pass
        {
            Name "Tablet"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Stone, _StoneLight, _Moss, _Groove, _Rim, _Gold, _Halo;
                float _HaloDark, _HaloLight, _Corner;
            CBUFFER_END

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Size)
                UNITY_DEFINE_INSTANCED_PROP(float4, _State)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Inlay)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
                float2 anchor : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float3 normalOS : TEXCOORD0;
                float2 face : TEXCOORD1; // x: 0..1 across, y: 0..1 up the slab
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float2 size = UNITY_ACCESS_INSTANCED_PROP(Props, _Size).xy;
                float2 hs = max(size * 0.5 - _Corner, 0.0);
                float3 p = v.positionOS.xyz;
                p.xy += v.anchor * hs;
                o.positionCS = TransformObjectToHClip(p);
                o.color = v.color;
                o.normalOS = v.normalOS;
                o.face = p.xy / max(size, 1e-4) + 0.5;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float4 state = UNITY_ACCESS_INSTANCED_PROP(Props, _State);
                float4 inlay = UNITY_ACCESS_INSTANCED_PROP(Props, _Inlay);
                float hover = state.x, dim = state.y, fill = state.z, glow = state.w;
                float part = i.color.a;

                // Halo: dark shadow close in (red channel), a faint warm glow further out (green channel).
                if (part > 0.875)
                {
                    float dark = i.color.r, light = i.color.g;
                    float a = dark * _HaloDark + light * _HaloLight * (1.0 + hover);
                    float3 c = lerp(_Halo.rgb, float3(0, 0, 0), dark / max(dark + light, 1e-4));
                    return half4(c, a * (1.0 - 0.5 * dim));
                }

                // Toon light in board space (front is -z): up, a little left, toward the viewer.
                float3 n = normalize(i.normalOS);
                float ndl = dot(n, normalize(float3(-0.35, 0.75, -0.55)));
                float band = ndl > 0.55 ? 1.0 : ndl > 0.1 ? 0.82 : 0.62;

                float3 col;
                if (part < 0.125)
                {
                    // Stone: the face dark (labels read on it), bevels catch the light.
                    float edge = saturate(1.0 + n.z * 1.25);
                    col = lerp(_Stone.rgb, _StoneLight.rgb, edge * saturate(ndl + 0.2)) * i.color.rgb * band;
                    // Hold runes fill gold from the bottom up.
                    float filled = step(i.face.y, fill) * step(0.001, fill);
                    col = lerp(col, _Gold.rgb * 0.8, filled * 0.75);
                }
                else if (part < 0.375) col = _Moss.rgb * i.color.rgb * band;
                else if (part < 0.625) col = lerp(_Groove.rgb, inlay.rgb, inlay.a) * lerp(1.0, band, 1.0 - inlay.a * 0.6) + inlay.rgb * inlay.a * 0.25 * (hover + glow);
                else col = lerp(_Rim.rgb, _Gold.rgb * 1.35, saturate(hover + glow * 0.7));

                // Looked at: the stone brightens a touch; disabled: grey and dim.
                col *= 1.0 + 0.18 * hover;
                float g = dot(col, float3(0.3, 0.59, 0.11));
                col = lerp(col, g.xxx * 0.6, dim * 0.7);
                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
