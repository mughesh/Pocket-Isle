#ifndef DIORAMA_WINDOW_INCLUDED
#define DIORAMA_WINDOW_INCLUDED

// The island as a window onto the map (IslandBuilder sets these globals every frame).
// Content instances near the footprint edge sink into the rocky base, and anything below the base top or
// outside the footprint is clipped. Only draws that set _WindowClip = 1 (IslandBuilder content) are affected.

float4x4 _DioramaWorldToIsland;
float4x4 _DioramaIslandToWorld;
float4x4 _DioramaWorldToMap;    // world to scrolling map space: stable hashes and wind while the map slides
float4   _DioramaFootprintUV;   // uv = (x + y * islandX, z + w * islandZ)
float4   _DioramaWindow;        // x clip plane (island-local y), y sinking band half width (tiles), z deepest sink, w distance range (tiles)
TEXTURE2D(_DioramaFootprint);   // signed distance to the footprint edge in tiles, positive inside
SAMPLER(sampler_DioramaFootprint);

// Battle clearing (IslandBuilder.SetClearing), all in map space (metres) so it stays on its tiles while zooming:
float4   _DioramaClearing;      // xy = ellipse centre (map xz), zw = unit axis along the pad line
float4   _DioramaClearingSize;  // x = radius along the axis, y = radius across, z = strength 0..1, w = focus dim 0..1
float4   _DioramaClearingView;  // xy = end of the view corridor toward the player (map xz), z = corridor half width
float4   _DioramaClearingWorld; // xyz = ellipse centre in world space (grass bends away from it)

// Scene swap (SceneSwap: shrine -> lodge, gates, temples): x = progress 0..1 (1 = all content sunk), y = island-local z
// of the front edge, z = front-to-back depth. Front tiles go first, a ripple toward the back.
float4   _DioramaSwap;

// Focus language (step 7c, FocusHighlight): gold = "do this now" (the Spirit's hint target), white = what the gaze is
// on. Spheres: xyz = world centre, w = radius in world metres (0 = off). Colours: rgb, a = strength (gold pulses).
// Any outline whose object origin is inside a sphere takes its colour and grows; pods glow at the rim.
float4   _DioramaHighlight;
float4   _DioramaHighlightColor;
float4   _DioramaHover;
float4   _DioramaHoverColor;

float DioramaInSphere(float4 sphere, float3 originWS)
{
    if (sphere.w <= 0.0) return 0.0;
    return 1.0 - smoothstep(sphere.w * 0.85, sphere.w * 1.15, distance(originWS, sphere.xyz));
}

// Gold (hint target) and white (gazed) strengths for an object.
float2 DioramaFocusAmounts(float3 originWS)
{
    float gold = DioramaInSphere(_DioramaHighlight, originWS) * _DioramaHighlightColor.a;
    float hover = DioramaInSphere(_DioramaHover, originWS) * _DioramaHoverColor.a * (1.0 - gold);
    return float2(gold, hover);
}

// Outline colour and width scale for an object at originWS.
float3 DioramaOutlineFocus(float3 col, float3 originWS, out float widthScale)
{
    float2 k = DioramaFocusAmounts(originWS);
    widthScale = 1.0 + 1.3 * k.x + 0.45 * k.y;
    col = lerp(col, _DioramaHoverColor.rgb, k.y);
    return lerp(col, _DioramaHighlightColor.rgb, k.x);
}

float DioramaSwapSink(float3 islandOrigin)
{
    if (_DioramaSwap.x <= 0.0) return 0.0;
    float delay = saturate((_DioramaSwap.y - islandOrigin.z) / max(_DioramaSwap.z, 1e-4)) * 0.6;
    return smoothstep(delay, delay + 0.4, _DioramaSwap.x);
}

float DioramaEdgeDistance(float2 islandXZ)
{
    float2 uv = float2(_DioramaFootprintUV.x + _DioramaFootprintUV.y * islandXZ.x,
                       _DioramaFootprintUV.z + _DioramaFootprintUV.w * islandXZ.y);
    float e = SAMPLE_TEXTURE2D_LOD(_DioramaFootprint, sampler_DioramaFootprint, uv, 0).r;
    return (e * 2.0 - 1.0) * _DioramaWindow.w;
}

