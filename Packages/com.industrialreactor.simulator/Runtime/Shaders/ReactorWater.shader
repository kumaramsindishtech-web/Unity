// Industrial Reactor Simulator - Realistic Water Shader (URP)
// Copyright (c) 2024 Industrial Reactor Simulator. MIT License.
// Features: Z-axis fill, depth-based coloring, caustics, foam, fresnel, waves

Shader "Industrial Reactor/Water"
{
    Properties
    {
        [Header(Water Colors)]
        _ShallowColor ("Shallow Color", Color) = (0.4, 0.75, 0.85, 0.7)
        _DeepColor ("Deep Color", Color) = (0.1, 0.35, 0.55, 0.95)
        _FresnelColor ("Fresnel Highlight", Color) = (0.9, 0.97, 1.0, 1.0)
        
        [Header(Water Fill Settings)]
        _WaterLevel ("Water Level", Range(0, 1)) = 1.0
        [Tooltip("0 = Y-axis fill, 1 = Z-axis fill")]
        _FillAxis ("Fill Axis (0=Y, 1=Z)", Range(0, 1)) = 1.0
        _FillMin ("Fill Min Position", Float) = 0.0
        _FillMax ("Fill Max Position", Float) = 1.0
        
        [Header(Transparency and Depth)]
        _Transparency ("Base Transparency", Range(0, 1)) = 0.75
        _DepthFade ("Depth Fade Distance", Range(0.1, 10)) = 2.0
        _FresnelPower ("Fresnel Power", Range(1, 10)) = 3.5
        _Clarity ("Water Clarity", Range(0, 1)) = 0.6
        
        [Header(Surface Waves)]
        _WaveSpeed ("Wave Speed", Range(0, 5)) = 0.8
        _WaveScale ("Wave Scale", Range(1, 50)) = 12.0
        _WaveAmplitude ("Wave Height", Range(0, 0.1)) = 0.015
        _MicroWaveScale ("Micro Wave Scale", Range(10, 100)) = 35.0
        _MicroWaveAmplitude ("Micro Wave Height", Range(0, 0.05)) = 0.004
        
        [Header(Caustics)]
        _CausticsIntensity ("Caustics Intensity", Range(0, 1)) = 0.25
        _CausticsScale ("Caustics Scale", Range(1, 20)) = 8.0
        _CausticsSpeed ("Caustics Speed", Range(0, 2)) = 0.4
        
        [Header(Surface Foam)]
        _FoamColor ("Foam Color", Color) = (1, 1, 1, 0.9)
        _FoamThreshold ("Foam Edge Threshold", Range(0, 0.5)) = 0.08
        _FoamSoftness ("Foam Softness", Range(0.001, 0.3)) = 0.04
        _FoamNoiseScale ("Foam Noise Scale", Range(1, 50)) = 15.0
        
        [Header(Swirl Effect)]
        _SwirlSpeed ("Swirl Speed (deg/s)", Range(0, 360)) = 0
        _SwirlStrength ("Swirl Strength", Range(0, 1)) = 0.5
        _SwirlCenter ("Swirl Center", Vector) = (0.5, 0.5, 0, 0)
        
        [Header(Turbulence)]
        _Turbulence ("Turbulence", Range(0, 1)) = 0
        _TurbulenceScale ("Turbulence Scale", Range(0.1, 10)) = 3
        _TurbulenceSpeed ("Turbulence Speed", Range(0, 5)) = 1.5
        
        [Header(Emission)]
        [HDR] _EmissionColor ("Emission Color", Color) = (0.2, 0.5, 1, 1)
        _EmissionIntensity ("Emission Intensity", Range(0, 5)) = 0
        _EmissionPulseSpeed ("Pulse Speed", Range(0, 5)) = 1
        
        [Header(Textures)]
        _BaseMap ("Base Texture", 2D) = "white" {}
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Range(0, 2)) = 0.5
        
        [Header(Lighting)]
        _Smoothness ("Smoothness", Range(0, 1)) = 0.95
        _SpecularIntensity ("Specular Intensity", Range(0, 2)) = 1.0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 positionOS : TEXCOORD2;
                float3 normalWS : TEXCOORD3;
                float4 tangentWS : TEXCOORD4;
                float4 screenPos : TEXCOORD5;
                float fogFactor : TEXCOORD6;
            };

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _ShallowColor;
                float4 _DeepColor;
                float4 _FresnelColor;
                float _WaterLevel;
                float _FillAxis;
                float _FillMin;
                float _FillMax;
                float _Transparency;
                float _DepthFade;
                float _FresnelPower;
                float _Clarity;
                float _WaveSpeed;
                float _WaveScale;
                float _WaveAmplitude;
                float _MicroWaveScale;
                float _MicroWaveAmplitude;
                float _CausticsIntensity;
                float _CausticsScale;
                float _CausticsSpeed;
                float4 _FoamColor;
                float _FoamThreshold;
                float _FoamSoftness;
                float _FoamNoiseScale;
                float _SwirlSpeed;
                float _SwirlStrength;
                float4 _SwirlCenter;
                float _Turbulence;
                float _TurbulenceScale;
                float _TurbulenceSpeed;
                float4 _EmissionColor;
                float _EmissionIntensity;
                float _EmissionPulseSpeed;
                float _BumpScale;
                float _Smoothness;
                float _SpecularIntensity;
            CBUFFER_END

            // Hash functions for procedural noise
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float2 hash2(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return -1.0 + 2.0 * frac(sin(p) * 43758.5453123);
            }

            // Gradient noise
            float gradientNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                
                return lerp(lerp(dot(hash2(i + float2(0, 0)), f - float2(0, 0)),
                                 dot(hash2(i + float2(1, 0)), f - float2(1, 0)), u.x),
                            lerp(dot(hash2(i + float2(0, 1)), f - float2(0, 1)),
                                 dot(hash2(i + float2(1, 1)), f - float2(1, 1)), u.x), u.y);
            }

            // Layered caustics effect
            float caustics(float2 uv, float time)
            {
                float2 uv1 = uv * _CausticsScale + float2(time * _CausticsSpeed, time * _CausticsSpeed * 0.7);
                float2 uv2 = uv * _CausticsScale * 1.4 + float2(-time * _CausticsSpeed * 0.8, time * _CausticsSpeed * 0.5);
                float2 uv3 = uv * _CausticsScale * 0.8 + float2(time * _CausticsSpeed * 0.3, -time * _CausticsSpeed * 0.6);
                
                float c1 = gradientNoise(uv1);
                float c2 = gradientNoise(uv2);
                float c3 = gradientNoise(uv3);
                
                // Combine layers with different weights
                float caustic = (c1 * 0.5 + c2 * 0.3 + c3 * 0.2);
                caustic = saturate(caustic * 0.5 + 0.5);
                caustic = pow(caustic, 1.5); // Sharpen
                
                return caustic;
            }

            // Swirl UV transformation
            float2 SwirlUV(float2 uv, float2 center, float angle, float strength)
            {
                float2 delta = uv - center;
                float dist = length(delta);
                float falloff = 1.0 - saturate(dist * 2.0);
                float rotation = angle * strength * falloff * falloff;
                float s = sin(rotation);
                float c = cos(rotation);
                float2x2 rotMatrix = float2x2(c, -s, s, c);
                return center + mul(rotMatrix, delta);
            }

            // Foam noise
            float foamNoise(float2 uv, float time)
            {
                float n1 = gradientNoise(uv * _FoamNoiseScale + time * 0.5);
                float n2 = gradientNoise(uv * _FoamNoiseScale * 2.0 - time * 0.3);
                return saturate((n1 + n2) * 0.5 + 0.5);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                float3 posOS = input.positionOS.xyz;
                output.positionOS = posOS;
                
                // Determine surface coordinate based on fill axis
                float2 waveUV = lerp(posOS.xz, posOS.xy, _FillAxis);
                float time = _Time.y * _WaveSpeed;
                
                // Multi-layer wave displacement
                float wave1 = sin(waveUV.x * _WaveScale + time) * cos(waveUV.y * _WaveScale * 0.8 + time * 0.9);
                float wave2 = sin(waveUV.x * _MicroWaveScale + time * 1.6) * cos(waveUV.y * _MicroWaveScale + time * 1.4);
                float wave3 = sin((waveUV.x + waveUV.y) * _WaveScale * 0.6 + time * 0.7);
                
                float totalWave = wave1 * _WaveAmplitude + wave2 * _MicroWaveAmplitude + wave3 * _WaveAmplitude * 0.4;
                
                // Check if this is the top surface (UV.y close to 1)
                float isSurface = smoothstep(0.85, 0.95, input.uv.y);
                
                // Apply wave displacement based on fill axis
                if (_FillAxis > 0.5)
                    posOS.z += totalWave * isSurface * _WaterLevel;
                else
                    posOS.y += totalWave * isSurface * _WaterLevel;
                
                VertexPositionInputs posInputs = GetVertexPositionInputs(posOS);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.tangentWS = float4(normalInputs.tangentWS, input.tangentOS.w);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.screenPos = ComputeScreenPos(output.positionCS);
                output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float time = _Time.y;
                
                // Swirl UV
                float swirlAngle = _SwirlSpeed * time * 0.0174533; // Convert to radians
                float2 swirlUV = SwirlUV(input.uv, _SwirlCenter.xy, swirlAngle, _SwirlStrength);
                
                // Turbulence offset
                float2 turbulenceOffset = float2(
                    gradientNoise(swirlUV * _TurbulenceScale + time * _TurbulenceSpeed),
                    gradientNoise(swirlUV * _TurbulenceScale + time * _TurbulenceSpeed + 100)
                ) * _Turbulence * 0.1;
                
                float2 finalUV = swirlUV + turbulenceOffset;
                
                // Sample base texture and normal
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, finalUV);
                
                // Normal mapping
                float2 normalUV1 = finalUV + time * float2(0.02, 0.015);
                float2 normalUV2 = finalUV * 1.3 + time * float2(-0.018, 0.022);
                float3 normalTS1 = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, normalUV1), _BumpScale);
                float3 normalTS2 = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, normalUV2), _BumpScale * 0.6);
                float3 normalTS = normalize(float3(normalTS1.xy + normalTS2.xy, normalTS1.z));
                
                // Construct TBN matrix
                float3 bitangent = cross(input.normalWS, input.tangentWS.xyz) * input.tangentWS.w;
                float3x3 TBN = float3x3(input.tangentWS.xyz, bitangent, input.normalWS);
                float3 normalWS = normalize(mul(normalTS, TBN));
                
                // View direction
                float3 viewDir = normalize(GetWorldSpaceViewDir(input.positionWS));
                
                // Fresnel
                float NdotV = saturate(dot(normalWS, viewDir));
                float fresnel = pow(1.0 - NdotV, _FresnelPower);
                
                // Depth-based coloring
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                
                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE)
                    float sceneDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                    float waterDepth = input.screenPos.w;
                    float depthDiff = sceneDepth - waterDepth;
                #else
                    float depthDiff = 1.0;
                #endif
                
                float depthFactor = saturate(depthDiff / _DepthFade);
                
                // Blend shallow to deep color based on depth
                float4 waterColor = lerp(_ShallowColor, _DeepColor, depthFactor);
                waterColor.rgb *= baseColor.rgb;
                
                // Add fresnel highlight
                waterColor.rgb = lerp(waterColor.rgb, _FresnelColor.rgb, fresnel * 0.4);
                
                // Caustics (more visible in shallow areas)
                float2 causticsUV = lerp(input.positionWS.xz, input.positionWS.xy, _FillAxis);
                float causticsValue = caustics(causticsUV, time);
                waterColor.rgb += causticsValue * _CausticsIntensity * (1.0 - depthFactor) * _ShallowColor.rgb;
                
                // Edge foam
                float foamMask = foamNoise(finalUV, time);
                float edgeFoam = 1.0 - saturate((depthDiff - _FoamThreshold) / _FoamSoftness);
                edgeFoam *= foamMask;
                waterColor.rgb = lerp(waterColor.rgb, _FoamColor.rgb, edgeFoam * _FoamColor.a);
                
                // Lighting
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                
                // Diffuse with wrap lighting for softer look
                float3 diffuse = waterColor.rgb * mainLight.color * (NdotL * 0.5 + 0.5);
                
                // Specular
                float3 halfDir = normalize(mainLight.direction + viewDir);
                float NdotH = saturate(dot(normalWS, halfDir));
                float specular = pow(NdotH, _Smoothness * 256.0) * _SpecularIntensity;
                
                // Emission
                float pulse = sin(time * _EmissionPulseSpeed) * 0.5 + 0.5;
                float3 emission = _EmissionColor.rgb * _EmissionIntensity * (0.6 + pulse * 0.4);
                
                // Final color
                float3 finalColor = diffuse + specular * mainLight.color + emission;
                
                // Subsurface scattering approximation
                float3 ssDir = normalize(mainLight.direction + normalWS * 0.5);
                float ssFactor = saturate(dot(viewDir, -ssDir));
                ssFactor = pow(ssFactor, 3.0) * 0.15;
                finalColor += _ShallowColor.rgb * ssFactor * mainLight.color;
                
                // Alpha
                float alpha = _Transparency;
                alpha = lerp(alpha, alpha * 0.6, _Clarity);
                alpha = lerp(alpha, 1.0, fresnel * 0.4);
                alpha = lerp(alpha, _FoamColor.a, edgeFoam);
                
                // Edge softness at water boundary
                float edgeSoft = saturate(depthDiff / 0.1);
                alpha *= edgeSoft;
                
                // Apply fog
                finalColor = MixFog(finalColor, input.fogFactor);
                
                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
