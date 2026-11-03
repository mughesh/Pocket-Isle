// Environment props (PropRecipe, baked by Tools > Pocket Isle > Props): banded toon lighting like Diorama/Toon, with
// the colour picked per vertex from a 6-slot palette (uv0.x) that blends from the unrestored to the restored colours
// by the global _RealmRestored. Glow-slot lights sputter dim at 0 and burn steady at 1; other glows scale with it.
// Step 10f: warm spots (_WarmSpots, WarmSpots.cs) light and smoke the props inside them while the realm is cold.
// Motion per piece (uv0.y):
// sway, flicker (glow + wobble), steam (puffs in a loop around the piece centre, uv1), spin (step 10a: windmill
// sails turning round the piece centre about +Z with the realm's wind) or fly (step 10a: kites on posts, only there and
// fluttering once the realm is restored). A still realm (Galewick unrestored, _RealmStill) damps sway to a tiny idle. The palette comes from a
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
        _TipsHeight ("Tips By Height (0 = by facing, 1 = bottom to top)", Range(0, 1)) = 0

        _Spec ("Specular Glint", Range(0, 2)) = 0.15
        _OutlineWidth ("Outline Width (shape units)", Float) = 0.022
        _OutlineTint ("Outline Colour Override (a = amount, the title logo)", Color) = (0, 0, 0, 0)
        [ToggleUI] _ClearSink ("Sink In Battle Clearing", Float) = 1

        [HideInInspector] _WindowClip ("Island Window Clip", Float) = 0
        [HideInInspector] _RestoredSelf ("Own Restoration (-1 = the realm's)", Float) = -1
        [HideInInspector] _SpinSelf ("Own Spin Angle (radians, -1000 = the realm's wind)", Float) = -1000
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
        float  _TipsHeight;
        float  _Spec;
        float  _OutlineWidth;
        float4 _OutlineTint;
        float  _ClearSink;
        float  _WindowClip;
        float  _RestoredSelf;
        float  _SpinSelf;
    CBUFFER_END

    float _RealmRestored; // RealmState: 0 = ash grey and cold, 1 = restored
    // Step 9c restoration wave: xy = map xz of its origin, z = radius (map metres; <= 0 = no wave), w = edge width.
    float4 _RestoreWave;
    // Step 10a (RealmState): 0 = sway as usual, 1 = nothing sways (a still realm); the wind's spin angle (radians,
    // grows only while the realm's wind blows) for spinning pieces.
    float _RealmStill;
    float _WindSpin;
    // Step 10f (WarmSpots): up to 12 (step 14c; was 8) rekindled places on the showing map (a village flame, a lantern, the forge): props
    // inside one burn and smoke as if restored while the realm is still cold. xy = map xz, z = radius (<= 0 = off), w = edge.
    float4 _WarmSpots[12];
    // Per vertex / fragment: how warm this prop is from the spots (set at the top of each stage, 0 = none).
    static float sPropWarm;

    // Spots are per instance (its origin), so the vertex stage works it out once and hands it to the fragment.
    float PropWarm()
    {
        if (_RestoredSelf >= 0.0 || _WindowClip < 0.5) return 0.0;
        float2 m = DioramaMapPoint(UNITY_MATRIX_M._m03_m13_m23, _WindowClip).xz;
        float w = 0.0;
        [unroll] for (int i = 0; i < 12; i++)
        {
            float4 s = _WarmSpots[i];
            if (s.z > 0.0) w = max(w, saturate((s.z - distance(m, s.xy)) / max(1e-4, s.w)));
        }
        return w;
    }

    struct PropAttributes
    {
        float4 positionOS : POSITION;
        float3 normalOS   : NORMAL;
        float4 uv0        : TEXCOORD0; // slot, motion, height within the piece 0..1, motion amount
        float3 uv1        : TEXCOORD1; // piece centre (shape units)
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    // A puzzle prop drawn on its own (step 9b: a brazier warming up, the forge door heating) sets its own restoration.
    // While a restoration wave runs (step 9c), props inside its ring are already restored.
    float PropRestored()
    {
        if (_RestoredSelf >= 0.0) return saturate(_RestoredSelf);
        float r = saturate(_RealmRestored);
        if (_RestoreWave.z > 0.0)
        {
            float2 m = DioramaMapPoint(UNITY_MATRIX_M._m03_m13_m23, _WindowClip).xz;
            r = max(r, saturate((_RestoreWave.z - distance(m, _RestoreWave.xy)) / max(1e-4, _RestoreWave.w)));
        }
        return r;
    }

    // Lights, glows and smoke: the restoration, or a warm spot over it (step 10f).
    float PropLit() { return max(PropRestored(), sPropWarm); }

    float3 PropOrigin() { return UNITY_MATRIX_M._m03_m13_m23; }

    // 0..1 per instance, from its map position so it stays put while the map scrolls.
    float PropHash()
    {
        float3 o = DioramaMapPoint(PropOrigin(), _WindowClip);
        return DioramaStableHash(o.xz);
    }

    // Object-space motion. flicker scales the glow; size scales the outline (steam shrinks to nothing).
    float3 PropMotion(float3 p, inout float3 n, float4 uv0, float3 centre, float hash, out float flicker, out float size)
    {
        float t = _Time.y;
        int m = (int)round(uv0.y);
        float h = uv0.z, amt = uv0.w, hh = h * h;
        flicker = 1.0;
        size = 1.0;
        if (m == 1) // sway
        {
            float s = sin(t * 1.3 + hash * 6.283) * 0.65 + sin(t * 2.3 + hash * 12.0) * 0.35;
            p.xz += float2(1.0, 0.45) * s * amt * hh * (1.0 - saturate(_RealmStill));
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
            size = sin(cyc * 3.14159) * PropLit();
            float3 d = p - centre;
            d.x += sin(t * 2.0 + p.y * 10.0 + hash * 6.0) * amt * 0.5;
            p = centre + d * size + float3(sin(t * 0.7 + hash * 4.0) * amt, cyc * amt * 6.0, 0.0);
        }
        else if (m == 4) // spin: round the piece centre about +Z; amount = how fast it follows the wind spin
        {
            // Step 11d: a piece with its own angle (a flicked mill, the tower's vanes) turns by exactly that angle, with no
            // per-position phase: that phase came from the map position, which moves with the window while walking.
            float ang = _SpinSelf > -999.0 ? _SpinSelf : _WindSpin * amt * (0.85 + 0.3 * hash) + hash * 6.283;
            float c = cos(ang), sn = sin(ang);
            float3 d = p - centre;
            d.xy = float2(c * d.x - sn * d.y, sn * d.x + c * d.y);
            n.xy = float2(c * n.x - sn * n.y, sn * n.x + c * n.y);
            p = centre + d;
        }
        else if (m == 5) // fly: only once restored (grows from its centre), a lazy drift plus a flutter toward the bottom
        {
            size = PropRestored();
            float3 d = p - centre;
            float drift = sin(t * 0.9 + hash * 6.283);
            float bob = sin(t * 1.6 + hash * 11.0);
            p = centre + d * size + float3(drift * amt, bob * amt * 0.45, cos(t * 0.7 + hash * 4.0) * amt * 0.4) * size;
            p.x += sin(t * 6.0 + h * 9.0 + hash * 20.0) * amt * 0.25 * (1.0 - h) * size;
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
        // A warm spot brings most of the colour back too (step 10f), the realm's own restoration all of it.
        return lerp(ash, rest, max(PropRestored(), sPropWarm * 0.7));
    }

    // Colour and glow of a fragment: the slot, or the tip slot on up-facing faces of the tips slot (tip = that mask),
    // blended toward a bottom-to-top gradient over the piece's height by _TipsHeight (canopies: red-orange to gold).
    void PropPaint(float slot, float3 normalOS, float height, out float3 c, out float glow, out float tip)
    {
        slot = round(slot);
        c = PropSlotColor(slot);
        // Step 10f: an unlit light (a cold kiln mouth, a dark window or lantern) is a dull ember, not the lit colour.
        if (abs(slot - 4.0) < 0.5) c *= lerp(0.5, 1.0, PropLit());
        glow = PropGlow(slot);
        tip = 0.0;
        if (_Tips.w > 0.5 && abs(slot - _Tips.x) < 0.5)
        {
            tip = smoothstep(_Tips.z, _Tips.z + 0.12, normalize(normalOS).y);
            tip = lerp(tip, smoothstep(0.3, 1.0, height), _TipsHeight);
            c = lerp(c, PropSlotColor(_Tips.y), tip);
            glow = lerp(glow, PropGlow(_Tips.y), tip);
        }
    }

    // Unlit (unrestored) flames and lanterns: dim and irregular, dropping out and catching again. 0..1.
    float PropSputter(float hash)
    {
        float t = _Time.y;
        float step = floor(t * 6.0 + hash * 40.0);
        float n = frac(sin(step * 12.9898 + hash * 78.233) * 43758.5453);
        float lit = n < 0.28 ? 0.08 : n < 0.45 ? 0.45 : 1.0;
        return lit * (0.7 + 0.3 * sin(t * 23.0 + hash * 9.0));
    }

    // How much a fragment's glow shows for the realm's state: the Glow slot sputters dim at 0 and burns steady at 1,
    // glowing leaf tips pulse gently only when restored, everything else simply scales with the restoration.
    float PropGlowLevel(float slot, float tip, float hash, float flicker)
    {
        float r = PropLit();
        if (abs(round(slot) - 4.0) < 0.5) return lerp(0.3 * PropSputter(hash), 1.0, r) * flicker;
        float pulse = 0.8 + 0.2 * sin(_Time.y * 1.7 + hash * 6.283);
        return r * lerp(flicker, pulse, tip);
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
                float  height     : TEXCOORD5; // height within the piece 0..1
                float  warm       : TEXCOORD6; // step 10f: warm spots
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(PropAttributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                sPropWarm = PropWarm();
                o.warm = sPropWarm;
                float hash = PropHash();
                float flicker, size;
                float3 nOS = v.normalOS;
                float3 p = PropMotion(v.positionOS.xyz, nOS, v.uv0, v.uv1, hash, flicker, size);
                o.positionWS = PropWindowVertex(p, o.islandPos);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalOS = nOS;
                o.normalWS = TransformObjectToWorldNormal(nOS);
                o.paint = float4(v.uv0.x, flicker, 1.0 + (hash - 0.5) * 0.08, hash);
                o.height = v.uv0.z;
                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                DioramaWindowClip(i.islandPos, _WindowClip);
                sPropWarm = i.warm;

                float3 c;
                float glow, tip;
                PropPaint(i.paint.x, i.normalOS, i.height, c, glow, tip);
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
                float level = PropGlowLevel(i.paint.x, tip, i.paint.w, i.paint.y);
                col += c * glow * level;
                // Burning lights get a soft halo at their silhouette when restored.
                col += c * glow * rim * 0.9 * PropLit() * step(abs(round(i.paint.x) - 4.0), 0.5);
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
                float3 focus      : TEXCOORD4; // x gold, y white hover
                float  height     : TEXCOORD5;
                float  warm       : TEXCOORD6;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(PropAttributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                sPropWarm = PropWarm();
                o.warm = sPropWarm;
                float flicker, size;
                float3 nOS = v.normalOS;
                float3 p = PropMotion(v.positionOS.xyz, nOS, v.uv0, v.uv1, PropHash(), flicker, size);
                float2 focus = DioramaFocusAmounts(TransformObjectToWorld(float3(0.0, 0.0, 0.0)));
                o.focus = float3(focus, 0.0);
                p += nOS * _OutlineWidth * size * (1.0 + 1.3 * focus.x + 0.45 * focus.y);
                o.positionWS = PropWindowVertex(p, o.islandPos);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.slot = v.uv0.x;
                o.height = v.uv0.z;
                o.normalOS = nOS;
                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                DioramaWindowClip(i.islandPos, _WindowClip);
                sPropWarm = i.warm;
                float3 c;
                float glow;
                float tip;
                PropPaint(i.slot, i.normalOS, i.height, c, glow, tip);
                float3 col = c * 0.28; // like the creature outlines: base colour x 0.28 in linear
                col = lerp(col, _OutlineTint.rgb, _OutlineTint.a); // step 9h: one dark outline colour (the logo)
                if (_WindowClip > 0.5) col = DioramaFocus(col, DioramaMapPoint(i.positionWS, _WindowClip).xz);
                col = lerp(col, _DioramaHoverColor.rgb, i.focus.y);
                col = lerp(col, _DioramaHighlightColor.rgb, i.focus.x);
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
                sPropWarm = PropWarm();
                float flicker, size;
                float3 nOS = v.normalOS;
                float3 p = PropMotion(v.positionOS.xyz, nOS, v.uv0, v.uv1, PropHash(), flicker, size);
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
