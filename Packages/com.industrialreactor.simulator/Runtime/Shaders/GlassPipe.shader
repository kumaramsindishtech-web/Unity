// ============================================================================
// Industrial Reactor Simulator - Pipe Water Shader (URP) - UV SCROLL FILL
//
// Realistic flowing water inside the pipe, sharing the look of the tank water
// via WaterFlowCore.hlsl.
//
// FILL METHOD: UV based. The fill and the flow run along the mesh's V (UV.y),
// so they follow the pipe's length INCLUDING bends (as long as the pipe mesh
// is unwrapped with V running 0..1 along the centreline).
//
// Direction:
//   _FlowDir   : +1 = fill/flow toward +V, -1 = toward -V  (material dropdown)
//   _InvertFill: runtime flip (set by PipeFlowVisualizer for reverse flow)
//
// Driven by PipeFlowVisualizer:
//   _FillProgress  : 0..1 how far the water has travelled along the pipe
//   _FlowSpeed     : scroll speed
//   _FlowIntensity : overall opacity (fade in/out)
// ============================================================================

Shader "Industrial Reactor/Glass Pipe"
{
    Properties
    {
        [Header(Water Colors)]
        _ShallowColor     ("Shallow Color", Color) = (0.15, 0.55, 0.75, 0.8)
        _DeepColor        ("Deep Color",    Color) = (0.02, 0.18, 0.45, 1.0)
        _FoamColor        ("Foam Color",    Color) = (0.88, 0.94, 0.97, 1.0)

        [Header(Flow)]
        _FlowSpeed        ("Flow Speed",        Range(0,5))   = 1.8
        _FlowTiling       ("Flow Tiling",       Range(0.1,8)) = 2.5
        _Turbulence       ("Turbulence",        Range(0,1))   = 0.35
        _WaveHeight       ("Ripple Strength",   Range(0,1))   = 0.4

        [Header(Lighting)]
        _SpecularPower    ("Specular Power",    Range(1,256)) = 80.0
        _SpecularStrength ("Specular Strength", Range(0,1))   = 0.4
        _FresnelPower     ("Fresnel Power",     Range(0.5,8)) = 2.5

        [Header(Foam)]
        _FoamAmount       ("Foam Amount",       Range(0,1))   = 0.35
        _FoamSpeed        ("Foam Speed",        Range(0,3))   = 0.6
        _FoamTiling       ("Foam Tiling",       Range(0.1,8)) = 4.0

        [Header(Caustics)]
        _CausticsScale    ("Caustics Scale",    Range(0.1,8)) = 3.0
        _CausticsSpeed    ("Caustics Speed",    Range(0,2))   = 0.5
        _CausticsStrength ("Caustics Strength", Range(0,1))   = 0.2

        [Header(Blending)]
        _DepthStrength    ("Depth Blend",       Range(0,1))    = 0.6
        _EdgeFoam         ("Edge Foam",         Range(0,0.3))  = 0.08
        _CapFade          ("Cap Fade",          Range(0,0.2))  = 0.06

        [Header(Surface Line)]
        _SurfaceFoamWidth ("Surface Foam Width",     Range(0,0.15)) = 0.04
        _SurfaceWaveAmp   ("Surface Wave Amplitude", Range(0,0.05)) = 0.015
        _SurfaceWaveFreq  ("Surface Wave Frequency", Range(1,30))   = 12.0

        [Header(Fill (UV based))]
        _FillProgress     ("Fill Progress (0-1)", Range(0,1)) = 0.0
        [Enum(Plus V (UV.y +),1, Minus V (UV.y -),-1)] _FlowDir ("Fill / Scroll Direction", Float) = 1
        [Toggle] _InvertFill ("Invert (runtime)", Float) = 0
        _UVTiling         ("Length UV Tiling", Float) = 1.0
        _FlowIntensity    ("Flow Intensity (opacity)", Range(0,1)) = 1.0

        [HideInInspector] _TimeOffset ("Time Offset", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "PipeWaterForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor;
                half4 _DeepColor;
                half4 _FoamColor;
                half  _FlowSpeed, _FlowTiling, _Turbulence, _WaveHeight;
                half  _SpecularPower, _SpecularStrength, _FresnelPower;
                half  _FoamAmount, _FoamSpeed, _FoamTiling;
                half  _CausticsScale, _CausticsSpeed, _CausticsStrength;
                half  _DepthStrength, _EdgeFoam, _CapFade;
                half  _SurfaceFoamWidth, _SurfaceWaveAmp, _SurfaceWaveFreq;
                half  _FillProgress, _FlowDir, _InvertFill, _UVTiling;
                half  _FlowIntensity, _TimeOffset;
            CBUFFER_END

            #include "WaterFlowCore.hlsl"

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv         = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Effective direction: material dropdown combined with runtime invert.
                float dir = _FlowDir * ((_InvertFill > 0.5) ? -1.0 : 1.0);

                // V coordinate runs along the pipe length (follows bends).
                float v = frac(IN.uv.y * _UVTiling);

                // fillCoord = 0 at the entry end, 1 at the far end (per direction).
                float fillCoord = (dir > 0.0) ? v : (1.0 - v);

                // cylUV.x = around the pipe, cylUV.y = along the length.
                float2 cylUV  = float2(IN.uv.x, v);
                float3 viewWS = GetWorldSpaceViewDir(IN.positionWS);

                half4 col = IR_ComputeWaterFlow(cylUV, fillCoord, _FillProgress,
                                                IN.normalWS, viewWS, _FlowIntensity, dir);

                clip(col.a - 0.001);
                return col;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
