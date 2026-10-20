#ifndef DIORAMA_TOON_COMMON_INCLUDED
#define DIORAMA_TOON_COMMON_INCLUDED

// Shared by Toon.shader and ToonOutline.shader. Both use the same material layout, so the
// outline material can carry the same breathe / jiggle values as the body it outlines.
// Ported from toon() in Docs/Reference/creature-diorama-v2.html.

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _BaseColor;
    float4 _TopColor;
    float  _Grad;

    float4 _BellyColor;
    float4 _BellyDir;
    float  _BellyAt;
    float  _BellyOn;
    float  _BellyMaxY;

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

    float  _Spec;
    float  _Emissive;
    float  _Flash;
    float  _OriginJitter;

    float  _Breathe;
    float  _Jiggle;

    float  _OutlineWidth;
    float4 _OutlineColor;
CBUFFER_END

// Breathe squashes the body vertically and widens it; jiggle wobbles it along the normal.
float3 ToonDeform(float3 positionOS, float3 normalOS)
{
    float t = _Time.y;
    float br = sin(t * 2.6) * _Breathe;
    float3 p = positionOS;
    p.y *= 1.0 - br * 0.6;
    p.xz *= 1.0 + br * 0.4;
    p += normalOS * sin(t * 5.0 + positionOS.y * 5.0 + positionOS.x * 3.0) * _Jiggle;
    return p;
}

// 0..1 hash of the object (or instance) origin. Gives each instanced tile its own shade.
float ToonOriginHash()
{
    float3 o = UNITY_MATRIX_M._m03_m13_m23;
    return frac(sin(dot(o.xz, float2(127.1, 311.7))) * 43758.5453);
}

#endif
