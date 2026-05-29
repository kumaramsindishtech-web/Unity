// Industrial Reactor Simulator - Pipe Flow Shader (URP)
// Copyright (c) 2024 Industrial Reactor Simulator. MIT License.
// Features: UV-based flow (top to bottom), fill progress, realistic water flow visualization

Shader "Industrial Reactor/Pipe Flow"
{
    Properties
    {
        [Header(Base Pipe Properties)]
        _BaseColor ("Pipe Color", Color) = (0.5, 0.5, 0.55, 1)
        _BaseMap ("Base Texture", 2D) = "white" {}
        _Metallic ("Metallic", Range(0, 1)) = 0.7
        _Smoothness ("Smoothness", Range(0, 1)) = 0.6
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Range(0, 2)) = 1
        
        [Header(Water Flow Settings)]
        _FlowColor ("Water Color", Color) = (0.3, 0.65, 0.9, 0.85)
        _FlowColorDeep ("Water Deep Color", Color) = (0.15, 0.4, 0.6, 0.95)
        
        [Header(Flow Animation)]
        [Tooltip("Fill progress 0-1 (0=empty, 1=full)")]
        _FillProgress ("Fill Progress", Range(0, 1)) = 0
        [Tooltip("Flow speed in UV units per second")]
        _FlowSpeed ("Flow Speed", Range(0, 10)) = 2
        [Tooltip("Flow direction: 1 = top to bottom (Y+), -1 = bottom to top")]
        _FlowDirection ("Flow Direction", Range(-1, 1)) = 1
        _FlowIntensity ("Flow Intensity", Range(0, 1)) = 1
        
        [Header(Flow Appearance)]
        _FlowNoiseScale ("Flow Noise Scale", Range(1, 50)) = 15
        _FlowNoiseSpeed ("Flow Noise Speed", Range(0, 5)) = 1.5
        _FlowEdgeSoftness ("Edge Softness", Range(0.01, 0.3)) = 0.05
        
        [Header(Water Surface Details)]
        _WaterWaveScale ("Wave Scale", Range(1, 30)) = 10
        _WaterWaveSpeed ("Wave Speed", Range(0, 3)) = 1
        _WaterWaveAmplitude ("Wave Amplitude", Range(0, 0.1)) = 0.02
        
        [Header(Caustics in Pipe)]
        _CausticsIntensity ("Caustics Intensity", Range(0, 1)) = 0.15
        _CausticsScale ("Caustics Scale", Range(1, 20)) = 6
        _CausticsSpeed ("Caustics Speed", Range(0, 2)) = 0.5
        
        [Header(Emission)]
        [HDR] _FlowEmissionColor ("Flow Emission", Color) = (0.2, 0.5, 1, 1)
        _FlowEmission ("Emission Intensity", Range(0, 3)) = 0.3
        
        [Header(Fresnel)]
        _FresnelPower ("Fresnel Power", Range(1, 10)) = 3
        _FresnelColor ("Fresnel Color", Color) = (0.8, 0.95, 1, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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
                float3 normalWS : TEXCOORD2;
                float4 tangentWS : TEXCOORD3;
                float fogFactor : TEXCOORD4;
            };

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _Metallic;
                float _Smoothness;
                float _BumpScale;
                float4 _FlowColor;
                float4 _FlowColorDeep;
                float _FillProgress;
                float _FlowSpeed;
                float _FlowDirection;
                float _FlowIntensity;
                float _FlowNoiseScale;
                float _FlowNoiseSpeed;
                float _FlowEdgeSoftness;
                float _WaterWaveScale;
                float _WaterWaveSpeed;
                float _WaterWaveAmplitude;
                float _CausticsIntensity;
                float _CausticsScale;
                float _CausticsSpeed;
                float4 _FlowEmissionColor;
                float _FlowEmission;
                float _FresnelPower;
                float4 _FresnelColor;
            CBUFFER_END

            // Noise functions
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float2 hash2(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return -1.0 + 2.0 * frac(sin(p) * 43758.5453123);
            }

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

            float voronoiNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float minDist = 1.0;
                
                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 neighbor = float2(x, y);
                        float2 point = hash2(i + neighbor) * 0.5 + 0.5;
                        float dist = length(neighbor + point - f);
                        minDist = min(minDist, dist);
                    }
                }
                return minDist;
            }

            // Caustics pattern
            float caustics(float2 uv, float time)
            {
                float2 uv1 = uv * _CausticsScale + float2(time * _CausticsSpeed, time * _CausticsSpeed * 0.7);
                float2 uv2 = uv * _CausticsScale * 1.3 + float2(-time * _CausticsSpeed * 0.8, time * _CausticsSpeed * 0.5);
                
                float c1 = voronoiNoise(uv1);
                float c2 = voronoiNoise(uv2);
                
                float caustic = saturate(1.0 - (c1 * c2 * 4.0));
                return pow(caustic, 2.0);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.tangentWS = float4(normalInputs.tangentWS, input.tangentOS.w);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float time = _Time.y;
                
                // Sample base texture
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                
                // Normal mapping
                float3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv), _BumpScale);
                float3 bitangent = cross(input.normalWS, input.tangentWS.xyz) * input.tangentWS.w;
                float3x3 TBN = float3x3(input.tangentWS.xyz, bitangent, input.normalWS);
                float3 normalWS = normalize(mul(normalTS, TBN));
                
                // View direction
                float3 viewDir = normalize(GetWorldSpaceViewDir(input.positionWS));
                
                // === WATER FLOW CALCULATION ===
                // UV.y goes from 0 (bottom) to 1 (top)
                // For top-to-bottom flow with fill progress:
                // - Fill progress 0 = no water
                // - Fill progress 0.5 = water fills top half (UV.y > 0.5)
                // - Fill progress 1 = water fills entire pipe
                
                // Invert UV.y for top-to-bottom fill (water comes from top)
                float fillCoord = 1.0 - input.uv.y; // Now 0 = top, 1 = bottom
                
                // Add some noise to the fill edge for natural look
                float edgeNoise = gradientNoise(input.uv * _FlowNoiseScale + time * _FlowNoiseSpeed * 0.3) * 0.05;
                
                // Calculate water mask based on fill progress
                float fillEdge = _FillProgress + edgeNoise;
                float waterMask = smoothstep(fillEdge - _FlowEdgeSoftness, fillEdge + _FlowEdgeSoftness, fillCoord);
                waterMask = 1.0 - waterMask; // Invert so water appears where fill has reached
                waterMask *= _FlowIntensity;
                
                // === ANIMATED WATER SURFACE ===
                // Flow UV animation (continuous flow within the filled area)
                float2 flowUV = input.uv;
                flowUV.y += time * _FlowSpeed * _FlowDirection;
                
                // Wave distortion
                float wave1 = sin(flowUV.x * _WaterWaveScale + time * _WaterWaveSpeed) * _WaterWaveAmplitude;
                float wave2 = cos(flowUV.y * _WaterWaveScale * 0.8 + time * _WaterWaveSpeed * 1.2) * _WaterWaveAmplitude * 0.7;
                float2 waveOffset = float2(wave1, wave2);
                
                // Flow noise for surface detail
                float flowNoise1 = gradientNoise((flowUV + waveOffset) * _FlowNoiseScale);
                float flowNoise2 = gradientNoise((flowUV + waveOffset) * _FlowNoiseScale * 0.5 + 50);
                float flowPattern = saturate((flowNoise1 + flowNoise2) * 0.5 + 0.5);
                
                // === WATER COLOR ===
                // Blend between shallow and deep water color based on flow pattern
                float4 waterColor = lerp(_FlowColor, _FlowColorDeep, flowPattern * 0.5);
                
                // Add caustics
                float causticsValue = caustics(flowUV + waveOffset * 2, time);
                waterColor.rgb += causticsValue * _CausticsIntensity * _FlowColor.rgb;
                
                // Fresnel for water
                float NdotV = saturate(dot(normalWS, viewDir));
                float fresnel = pow(1.0 - NdotV, _FresnelPower);
                waterColor.rgb = lerp(waterColor.rgb, _FresnelColor.rgb, fresnel * 0.3 * waterMask);
                
                // === BLEND PIPE AND WATER ===
                float3 finalColor = lerp(baseColor.rgb, waterColor.rgb, waterMask);
                
                // Add flow emission only where water is
                float3 emission = _FlowEmissionColor.rgb * _FlowEmission * waterMask * (0.7 + flowPattern * 0.3);
                
                // === LIGHTING ===
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                
                // Diffuse
                float3 diffuse = finalColor * mainLight.color * (NdotL * 0.5 + 0.5);
                
                // Specular (stronger on water)
                float3 halfDir = normalize(mainLight.direction + viewDir);
                float NdotH = saturate(dot(normalWS, halfDir));
                float specPower = lerp(_Smoothness, _Smoothness * 1.5, waterMask);
                float spec = pow(NdotH, specPower * 128.0) * lerp(_Metallic, _Metallic + 0.3, waterMask);
                
                // Combine
                float3 result = diffuse + spec * mainLight.color + emission;
                
                // Apply fog
                result = MixFog(result, input.fogFactor);
                
                return half4(result, 1.0);
            }
            ENDHLSL
        }

        // Shadow caster pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            float3 _LightDirection;

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif
                
                output.positionCS = positionCS;
                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