// Map-space position of a world point for island content; world space for everything else.
float3 DioramaMapPoint(float3 positionWS, float on)
{
    return on > 0.5 ? mul(_DioramaWorldToMap, float4(positionWS, 1.0)).xyz : positionWS;
}

// Stable 0..1 hash of a map-space point (per-instance phases and shades). Map positions are recovered from world space
// every frame, so they wobble by ~1e-6 m while the map scrolls; a plain sin hash amplifies that into a new random value
// each frame (effects and shades jittered only while walking). Snapping to a 1 mm grid first keeps it fixed.
float DioramaStableHash(float2 mapXZ)
{
    float2 q = floor(mapXZ * 1000.0 + 0.5);
    return frac(sin(dot(q, float2(12.9898, 78.233))) * 43758.5453);
}

// 0..1 inside the clearing ellipse (soft edge), ignoring the fade.
float DioramaClearingShape(float2 mapXZ)
{
    float2 d = mapXZ - _DioramaClearing.xy;
    float2 ax = _DioramaClearing.zw;
    float2 r = float2(dot(d, ax) / max(_DioramaClearingSize.x, 1e-4), dot(d, float2(-ax.y, ax.x)) / max(_DioramaClearingSize.y, 1e-4));
    return 1.0 - smoothstep(0.75, 1.1, length(r));
}

// Inside the clearing, faded in with the battle zoom (0 when there is no battle).
float DioramaClearing(float2 mapXZ)
{
    if (_DioramaClearingSize.z <= 0.0) return 0.0;
    return DioramaClearingShape(mapXZ) * _DioramaClearingSize.z;
}

// Props (trees, rocks) sink inside the clearing and in a corridor from it toward the player, so nothing blocks the view.
float DioramaPropSink(float2 mapXZ)
{
    if (_DioramaClearingSize.z <= 0.0) return 0.0;
    float2 a = _DioramaClearing.xy, b = _DioramaClearingView.xy, p = mapXZ - a, ab = b - a;
    float t = saturate(dot(p, ab) / max(dot(ab, ab), 1e-6));
    float corridor = 1.0 - smoothstep(_DioramaClearingView.z * 0.8, _DioramaClearingView.z * 1.2, length(p - ab * t));
    return max(DioramaClearingShape(mapXZ), corridor) * _DioramaClearingSize.z;
}

// Darkens and desaturates content outside the clearing while a battle is on (the rocky base doesn't call this).
float3 DioramaFocus(float3 col, float2 mapXZ)
{
    float dim = _DioramaClearingSize.w * (1.0 - DioramaClearingShape(mapXZ));
    float grey = dot(col, float3(0.299, 0.587, 0.114));
    return lerp(col, grey.xxx, dim * 0.6) * (1.0 - dim);
}

// Sinks the whole instance by how close originWS (usually the object origin) is to the window edge (continuous
// while the map slides). Multi-mesh objects (creatures) pass one shared origin so all their pieces sink together.
// extraSink (0..1) sinks it further (props inside the battle clearing).
// Returns the new world position; islandPos is the island-local position for DioramaWindowClip.
float3 DioramaWindowVertexSink(float3 positionWS, float on, float3 originWS, float extraSink, out float3 islandPos)
{
    islandPos = mul(_DioramaWorldToIsland, float4(positionWS, 1.0)).xyz;
    if (on < 0.5) return positionWS;
    float3 origin = mul(_DioramaWorldToIsland, float4(originWS, 1.0)).xyz;
    float s = DioramaEdgeDistance(origin.xz);
    float sink = max(max(1.0 - smoothstep(-_DioramaWindow.y, _DioramaWindow.y, s), extraSink), DioramaSwapSink(origin));
    islandPos.y -= _DioramaWindow.z * sink;
    return mul(_DioramaIslandToWorld, float4(islandPos, 1.0)).xyz;
}

float3 DioramaWindowVertex(float3 positionWS, float on, float3 originWS, out float3 islandPos)
{
    return DioramaWindowVertexSink(positionWS, on, originWS, 0.0, islandPos);
}

void DioramaWindowClip(float3 islandPos, float on)
{
    if (on < 0.5) return;
    clip(islandPos.y - _DioramaWindow.x);
    clip(DioramaEdgeDistance(islandPos.xz));
}

#endif
