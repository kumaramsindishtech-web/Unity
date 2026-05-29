// ============================================================================
// Industrial Reactor Simulator - Water Shader (URP)
//
// Fills BOTTOM -> TOP using the mesh's real OBJECT-SPACE height bounds.
// The script (WaterSimulator) passes:
//     _WaterLevel : 0..1 fill amount
//     _FillMin    : object-space minimum along the fill axis
//     _FillMax    : object-space maximum along the fill axis
//     _FillAxis   : 0 = fill along Y, 1 = fill along Z
//
// Because clipping is based on real geometry bounds (NOT UVs and NOT the
// pivot), the water can never "fill from the middle".
// ============================================================================

Shader "Industrial Reactor/Water"
{
    Properties
    {
        _Color        ("Water Color", Color) = (0.2, 0.5, 0.8, 0.7)
        _DeepColor    ("Deep Color", Color)  = (0.1, 0.3, 0.5, 0.9)

        _WaterLevel   ("Water Level (0-1)", Range(0,1)) = 0.0
        _FillMin      ("Fill Min (object space)", Float) = -0.5
        _FillMax      ("Fill Max (object space)", Float) =  0.5
        _FillAxis     ("Fill Axis (0=Y, 1=Z)", Float)    =  0

        _Smoothness   ("Smoothness", Range(0,1)) = 0.9
        _FresnelPower ("Fresnel Power", Range(1,5)) = 2.0
        _EmissionIntensity ("Emission Intensity", Range(0,5)) = 0.0

        [Header(Surface Waves)]
        _WaveSpeed    ("Wave Speed", Float) = 1.0
        _WaveStrength ("Wave Strength", Range(0,0.1)) = 0.02
        _WaveFrequency("Wave Frequency", Float) = 8.0
        [Enum(X,0,Z,1,Both,2)] _WaveAxis ("Wave Axis", Float) = 1

        [Header(Swirl)]
        _SwirlSpeed   ("Swirl Speed", Range(0,200)) = 0
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
            Cull Off          // show inner surface of the water volume too

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float3 positionOS  : TEXCOORD0;  // object space (for fill clip)
                float3 positionWS  : TEXCOORD1;
                float3 normalWS    : TEXCOORD2;
                float2 uv          : TEXCOORD3;
                float  fillCoord   : TEXCOORD4;  // 0 at bottom, 1 at top
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _DeepColor;
                half  _WaterLevel;
                half  _FillMin;
                half  _FillMax;
                half  _FillAxis;
                half  _Smoothness;
                half  _FresnelPower;
                half  _EmissionIntensity;
                half  _WaveSpeed;
                half  _WaveStrength;
                half  _WaveFrequency;
                half  _WaveAxis;
                half  _SwirlSpeed;
            CBUFFER_END

            // Normalized 0..1 height of a vertex/fragment along the fill axis.
            float GetFillCoord(float3 posOS)
            {
                float v = (_FillAxis > 0.5) ? posOS.z : posOS.y;
                float range = max(_FillMax - _FillMin, 1e-4);
                return saturate((v - _FillMin) / range);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 posOS = IN.positionOS.xyz;
                float fillCoord = GetFillCoord(posOS);

                // Gentle surface waves near the water top only.
                // _WaveAxis selects which axis the ripples travel along:
                //   0 = X only, 1 = Z only (default), 2 = both (radial-ish)
                float t = _Time.y * _WaveSpeed;
                float freq = _WaveFrequency;
                float wave;
                if (_WaveAxis < 0.5)        // X only
                    wave = sin(posOS.x * freq + t);
                else if (_WaveAxis < 1.5)   // Z only
                    wave = sin(posOS.z * freq + t);
                else                        // Both
                    wave = sin(posOS.x * freq + t) * cos(posOS.z * (freq * 0.8) + t * 0.7);

                float topMask = smoothstep(_WaterLevel - 0.08, _WaterLevel, fillCoord);
                posOS.y += wave * _WaveStrength * topMask;

                OUT.positionOS = IN.positionOS.xyz; // unmodified for stable clip
                OUT.positionCS = TransformObjectToHClip(posOS);
                OUT.positionWS = TransformObjectToWorld(posOS);
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv         = IN.uv;
                OUT.fillCoord  = fillCoord;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Recompute precise fill coordinate per-fragment for a crisp line
                float fillCoord = GetFillCoord(IN.positionOS);

                // BOTTOM -> TOP: keep fragments below the water line, clip above
                clip(_WaterLevel - fillCoord + 1e-4);

                // Depth-based tint (deeper = darker)
                float depth = saturate(1.0 - (fillCoord / max(_WaterLevel, 0.01)));
                half4 col = lerp(_Color, _DeepColor, depth * 0.6);

                float3 N = normalize(IN.normalWS);
                float3 V = normalize(GetWorldSpaceViewDir(IN.positionWS));

                // Fresnel rim
                float fres = pow(1.0 - saturate(dot(N, V)), _FresnelPower);
                col.rgb += fres * 0.15;

                // Main light
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(N, mainLight.direction));
                col.rgb *= mainLight.color * (NdotL * 0.5 + 0.5);

                // Specular highlight
                float3 H = normalize(mainLight.direction + V);
                float spec = pow(saturate(dot(N, H)), _Smoothness * 100.0 + 1.0);
                col.rgb += spec * mainLight.color * 0.3;

                // Swirl pattern (driven by agitator)
                float2 c = IN.uv - 0.5;
                float ang = _SwirlSpeed * _Time.y * 0.01;
                float dist = length(c);
                float swirl = sin(dist * 20.0 - ang) * 0.5 + 0.5;
                col.rgb += swirl * 0.05 * _Color.rgb;

                // Emission glow
                col.rgb += _Color.rgb * _EmissionIntensity;

                // Brighten the very top surface line slightly
                float surface = smoothstep(_WaterLevel - 0.02, _WaterLevel, fillCoord);
                col.rgb += surface * 0.2;

                return col;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
