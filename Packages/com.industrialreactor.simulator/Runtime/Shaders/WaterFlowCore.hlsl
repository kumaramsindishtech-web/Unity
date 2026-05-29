// ============================================================================
// Industrial Reactor Simulator - Shared Water Flow Core (URP HLSL)
//
// Realistic flowing-water look (flow, foam, caustics, fresnel, surface band)
// ported from the Built-in/CG "Custom/PipeWaterFlow" shader to URP.
//
// Used by BOTH:
//   - ReactorWater.shader  (tank water,  fill = _WaterLevel)
//   - GlassPipe.shader     (pipe water,  fill = _FillProgress)
//
// The fill is driven by OBJECT-SPACE bounds (passed as _FillMin/_FillMax/
// _FillAxis), NOT by UVs, so it never "fills from the middle".
//
// Each shader must declare the appearance properties below in its own
// CBUFFER(UnityPerMaterial) BEFORE including this file.
// ============================================================================
#ifndef INDUSTRIAL_REACTOR_WATER_FLOW_CORE_INCLUDED
#define INDUSTRIAL_REACTOR_WATER_FLOW_CORE_INCLUDED

// ---- noise helpers ---------------------------------------------------------
float IR_hash21(float2 p)
{
    p = frac(p * float2(127.1, 311.7));
    return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
}

float2 IR_hash22(float2 p)
{
    p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
    return frac(sin(p) * 43758.5453);
}

float IR_valueNoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);
    return lerp(lerp(IR_hash21(i),                 IR_hash21(i + float2(1, 0)), u.x),
                lerp(IR_hash21(i + float2(0, 1)),   IR_hash21(i + float2(1, 1)), u.x), u.y);
}

float IR_fbm(float2 p, int oct)
{
    float v = 0.0, a = 0.5, f = 1.0;
    for (int n = 0; n < oct; n++) { v += a * IR_valueNoise(p * f); a *= 0.5; f *= 2.05; }
    return v;
}

float IR_voronoi(float2 p, float causticsSpeed)
{
    float2 i = floor(p), ff = frac(p);
    float md = 8.0;
    for (int y = -1; y <= 1; y++)
    for (int x = -1; x <= 1; x++)
    {
        float2 nb = float2(x, y);
        float2 rp = IR_hash22(i + nb);
        rp = 0.5 + 0.5 * sin(rp * 6.2831 + _Time.y * causticsSpeed);
        md = min(md, length(nb + rp - ff));
    }
    return md;
}

