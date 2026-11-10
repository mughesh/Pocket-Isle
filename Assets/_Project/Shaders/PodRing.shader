// Step 29 C: the HP ring round a rim pod, always shown. A band round the quad's middle, filled clockwise from the top by
// _Fill in _FillColor over a dark track, with a thin dark outline so it reads on rock and in the room. _Alpha fades it.
Shader "Diorama/Pod Ring"
{
    Properties
    {
        _FillColor ("Fill", Color) = (0.44, 0.82, 0.42, 1)
        _TrackColor ("Track", Color) = (0.1, 0.09, 0.08, 0.75)
        _Outline ("Outline", Color) = (0.05, 0.04, 0.03, 0.9)
        _Fill ("Fill", Range(0, 1)) = 1
        _Inner ("Inner radius (of half size)", Range(0, 1)) = 0.76
        _Outer ("Outer radius (of half size)", Range(0, 1)) = 0.94
        _Alpha ("Alpha", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent+5" }

        Pass
        {
            Name "PodRing"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _FillColor;
                float4 _TrackColor;
                float4 _Outline;
                float _Fill;
                float _Inner;
                float _Outer;
                float _Alpha;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            // Nov 11 (Mugesh: the ring was blocky, thick and primitive): a slim bevelled channel that sits round the pod's
            // socket. A bronze bezel on both edges (lit from the top-left like the rim rock), a recessed dark track, and a
            // rounded, softly shaded fill tube with a specular streak; the fill's ends are round caps, not cut edges.
            float Cov(float d, float aa) { return saturate(0.5 - d / aa); }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float2 p = i.uv * 2.0 - 1.0;
                float r = length(p);
                float aa = max(fwidth(r), 1e-3);
                float mid = (_Inner + _Outer) * 0.5, half_ = (_Outer - _Inner) * 0.5;
                float d = abs(r - mid) - half_;                        // whole channel (negative inside)
                float across = clamp((r - mid) / max(half_, 1e-4), -1.0, 1.0); // -1 inner edge .. +1 outer edge
                float2 radial = p / max(r, 1e-4);
                float2 lightDir = normalize(float2(-0.45, 0.9));       // top-left, like the rim rock's key light

                // Bezel: the outer 28% of the channel on both sides, a bronze lip that catches the light.
                float bezelW = 0.22;
                float inBezel = smoothstep(1.0 - bezelW - 0.08, 1.0 - bezelW + 0.02, abs(across));
                float lip = dot(radial * sign(across), lightDir);      // the outer lip faces out, the inner one in
                float3 bronze = float3(0.62, 0.43, 0.24);
                float3 bezel = bronze * (0.72 + 0.45 * lip) + pow(saturate(lip), 6.0) * 0.35;

                // Track: recessed, darker toward the lit side (it is in the lip's shadow).
                float3 track = _TrackColor.rgb * (0.85 - 0.25 * dot(radial, lightDir) * across);

                // Fill tube: a rounded cross-section, sweeping clockwise from 12 o'clock with round caps.
                float tubeHalf = half_ * (1.0 - bezelW);
                float t = frac(atan2(p.x, p.y) / 6.2831853 + 1.0);
                float arcIn = saturate((_Fill - t) * 6.2831853 * mid / aa + 0.5);
                float ang = _Fill * 6.2831853;
                float2 endP = mid * float2(sin(ang), cos(ang));
                float2 startP = float2(0.0, mid);
                float caps = max(Cov(length(p - endP) - tubeHalf, aa), Cov(length(p - startP) - tubeHalf, aa));
                float tube = Cov(abs(r - mid) - tubeHalf, aa) * max(arcIn, caps * step(0.002, _Fill));
                tube *= step(0.002, _Fill);
                float s = saturate(abs(r - mid) / max(tubeHalf, 1e-4));
                float nz = sqrt(saturate(1.0 - s * s));                // tube normal's z (toward the viewer)
                float2 nxy = radial * ((r - mid) / max(tubeHalf, 1e-4));
                float diff = saturate(0.55 + 0.5 * dot(nxy, lightDir) + 0.25 * nz);
                float spec = pow(saturate(dot(normalize(float3(nxy, nz)), normalize(float3(lightDir * 0.7, 1.0)))), 24.0);
                float3 fill = _FillColor.rgb * diff + spec * 0.55;

                float3 rgb = lerp(track, bezel, inBezel);
                rgb = lerp(rgb, fill, tube * (1.0 - inBezel));
                float chan = Cov(d, aa);
                // A thin dark hairline outside the bezel, so it still reads against bright rock or a lit room.
                float hair = Cov(d - 0.018, aa) * (1.0 - chan);
                float a = max(chan * lerp(_TrackColor.a, 1.0, max(inBezel, tube)), hair * _Outline.a * 0.6);
                rgb = lerp(_Outline.rgb, rgb, saturate(chan / max(a, 1e-4)));
                return half4(rgb, a * _Alpha);
            }
            ENDHLSL
        }
    }
}
