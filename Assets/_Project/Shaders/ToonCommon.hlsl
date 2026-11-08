#ifndef DIORAMA_TOON_COMMON_INCLUDED
#define DIORAMA_TOON_COMMON_INCLUDED

// Shared by Toon.shader and ToonOutline.shader. Both use the same material layout, so the
// outline material can carry the same breathe / jiggle values as the body it outlines.
// Ported from toon() in Docs/Reference/creature-diorama-v2.html.

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "DioramaWindow.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _BaseColor;
    float4 _TopColor;
    float  _Grad;

    float4 _BellyColor;
    float4 _BellyDir;
    float  _BellyAt;
    float  _BellyOn;
    float  _BellyMaxY;
    float  _BellyDrips;   // > 0: the belly edge runs down in this many drips around Y (glaze)
    float  _BellyDripLen; // how far the drips reach (in dot(normal, direction) units)

    float4 _SockColor;
    float  _SockY;
    float  _SockOn;

    float4 _TipColor;
    float4 _TipAxis;
    float  _TipAt;
    float  _TipOn;

    float4 _SpotColor;
    float  _SpotScale;
    float  _SpotOn;

    float4 _CrackColor;  // glowing crack lines (creatures): self-lit, pulse over time
    float  _CrackOn;
    float  _CrackScale;
    float  _CrackWidth;
    float  _CrackPulse;
    float  _CrackDepth;  // step 22: object units the crack glow sits below the surface (0 = painted on, the old look)
    float  _CrackFlow;   // step 22: specks of light drifting up through the cracks (0..1)
    float  _CrackInner;  // step 22: how much of the inner glow shows through the stone around the cracks (0..1)

    float  _Spec;
    float  _Emissive;
    float  _Flash;
    float  _OriginJitter;
    float  _Ripple;

    float  _Breathe;
    float  _BreatheSpeed;
    float  _Jiggle;
    float  _DeformPivotY; // breathing squashes toward this height (creature feet)
    float  _TimeOffset;   // per-creature phase so they don't breathe in step

    // Round painted patches (creatures): xyz centre, w radius in object space (0 = off). Later ones paint on top.
    float4 _Patch0; float4 _Patch1; float4 _Patch2; float4 _Patch3; float4 _Patch4; float4 _Patch5;
    float4 _PatchColor0; float4 _PatchColor1; float4 _PatchColor2; float4 _PatchColor3; float4 _PatchColor4; float4 _PatchColor5;

    float  _OutlineWidth;
    float4 _OutlineColor;

    float  _ClearSink;  // 1 = prop (tree, rock): sinks inside the battle clearing and its view corridor
    float4 _ClearColor; // a = 1: this colour inside the battle clearing (tall grass caps turn short)

    float  _WindowClip; // 1 = island map content (set by IslandBuilder)
    float4 _WindowOrigin; // w = 1: sink by this world point instead of the object origin (creatures, set per frame)
CBUFFER_END

// Breathe squashes the body vertically and widens it; jiggle wobbles it along the normal.
float3 ToonDeform(float3 positionOS, float3 normalOS)
{
    float t = _Time.y + _TimeOffset;
    float br = sin(t * _BreatheSpeed) * _Breathe;
    float3 p = positionOS;
    p.y = _DeformPivotY + (p.y - _DeformPivotY) * (1.0 - br * 0.6);
    p.xz *= 1.0 + br * 0.4;
    p += normalOS * sin(t * 5.0 + positionOS.y * 5.0 + positionOS.x * 3.0) * _Jiggle;
    return p;
}

// 0..1 hash of the object (or instance) origin. Gives each instanced tile its own shade.
// Island content hashes its map position, so shades stay put while the map scrolls.
float ToonOriginHash()
{
    float3 o = DioramaMapPoint(UNITY_MATRIX_M._m03_m13_m23, _WindowClip);
    return DioramaStableHash(o.xz);
}

float3 ToonWindowOrigin()
{
    return _WindowOrigin.w > 0.5 ? _WindowOrigin.xyz : UNITY_MATRIX_M._m03_m13_m23;
}

// Window sink and clip for any toon pass, including the battle clearing for props.
float3 ToonWindowVertex(float3 positionWS, out float3 islandPos)
{
    float3 origin = ToonWindowOrigin();
    float extra = _ClearSink > 0.5 ? DioramaPropSink(DioramaMapPoint(origin, _WindowClip).xz) : 0.0;
    return DioramaWindowVertexSink(positionWS, _WindowClip, origin, extra, islandPos);
}

// Belly threshold lowered in vertical streaks around Y, so the painted edge drips (glaze running down a pot).
float ToonBellyAt(float3 pO)
{
    if (_BellyDrips <= 0.0) return _BellyAt;
    float a = atan2(pO.x, pO.z);
    float d = sin(a * _BellyDrips + sin(a * 3.0) * 1.3);
    return _BellyAt - _BellyDripLen * pow(saturate(d), 6.0);
}

float3 ToonHash3(float3 p)
{
    p = float3(dot(p, float3(127.1, 311.7, 74.7)), dot(p, float3(269.5, 183.3, 246.1)), dot(p, float3(113.5, 271.9, 124.6)));
    return frac(sin(p) * 43758.5453);
}

