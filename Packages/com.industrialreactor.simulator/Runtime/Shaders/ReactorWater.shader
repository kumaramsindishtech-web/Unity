// ============================================================================
// Industrial Reactor Simulator - Tank Water Shader (URP)
//
// Realistic flowing water (flow/foam/caustics/fresnel/surface band) shared with
// the pipe shader via WaterFlowCore.hlsl.
//
// Fill is driven BOTTOM -> TOP by the mesh's real OBJECT-SPACE bounds, so it
// never fills from the middle. The WaterSimulator passes:
//     _WaterLevel : 0..1 fill amount
//     _FillMin    : object-space minimum along the fill axis
//     _FillMax    : object-space maximum along the fill axis
//     _FillAxis   : 0 = X, 1 = Y (default), 2 = Z
//     _SwirlSpeed / _EmissionIntensity : agitator + glow
// ============================================================================

Shader "Industrial Reactor/Water"
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

        [Header(Fill (driven by WaterSimulator))]
        _WaterLevel       ("Water Level (0-1)",         Range(0,1)) = 0.0
        _FillMin          ("Fill Min (object space)",   Float)      = -0.5
        _FillMax          ("Fill Max (object space)",   Float)      =  0.5
        _FillAxis         ("Fill Axis (0=X,1=Y,2=Z)",   Float)      =  1
        [Toggle] _InvertFill ("Invert Fill", Float) = 0

        [Header(Extras)]
        _SwirlSpeed        ("Swirl Speed",        Range(0,200)) = 0
        _EmissionIntensity ("Emission Intensity", Range(0,5))   = 0.0
        [HideInInspector] _TimeOffset ("Time Offset", Float)    = 0
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
            Name "WaterForward"
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
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
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
                half  _WaterLevel, _FillMin, _FillMax, _FillAxis, _InvertFill;
                half  _SwirlSpeed, _EmissionIntensity, _TimeOffset;
            CBUFFER_END

            #include "WaterFlowCore.hlsl"

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionOS = IN.positionOS.xyz;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float fillCoord = IR_GetFillCoord(IN.positionOS, _FillAxis, _FillMin, _FillMax, _InvertFill);
                float2 cylUV    = IR_CylUV(IN.positionOS, fillCoord);
                float3 viewWS   = GetWorldSpaceViewDir(IN.positionWS);

                half4 col = IR_ComputeWaterFlow(cylUV, fillCoord, _WaterLevel, IN.normalWS, viewWS, 1.0);

                // Swirl tint (driven by agitator) + emission glow
                float ang   = _SwirlSpeed * _Time.y * 0.01;
                float swirl = sin(length(cylUV - 0.5) * 20.0 - ang) * 0.5 + 0.5;
                col.rgb += swirl * 0.04 * _ShallowColor.rgb;
                col.rgb += _ShallowColor.rgb * _EmissionIntensity;
                col.rgb = saturate(col.rgb);

                clip(col.a - 0.001);
                return col;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
