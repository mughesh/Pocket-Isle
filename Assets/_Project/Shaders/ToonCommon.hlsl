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
    float  _OutlineRadial; // step 22: 1 = push the hull out from the centre, so hard-edged meshes (the shard) don't split at corners
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

// Step 22 (second pass; Mugesh, Nov 8: keep the old stone and gold, give the cracks depth and something flowing in
// them). The same crack lines as ToonLava, carved in: the stone beside a crack tilts toward it before the toon lighting
// (a procedural normal map, _CrackInner), and inside the opening the view ray walks down the groove (_CrackDepth, about
// one crack width), so you see the far wall and the fluid floor shift as the head moves. Fluid: noise scrolling up
// (_CrackFlow = speed). The particles that leave the cracks are real ones (Opening.EmitCrackSparks, CrackField.cs).
// Plates around fixed points on a sphere: every crack is a plane through the body's centre, so it always cuts straight
// through the surface. (A 3D cell pattern has planes in any direction; a face lying along one smears into a blob.)
// Returns the distance to the nearest crack, scaled like ToonCrackDistance so _CrackWidth means the same.
// dirO: toward that crack. edge: 0..1, the same on both sides of one crack segment (main cracks and thin ones).
// Keep CrackField.cs the same.
static const float3 ToonCrackSites[26] =
{
    float3(0.3904, 0.9202, -0.0294),
    float3(-0.3486, 0.9195, 0.1817),
    float3(-0.0362, 0.8626, -0.5046),
    float3(0.299, 0.8641, 0.4049),
    float3(-0.7035, 0.7046, -0.0932),
    float3(0.7092, 0.5774, -0.4045),
    float3(-0.1175, 0.536, 0.836),
    float3(-0.411, 0.221, -0.8845),
    float3(0.8539, 0.3199, 0.4104),
    float3(-0.7146, 0.4479, 0.5373),
    float3(0.5001, 0.404, -0.766),
    float3(0.2273, 0.2827, 0.9319),
    float3(-0.8282, 0.2246, -0.5134),
    float3(0.9793, -0.0393, -0.1984),
    float3(-0.7018, -0.212, 0.6801),
    float3(-0.2828, -0.2451, -0.9273),
    float3(0.6745, -0.1757, 0.7171),
    float3(-0.8823, -0.4385, 0.1712),
    float3(0.6976, -0.3805, -0.6071),
    float3(-0.185, -0.4214, 0.8878),
    float3(-0.5818, -0.5196, -0.6257),
    float3(0.8022, -0.5833, 0.1276),
    float3(-0.5068, -0.7526, 0.4204),
    float3(0.2779, -0.7833, -0.5561),
    float3(0.2587, -0.834, 0.4873),
    float3(-0.3655, -0.8966, -0.2499)
};

float ToonCrackField(float3 pO, out float3 dirO, out float edge)
{
    // Three scales of wobble: broken lines, not smooth arcs.
    float3 p = pO + sin(pO.yzx * 9.0) * 0.03 + sin(pO.zxy * 23.0 + 1.3) * 0.0125 + sin(pO.yzx * 57.0 + 0.7) * 0.005;
    float3 d = normalize(p);
    int best = 0;
    float top = -2.0;
    for (int i = 0; i < 26; i++)
    {
        float c = dot(d, ToonCrackSites[i]);
        if (c > top) { top = c; best = i; }
    }
    float3 a = ToonCrackSites[best];
    float e = 8.0;
    dirO = float3(1.0, 0.0, 0.0);
    edge = 0.0;
    for (int j = 0; j < 26; j++)
    {
        if (j == best) continue;
        float3 ab = a - ToonCrackSites[j];
        float len = length(ab);
        float g = dot(d, ab) / len;
        if (g < e) { e = g; dirO = -ab / len; edge = ToonHash3((a + ToonCrackSites[j]) * 17.0).y; }
    }
    return max(e, 0.0) * length(p) * 2.0 * _CrackScale;
}

// Swells and thins gently; never closes (a line that fades out reads as a scratch, not a crack).
float ToonCrackWidth(float3 pO, float edge)
{
    return _CrackWidth * lerp(0.5, 1.4, smoothstep(0.25, 0.8, edge)) * (0.8 + 0.2 * sin(dot(pO, float3(1.7, 2.3, 1.1)) * 2.6));
}

