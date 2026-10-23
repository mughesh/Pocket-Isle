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

float DioramaEdgeDistance(float2 islandXZ)
{
    float2 uv = float2(_DioramaFootprintUV.x + _DioramaFootprintUV.y * islandXZ.x,
                       _DioramaFootprintUV.z + _DioramaFootprintUV.w * islandXZ.y);
    float e = SAMPLE_TEXTURE2D_LOD(_DioramaFootprint, sampler_DioramaFootprint, uv, 0).r;
    return (e * 2.0 - 1.0) * _DioramaWindow.w;
}

// Sinks the whole instance by how close its origin is to the window edge (continuous while the map slides).
// Returns the new world position; islandPos is the island-local position for DioramaWindowClip.
float3 DioramaWindowVertex(float3 positionWS, float on, out float3 islandPos)
{
    islandPos = mul(_DioramaWorldToIsland, float4(positionWS, 1.0)).xyz;
    if (on < 0.5) return positionWS;
    float3 origin = mul(_DioramaWorldToIsland, float4(UNITY_MATRIX_M._m03_m13_m23, 1.0)).xyz;
    float s = DioramaEdgeDistance(origin.xz);
    islandPos.y -= _DioramaWindow.z * (1.0 - smoothstep(-_DioramaWindow.y, _DioramaWindow.y, s));
    return mul(_DioramaIslandToWorld, float4(islandPos, 1.0)).xyz;
}

void DioramaWindowClip(float3 islandPos, float on)
{
    if (on < 0.5) return;
    clip(islandPos.y - _DioramaWindow.x);
    clip(DioramaEdgeDistance(islandPos.xz));
}

// Map-space position of a world point for island content; world space for everything else.
float3 DioramaMapPoint(float3 positionWS, float on)
{
    return on > 0.5 ? mul(_DioramaWorldToMap, float4(positionWS, 1.0)).xyz : positionWS;
}

#endif
