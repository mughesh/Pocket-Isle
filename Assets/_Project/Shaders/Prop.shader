// Environment props (PropRecipe, baked by Tools > Pocket Isle > Props): banded toon lighting like Diorama/Toon, with
// the colour picked per vertex from a 6-slot palette (uv0.x) that blends from the unrestored to the restored colours
// by the global _RealmRestored. Glows scale with it too, so kilns and lanterns go cold. Motion per piece (uv0.y):
// sway, flicker (glow + wobble) or steam (puffs in a loop around the piece centre, uv1). The palette comes from a
// property block per prop type, so each type is one instanced draw plus its outline pass (same material).
// Island window sinking / clipping and the battle clearing: DioramaWindow.hlsl.
Shader "Diorama/Prop"
{
    Properties
    {
        [Header(Restored palette (Clay Wood Stone Leaf Glow Accent))]
        _Slot0 ("Clay", Color) = (0.76, 0.46, 0.26, 1)
        _Slot1 ("Wood", Color) = (0.42, 0.29, 0.2, 1)
        _Slot2 ("Stone", Color) = (0.65, 0.62, 0.57, 1)
        _Slot3 ("Leaf", Color) = (0.79, 0.31, 0.16, 1)
        _Slot4 ("Glow", Color) = (1, 0.7, 0.28, 1)
        _Slot5 ("Accent", Color) = (0.94, 0.87, 0.75, 1)

        [Header(Unrestored palette)]
        _Ash0 ("Clay", Color) = (0.56, 0.52, 0.49, 1)
        _Ash1 ("Wood", Color) = (0.35, 0.33, 0.31, 1)
        _Ash2 ("Stone", Color) = (0.54, 0.53, 0.5, 1)
        _Ash3 ("Leaf", Color) = (0.63, 0.59, 0.54, 1)
        _Ash4 ("Glow", Color) = (0.23, 0.23, 0.23, 1)
        _Ash5 ("Accent", Color) = (0.7, 0.68, 0.64, 1)

        _GlowA ("Glow per slot (Clay Wood Stone Leaf)", Vector) = (0, 0, 0, 0)
        _GlowB ("Glow per slot (Glow Accent)", Vector) = (1, 0, 0, 0)
        _Tips ("Tips (on slot, tip slot, threshold, enabled)", Vector) = (0, 0, 0, 0)

        _Spec ("Specular Glint", Range(0, 2)) = 0.15
        _OutlineWidth ("Outline Width (shape units)", Float) = 0.022
        [ToggleUI] _ClearSink ("Sink In Battle Clearing", Float) = 1

        [HideInInspector] _WindowClip ("Island Window Clip", Float) = 0
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "DioramaWindow.hlsl"

    CBUFFER_START(UnityPerMaterial)
        float4 _Slot0; float4 _Slot1; float4 _Slot2; float4 _Slot3; float4 _Slot4; float4 _Slot5;
        float4 _Ash0; float4 _Ash1; float4 _Ash2; float4 _Ash3; float4 _Ash4; float4 _Ash5;
        float4 _GlowA;
        float4 _GlowB;
        float4 _Tips;
        float  _Spec;
        float  _OutlineWidth;
        float  _ClearSink;
        float  _WindowClip;
    CBUFFER_END

    float _RealmRestored; // RealmState: 0 = ash grey and cold, 1 = restored

    struct PropAttributes
    {
        float4 positionOS : POSITION;
        float3 normalOS   : NORMAL;
        float4 uv0        : TEXCOORD0; // slot, motion, height within the piece 0..1, motion amount
        float3 uv1        : TEXCOORD1; // piece centre (shape units)
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    float PropRestored() { return saturate(_RealmRestored); }

    float3 PropOrigin() { return UNITY_MATRIX_M._m03_m13_m23; }

    // 0..1 per instance, from its map position so it stays put while the map scrolls.
    float PropHash()
    {
        float3 o = DioramaMapPoint(PropOrigin(), _WindowClip);
        return frac(sin(dot(o.xz, float2(127.1, 311.7))) * 43758.5453);
    }

    // Object-space motion. flicker scales the glow; size scales the outline (steam shrinks to nothing).
    float3 PropMotion(float3 p, float4 uv0, float3 centre, float hash, out float flicker, out float size)
    {
        float t = _Time.y;
        int m = (int)round(uv0.y);
        float h = uv0.z, amt = uv0.w, hh = h * h;
        flicker = 1.0;
        size = 1.0;
        if (m == 1) // sway
        {
            float s = sin(t * 1.3 + hash * 6.283) * 0.65 + sin(t * 2.3 + hash * 12.0) * 0.35;
            p.xz += float2(1.0, 0.45) * s * amt * hh;
        }
        else if (m == 2) // flicker
        {
            float w = sin(t * 9.0 + hash * 20.0 + p.y * 12.0) * 0.6 + sin(t * 13.0 + hash * 7.0) * 0.4;
            p.x += w * amt * hh;
            p.z += cos(t * 11.0 + hash * 9.0) * amt * 0.6 * hh;
            flicker = 0.8 + 0.1 * sin(t * 11.0 + hash * 30.0) + 0.1 * sin(t * 17.3 + hash * 11.0);
        }
        else if (m == 3) // steam: grows, rises, drifts and shrinks away, then starts again
        {
            float cyc = frac(t * 0.22 + hash);
            size = sin(cyc * 3.14159) * PropRestored();
            float3 d = p - centre;
            d.x += sin(t * 2.0 + p.y * 10.0 + hash * 6.0) * amt * 0.5;
            p = centre + d * size + float3(sin(t * 0.7 + hash * 4.0) * amt, cyc * amt * 6.0, 0.0);
        }
        return p;
    }

    float3 PropWindowVertex(float3 positionOS, out float3 islandPos)
    {
        float3 ws = TransformObjectToWorld(positionOS);
        float3 origin = PropOrigin();
        float extra = _ClearSink > 0.5 ? DioramaPropSink(DioramaMapPoint(origin, _WindowClip).xz) : 0.0;
        return DioramaWindowVertexSink(ws, _WindowClip, origin, extra, islandPos);
    }

    float4 PropPick(float s, float4 a, float4 b, float4 c, float4 d, float4 e, float4 f)
    {
        return s < 0.5 ? a : s < 1.5 ? b : s < 2.5 ? c : s < 3.5 ? d : s < 4.5 ? e : f;
    }

    float PropGlow(float s)
    {
        return s < 0.5 ? _GlowA.x : s < 1.5 ? _GlowA.y : s < 2.5 ? _GlowA.z : s < 3.5 ? _GlowA.w : s < 4.5 ? _GlowB.x : _GlowB.y;
    }

    // Palette colour of a slot for the current restoration.
    float3 PropSlotColor(float s)
    {
        float3 rest = PropPick(s, _Slot0, _Slot1, _Slot2, _Slot3, _Slot4, _Slot5).rgb;
        float3 ash = PropPick(s, _Ash0, _Ash1, _Ash2, _Ash3, _Ash4, _Ash5).rgb;
        return lerp(ash, rest, PropRestored());
    }

    // Colour and glow of a fragment: the slot, or the tip slot on up-facing faces of the tips slot.
    void PropPaint(float slot, float3 normalOS, out float3 c, out float glow)
    {
        slot = round(slot);
        c = PropSlotColor(slot);
        glow = PropGlow(slot);
        if (_Tips.w > 0.5 && abs(slot - _Tips.x) < 0.5)
        {
            float m = smoothstep(_Tips.z, _Tips.z + 0.12, normalize(normalOS).y);
            c = lerp(c, PropSlotColor(_Tips.y), m);
            glow = lerp(glow, PropGlow(_Tips.y), m);
        }
    }
    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }

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

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalOS   : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                float3 islandPos  : TEXCOORD3;
                float4 paint      : TEXCOORD4; // slot, flicker, instance shade, -
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(PropAttributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float hash = PropHash();
                float flicker, size;
                float3 p = PropMotion(v.positionOS.xyz, v.uv0, v.uv1, hash, flicker, size);
                o.positionWS = PropWindowVertex(p, o.islandPos);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalOS = v.normalOS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.paint = float4(v.uv0.x, flicker, 1.0 + (hash - 0.5) * 0.08, 0.0);
                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                DioramaWindowClip(i.islandPos, _WindowClip);

                float3 c;
                float glow;
                PropPaint(i.paint.x, i.normalOS, c, glow);
                c *= i.paint.z;

                float3 L = _MainLightPosition.xyz;
                if (dot(L, L) < 1e-4) L = normalize(float3(-0.45, 0.85, 0.5));
                float3 n = normalize(i.normalWS);
                float3 v = normalize(GetCameraPositionWS() - i.positionWS);
                float ndl = dot(n, L);
                float band = 0.58 + 0.27 * smoothstep(-0.08, 0.0, ndl) + 0.15 * smoothstep(0.45, 0.52, ndl);
                float3 col = c * band;
                float rim = pow(1.0 - max(dot(n, v), 0.0), 3.0);
                col += rim * 0.32 * lerp(float3(1.0, 1.0, 1.0), c, 0.35) * smoothstep(-0.3, 0.2, ndl);
                float3 h = normalize(L + v);
                col += smoothstep(0.93, 0.95, dot(n, h)) * _Spec * 0.55;
                col += c * glow * PropRestored() * i.paint.y;
                if (_WindowClip > 0.5) col = DioramaFocus(col, DioramaMapPoint(i.positionWS, _WindowClip).xz);
                return float4(col, 1.0);
            }
            ENDHLSL
        }

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

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 islandPos  : TEXCOORD0;
                float  slot       : TEXCOORD1;
                float3 normalOS   : TEXCOORD2;
                float3 positionWS : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(PropAttributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float flicker, size;
                float3 p = PropMotion(v.positionOS.xyz, v.uv0, v.uv1, PropHash(), flicker, size);
                p += v.normalOS * _OutlineWidth * size;
                o.positionWS = PropWindowVertex(p, o.islandPos);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.slot = v.uv0.x;
                o.normalOS = v.normalOS;
                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                DioramaWindowClip(i.islandPos, _WindowClip);
                float3 c;
                float glow;
                PropPaint(i.slot, i.normalOS, c, glow);
                float3 col = c * 0.28; // like the creature outlines: base colour x 0.28 in linear
                if (_WindowClip > 0.5) col = DioramaFocus(col, DioramaMapPoint(i.positionWS, _WindowClip).xz);
                return float4(col, 1.0);
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

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 islandPos  : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(PropAttributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float flicker, size;
                float3 p = PropMotion(v.positionOS.xyz, v.uv0, v.uv1, PropHash(), flicker, size);
                o.positionCS = TransformWorldToHClip(PropWindowVertex(p, o.islandPos));
                return o;
            }

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