float ToonNoise3(float3 p)
{
    float3 i = floor(p);
    float3 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    return lerp(
        lerp(lerp(ToonHash3(i).x, ToonHash3(i + float3(1, 0, 0)).x, f.x), lerp(ToonHash3(i + float3(0, 1, 0)).x, ToonHash3(i + float3(1, 1, 0)).x, f.x), f.y),
        lerp(lerp(ToonHash3(i + float3(0, 0, 1)).x, ToonHash3(i + float3(1, 0, 1)).x, f.x), lerp(ToonHash3(i + float3(0, 1, 1)).x, ToonHash3(i + float3(1, 1, 1)).x, f.x), f.y),
        f.z);
}

// The normal-map part: world normal tilted toward the crack over the bevel beside it. Call before lighting.
float3 ToonCrackBevel(float3 nW, float3 pO, float e, float3 side, float edge)
{
    float w = ToonCrackWidth(pO, edge);
    float slope = 1.0 - smoothstep(w, w * 2.4 + 0.012, e);
    return normalize(nW + TransformObjectToWorldDir(side, false) * slope * _CrackInner);
}

float3 ToonDeepCracks(float3 col, float3 pO, float3 vO, float3 nO, float e0, float edge)
{
    float w0 = ToonCrackWidth(pO, edge);
    float open = 1.0 - smoothstep(w0 * 0.86, w0, e0);
    float pulse = 1.0 + 0.4 * _CrackPulse * sin((_Time.y + _TimeOffset) * 2.0 - pO.y * 4.0);

    // Stone: a thin dark edge, then the crack's warm light spilling over it.
    // Main cracks and hot stretches light the stone; thin ones and dim stretches stay dark.
    float lip = 1.0 - smoothstep(w0, w0 * 2.2 + 0.006, e0);
    float bleed = 1.0 - smoothstep(w0, w0 * 4.0 + 0.02, e0);
    float heat = bleed > 0.001 ? smoothstep(0.35, 0.72, ToonNoise3(pO * 6.0 + 3.1)) * smoothstep(0.3, 0.8, edge) : 0.0;
    col *= lerp(1.0, 0.34, lip * (1.0 - open));
    col += _CrackColor.rgb * bleed * bleed * 0.55 * heat * pulse * (1.0 - open);
    if (open <= 0.001) return col;

    // Down the groove, slanting with the view; the first step that leaves the crack has hit a wall.
    float3 ray = vO / max(dot(vO, nO), 0.3) * _CrackDepth;
    float hit = 1.0;
    [unroll] for (int k = 3; k >= 1; k--)
    {
        float3 skipDir;
        float skipEdge;
        if (ToonCrackField(pO - ray * (k / 3.0), skipDir, skipEdge) > w0 * (1.0 - 0.08 * k)) hit = (k - 1.0) / 3.0 + 0.001;
    }

    float t = (_Time.y + _TimeOffset) * _CrackFlow;
    float3 fp = pO - ray * hit;
    float3 c1 = fp * 15.0 + float3(0.0, -t * 2.4, 0.0);
    c1.xz += sin(fp.y * 18.0 + t * 0.7) * 0.5;
    float flow = ToonNoise3(c1) * 0.65 + ToonNoise3(c1 * 2.1 + float3(t * 0.6, -t * 1.5, t * 0.4)) * 0.35;

    float3 crust = _CrackColor.rgb * 0.72;
    float3 hot = lerp(_CrackColor.rgb, float3(1.0, 0.97, 0.75), 0.85);
    float centre = 1.0 - smoothstep(w0 * 0.1, w0 * 0.7, e0);
    float3 fluid = lerp(crust, _CrackColor.rgb, smoothstep(0.32, 0.52, flow));
    fluid = lerp(fluid, hot, saturate(centre * lerp(0.25, 0.9, heat) + smoothstep(0.56, 0.7, flow))) * pulse * lerp(0.72, 1.0, heat);
    float3 wall = _CrackColor.rgb * lerp(0.3, 0.85, hit);
    return lerp(col, hit >= 1.0 ? fluid : wall, open);
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
