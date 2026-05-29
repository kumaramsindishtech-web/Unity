// Industrial Reactor Simulator - Glass Pipe with Water Flow (URP)
// Copyright (c) 2024 Industrial Reactor Simulator. MIT License.
// Transparent glass pipe with internal water flow visualization

Shader "Industrial Reactor/Glass Pipe"
{
    Properties
    {
        [Header(Glass Properties)]
        _GlassColor ("Glass Tint", Color) = (0.9, 0.95, 1.0, 0.15)
        _GlassThickness ("Glass Thickness", Range(0, 0.5)) = 0.1
        _GlassRefraction ("Refraction", Range(0, 0.3)) = 0.05
        _GlassSmoothness ("Glass Smoothness", Range(0, 1)) = 0.95
        _GlassFresnelPower ("Fresnel Power", Range(1, 10)) = 4.0
        
        [Header(Water Properties)]
        _WaterColor ("Water Color", Color) = (0.2, 0.6, 0.9, 0.8)
        _WaterColorDeep ("Water Deep Color", Color) = (0.1, 0.35, 0.6, 0.9)
        
        [Header(Water Fill - UV Y Based)]
        _FillProgress ("Fill Progress", Range(0, 1)) = 0
        _FillDirection ("Fill Direction (1=down, -1=up)", Range(-1, 1)) = 1
        _FillEdgeSoftness ("Fill Edge Softness", Range(0.01, 0.2)) = 0.03
        
        [Header(Water Flow Animation)]
        _FlowSpeed ("Flow Speed", Range(0, 10)) = 2.0
        _FlowIntensity ("Flow Intensity", Range(0, 1)) = 1.0
        
        [Header(Water Surface Detail)]
        _WaveScale ("Wave Scale", Range(1, 30)) = 12.0
        _WaveSpeed ("Wave Speed", Range(0, 3)) = 1.0
        _CausticIntensity ("Caustic Intensity", Range(0, 0.5)) = 0.2
        
        [Header(Emission)]
        [HDR] _WaterEmission ("Water Emission", Color) = (0.1, 0.3, 0.6, 1)
        _EmissionIntensity ("Emission Intensity", Range(0, 2)) = 0.2
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        LOD 200

        // Main pass - glass with water inside
        Pass
        {
            Name "GlassPipe"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

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
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 viewDirWS : TEXCOORD3;
                float fogFactor : TEXCOORD4;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _GlassColor;
                float _GlassThickness;
                float _GlassRefraction;
                float _GlassSmoothness;
                float _GlassFresnelPower;
                float4 _WaterColor;
                float4 _WaterColorDeep;
                float _FillProgress;
                float _FillDirection;
                float _FillEdgeSoftness;
                float _FlowSpeed;
                float _FlowIntensity;
                float _WaveScale;
                float _WaveSpeed;
                float _CausticIntensity;
                float4 _WaterEmission;
                float _EmissionIntensity;
            CBUFFER_END

            // Noise functions
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float smoothNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash(i);
                float b = hash(i + float2(1, 0));
                float c = hash(i + float2(0, 1));
                float d = hash(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float causticPattern(float2 uv, float time)
            {
                float c1 = smoothNoise(uv * _WaveScale + time * _WaveSpeed);
                float c2 = smoothNoise(uv * _WaveScale * 1.5 - time * _WaveSpeed * 0.7);
                float c3 = smoothNoise(uv * _WaveScale * 0.7 + time * _WaveSpeed * 0.5);
                return (c1 + c2 + c3) / 3.0;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceViewDir(posInputs.positionWS);
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float time = _Time.y;
                float3 normalWS = normalize(input.normalWS);
                float3 viewDir = normalize(input.viewDirWS);
                
                // Fresnel for glass reflection
                float NdotV = saturate(dot(normalWS, viewDir));
                float fresnel = pow(1.0 - NdotV, _GlassFresnelPower);
                
                // === WATER FILL CALCULATION ===
                // UV.y based fill (0 = one end, 1 = other end)
                float fillCoord = _FillDirection > 0 ? (1.0 - input.uv.y) : input.uv.y;
                
                // Add noise to fill edge
                float edgeNoise = smoothNoise(input.uv * 15.0 + time * 0.5) * 0.02;
                float fillEdge = _FillProgress + edgeNoise;
                
                // Water mask with soft edge
                float waterMask = smoothstep(fillEdge - _FillEdgeSoftness, fillEdge + _FillEdgeSoftness, fillCoord);
                waterMask = (1.0 - waterMask) * _FlowIntensity;
                
                // === ANIMATED WATER ===
                float2 flowUV = input.uv;
                flowUV.y += time * _FlowSpeed * _FillDirection;
                
                // Caustics/waves pattern
                float caustic = causticPattern(flowUV, time);
                caustic = pow(caustic, 2.0); // Sharpen
                
                // Water color with variation
                float4 waterColor = lerp(_WaterColor, _WaterColorDeep, caustic * 0.4);
                waterColor.rgb += caustic * _CausticIntensity * _WaterColor.rgb;
                
                // Water emission glow
                float3 waterEmission = _WaterEmission.rgb * _EmissionIntensity * waterMask * (0.6 + caustic * 0.4);
                
                // === GLASS ===
                // Glass is mostly transparent with slight tint and reflections
                float4 glassColor = _GlassColor;
                glassColor.rgb += fresnel * 0.3; // Reflection at edges
                
                // === COMBINE GLASS + WATER ===
                // Where there's water, show water through glass
                // Where there's no water, show empty glass
                float3 finalColor;
                float finalAlpha;
                
                if (waterMask > 0.01)
                {
                    // Water visible through glass
                    finalColor = lerp(glassColor.rgb, waterColor.rgb, waterMask * 0.85);
                    finalColor += waterEmission;
                    finalAlpha = lerp(glassColor.a, waterColor.a, waterMask);
                    finalAlpha = max(finalAlpha, fresnel * 0.5); // Glass reflection
                }
                else
                {
                    // Empty glass
                    finalColor = glassColor.rgb;
                    finalAlpha = glassColor.a + fresnel * 0.3;
                }
                
                // === LIGHTING ===
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                finalColor *= mainLight.color * (NdotL * 0.3 + 0.7);
                
                // Specular highlight (sharp for glass)
                float3 halfDir = normalize(mainLight.direction + viewDir);
                float NdotH = saturate(dot(normalWS, halfDir));
                float spec = pow(NdotH, _GlassSmoothness * 256.0);
                finalColor += spec * mainLight.color * 0.5;
                
                // Secondary specular for water
                if (waterMask > 0.01)
                {
                    float waterSpec = pow(NdotH, 64.0) * waterMask * 0.3;
                    finalColor += waterSpec * mainLight.color * _WaterColor.rgb;
                }
                
                finalColor = MixFog(finalColor, input.fogFactor);
                
                return half4(finalColor, saturate(finalAlpha));
            }
            ENDHLSL
        }

        // Back faces for inside view
        Pass
        {
            Name "GlassPipeBack"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Front

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _GlassColor;
                float _GlassThickness;
                float _GlassRefraction;
                float _GlassSmoothness;
                float _GlassFresnelPower;
                float4 _WaterColor;
                float4 _WaterColorDeep;
                float _FillProgress;
                float _FillDirection;
                float _FillEdgeSoftness;
                float _FlowSpeed;
                float _FlowIntensity;
                float _WaveScale;
                float _WaveSpeed;
                float _CausticIntensity;
                float4 _WaterEmission;
                float _EmissionIntensity;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInputs.positionCS;
                output.normalWS = -TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceViewDir(posInputs.positionWS);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDir = normalize(input.viewDirWS);
                float NdotV = saturate(dot(normalWS, viewDir));
                float fresnel = pow(1.0 - NdotV, _GlassFresnelPower * 0.5);
                
                // Inner glass surface - very subtle
                float4 color = _GlassColor * 0.5;
                color.a = _GlassColor.a * 0.3 + fresnel * 0.1;
                
                return color;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