// ---- main water color ------------------------------------------------------
// cylUV     : x = around the cross-section (0..1), y = coordinate along the length / fill axis
// fillCoord : 0 at the start of the fill, 1 at the end
// fillLevel : current fill amount (0..1)
// alphaMul  : extra alpha multiplier (e.g. pipe flow intensity; 1 for tank)
// flowDir   : +1 or -1 -> direction the water flows along cylUV.y
half4 IR_ComputeWaterFlow(float2 cylUV, float fillCoord, float fillLevel,
                          float3 normalWS, float3 viewWS, float alphaMul, float flowDir)
{
    float t = _Time.y + _TimeOffset;

    // surface line (wavy across the cross-section)
    float sw = sin(cylUV.x * _SurfaceWaveFreq + t * 3.5) * _SurfaceWaveAmp
             + sin(cylUV.x * _SurfaceWaveFreq * 1.7 + t * 2.1) * _SurfaceWaveAmp * 0.5;
    float surfaceY     = fillLevel + sw;
    float belowSurface = step(fillCoord, surfaceY); // 1 = below water line

    // flow field (scrolls along cylUV.y in the chosen direction)
    float2 uv  = cylUV;
    float2 fUV = uv; fUV.y -= t * _FlowSpeed * 0.08 * flowDir;
    float2 warp = float2(IR_fbm(fUV * 2.0 + float2(0,   t * 0.12), 2),
                         IR_fbm(fUV * 2.0 + float2(3.7, t * 0.09), 2));
    float2 wUV  = fUV + (warp - 0.5) * _Turbulence * 0.4;

    float f1 = IR_fbm(wUV * _FlowTiling, 3);
    float f2 = IR_fbm(wUV * _FlowTiling * 1.3 + float2(5.1, 2.7) + t * 0.07, 3);
    float flowMask = saturate((lerp(f1, f2, 0.5) - 0.2) / 0.6);

    // perturbed normal
    float eps = 0.005;
    float2 nUV = wUV * _FlowTiling + float2(0, -t * _FlowSpeed * 0.1 * flowDir);
    float h0 = IR_fbm(nUV, 3);
    float hx = IR_fbm(nUV + float2(eps, 0), 3);
    float hy = IR_fbm(nUV + float2(0, eps), 3);
    float3 tN = normalize(float3((h0 - hx) / eps * _WaveHeight, 1.0, (h0 - hy) / eps * _WaveHeight));
    float3 N  = normalize(normalWS + tN * _WaveHeight * 0.5);
    float3 V  = normalize(viewWS);

    // fake sun (independent of scene lights)
    float3 L = normalize(float3(0.5, 1.0, 0.3));
    float3 H = normalize(L + V);
    float NdotL = saturate(dot(N, L));
    float NdotH = saturate(dot(N, H));

    float spec    = saturate(pow(NdotH, _SpecularPower) * _SpecularStrength);
    float fresnel = saturate(pow(1.0 - saturate(dot(N, V)), _FresnelPower)) * 0.3;

    // base color
    float depth = saturate(flowMask * _DepthStrength + fresnel);
    half3 col   = lerp(_ShallowColor.rgb, _DeepColor.rgb, depth);
    col *= (NdotL * 0.4 + 0.6);

    // caustics
    float2 cUV  = wUV * _CausticsScale + float2(t * _CausticsSpeed * 0.3, 0);
    float caust = pow(saturate((1.0 - IR_voronoi(cUV, _CausticsSpeed))
                * (1.0 - IR_voronoi(cUV * 1.4 + 1.7, _CausticsSpeed) * 0.5) * 2.0), 2.5);
    col += caust * _CausticsStrength * 0.15;

    col += spec * 0.3;
    col += fresnel * 0.2;

    // flow foam
    float2 foamUV = uv; foamUV.y -= t * _FoamSpeed * 0.05 * flowDir;
    float2 fmW = float2(IR_fbm(foamUV * _FoamTiling + float2(1.3, t * 0.08), 2),
                        IR_fbm(foamUV * _FoamTiling + float2(4.1, t * 0.06), 2));
    float foamN = IR_fbm(foamUV * _FoamTiling + (fmW - 0.5) * 0.3, 3);
    float foamM = saturate(1.0 - flowMask * 1.8) + smoothstep(0.5 - _EdgeFoam, 0.5, abs(uv.x));
    float foam  = smoothstep(1.0 - _FoamAmount, 1.0, foamN) * saturate(foamM);
    col = lerp(col, _FoamColor.rgb, foam);

    col = saturate(col);

    // alpha
    float alpha = lerp(_ShallowColor.a, _DeepColor.a, depth);
    alpha = saturate(alpha + foam * 0.3 + spec * 0.1);

    // surface foam band at the water line
    float dist = surfaceY - fillCoord;
    float sfN  = IR_fbm(float2(uv.x * 8.0 + t * 0.4, t * 0.3), 2);
    float sfB  = smoothstep(_SurfaceFoamWidth, 0.0, dist)
               * smoothstep(0.0, _SurfaceFoamWidth * 0.5, dist + 0.002)
               * lerp(0.5, 1.0, sfN);
    col   = saturate(lerp(col, _FoamColor.rgb, sfB));
    alpha = saturate(lerp(alpha, 1.0, sfB));

    // bottom cap fade + fill mask + external multiplier
    alpha *= smoothstep(0.0, _CapFade, fillCoord);
    alpha *= belowSurface;
    alpha *= saturate(alphaMul);

    return half4(col, alpha);
}

// Normalized 0..1 coordinate along the chosen object-space fill axis.
// fillAxis: 0 = X, 1 = Y, 2 = Z. invert flips the direction.
float IR_GetFillCoord(float3 posOS, float fillAxis, float fillMin, float fillMax, float invert)
{
    float v = (fillAxis < 0.5) ? posOS.x : (fillAxis < 1.5 ? posOS.y : posOS.z);
    float range = max(fillMax - fillMin, 1e-4);
    float c = saturate((v - fillMin) / range);
    return (invert > 0.5) ? (1.0 - c) : c;
}

// Angle around the cylinder's local Y axis, mapped to 0..1.
float2 IR_CylUV(float3 posOS, float fillCoord)
{
    float a = atan2(posOS.z, posOS.x) * 0.15915494; // 1/(2*PI)
    return float2(a, fillCoord);
}

#endif // INDUSTRIAL_REACTOR_WATER_FLOW_CORE_INCLUDED