// Distance to the nearest crack line: edges of a 3D cell pattern (second nearest minus nearest cell point).
float ToonCrackDistance(float3 pO)
{
    float3 p = pO * _CrackScale + sin(pO.yzx * 9.0) * 0.06;
    float3 i = floor(p);
    float3 f = frac(p);
    float d1 = 8.0, d2 = 8.0;
    [unroll] for (int z = -1; z <= 1; z++)
    [unroll] for (int y = -1; y <= 1; y++)
    [unroll] for (int x = -1; x <= 1; x++)
    {
        float3 g = float3(x, y, z);
        float3 r = g + ToonHash3(i + g) - f;
        float d = dot(r, r);
        if (d < d1) { d2 = d1; d1 = d; }
        else if (d < d2) d2 = d;
    }
    return sqrt(d2) - sqrt(d1);
}

// Molten cracks: a hot yellow core, an orange edge and a charred rim on the surface around them. Width swells
// and thins along each line, so some segments close up (reads as cracks, not a net). The glow pulse runs upward.
float3 ToonLava(float3 col, float3 pO)
{
    float e = ToonCrackDistance(pO);
    float w = _CrackWidth * saturate(sin(dot(pO, float3(1.7, 2.3, 1.1)) * 2.6) * 0.9 + 0.75);
    float core = 1.0 - smoothstep(w * 0.3, w * 0.55, e);
    float glow = 1.0 - smoothstep(w * 0.55, w + 0.004, e);
    float charred = 1.0 - smoothstep(w, w * 2.4 + 0.01, e);
    float pulse = 1.1 + 0.45 * _CrackPulse * sin((_Time.y + _TimeOffset) * 2.0 - pO.y * 4.0);
    float3 hot = lerp(_CrackColor.rgb, float3(1.0, 0.92, 0.55), 0.65);
    float3 lava = lerp(_CrackColor.rgb, hot, core) * pulse;
    col *= lerp(1.0, 0.55, charred * (1.0 - glow));
    return lerp(col, lava, glow);
}

// Step 22 (Mugesh, Nov 8: the shard's cracks look like one flat painted texture; the lava Islings too): cracks with
// depth. The crack field is sampled again inside the body along the view ray (object space), wider and hotter the
// deeper, so the glow sits down in the crack and slides against the surface lines as the head moves. Specks of light
// drift up through the cracks; a faint glow shows through the stone around them (_CrackInner). The surface keeps a
// dark lip, so each crack reads as a groove. Cost: four crack lookups instead of one, only where _CrackDepth > 0.
float3 ToonDeepCracks(float3 col, float3 pO, float3 vO)
{
    float t = _Time.y + _TimeOffset;
    float pulse = 1.0 + 0.4 * _CrackPulse * sin(t * 2.0 - pO.y * 4.0);
    float3 hot = lerp(_CrackColor.rgb, float3(1.0, 0.95, 0.7), 0.45);

    // The surface: a dark lip around each crack (a groove) and its opening.
    float e0 = ToonCrackDistance(pO);
    float w0 = _CrackWidth * saturate(sin(dot(pO, float3(1.7, 2.3, 1.1)) * 2.6) * 0.9 + 0.75);
    float open = 1.0 - smoothstep(w0 * 0.45, w0 + 0.003, e0);
    float lip = 1.0 - smoothstep(w0, w0 * 2.6 + 0.01, e0);
    col *= lerp(1.0, 0.35, lip * (1.0 - open));

    // Down inside: the same cracks further along the view ray, a little wider and hotter each layer, dimmer through
    // more stone. Inside the opening all of it shows; around it only _CrackInner of it.
    float3 inner = 0.0;
    float near = 0.0;
    [unroll] for (int k = 1; k <= 3; k++)
    {
        float3 q = pO - vO * (_CrackDepth * k / 3.0);
        float e = ToonCrackDistance(q);
        float w = w0 * (1.0 + 0.3 * k);
        float core = 1.0 - smoothstep(w * 0.15, w * 0.6, e);
        float halo = 1.0 - smoothstep(w * 0.6, w * 1.6, e);
        float through = lerp(_CrackInner, 1.0, open) / (1.0 + 0.6 * k);
        inner += (hot * core + _CrackColor.rgb * halo * 0.22) * through;
        near = max(near, halo);
    }
    // The crack's own bright floor right under the opening.
    inner += hot * open * 0.9;

    // Specks: a moving cell grid halfway down; a few cells hold a twinkling point drifting upward, only in the cracks.
    float3 m = pO - vO * (_CrackDepth * 0.5);
    float3 sp = (m - float3(0.0, t * 0.09, 0.0)) * (_CrackScale * 4.0);
    float3 cell = floor(sp);
    float3 r = ToonHash3(cell);
    float3 f = frac(sp) - 0.5 - (r - 0.5) * 0.5;
    float speck = step(0.55, r.x) * smoothstep(0.16, 0.02, length(f)) * (0.5 + 0.5 * sin(t * (2.0 + 3.0 * r.y) + r.z * 20.0));
    speck *= max(near, open) * _CrackFlow;

    col += inner * pulse;
    col += float3(1.0, 0.97, 0.85) * speck * 1.6;
    return col;
}

float3 ToonPatch(float3 c, float4 patch, float4 color, float3 pO)
{
    float m = patch.w > 0.0 ? 1.0 - smoothstep(patch.w - 0.012, patch.w + 0.012, distance(pO, patch.xyz)) : 0.0;
    return lerp(c, color.rgb, m);
}

float3 ToonPatches(float3 c, float3 pO)
{
    c = ToonPatch(c, _Patch0, _PatchColor0, pO);
    c = ToonPatch(c, _Patch1, _PatchColor1, pO);
    c = ToonPatch(c, _Patch2, _PatchColor2, pO);
    c = ToonPatch(c, _Patch3, _PatchColor3, pO);
    c = ToonPatch(c, _Patch4, _PatchColor4, pO);
    c = ToonPatch(c, _Patch5, _PatchColor5, pO);
    return c;
}

#endif
