// Industrial Reactor Simulator - Pipe Flow Shader (URP)
// Copyright (c) 2024 Industrial Reactor Simulator. MIT License.

Shader "Industrial Reactor/Pipe Flow"
{
    Properties
    {
        [Header(Base Pipe Properties)]
        _BaseColor ("Pipe Color", Color) = (0.5, 0.5, 0.55, 1)
        _BaseMap ("Base Texture", 2D) = "white" {}
        _Metallic ("Metallic", Range(0, 1)) = 0.7
        _Smoothness ("Smoothness", Range(0, 1)) = 0.6
        
        [Header(Water Flow Settings)]
        _FlowColor ("Water Color", Color) = (0.3, 0.65, 0.9, 0.85)
        _FlowColorDeep ("Water Deep Color", Color) = (0.15, 0.4, 0.6, 0.95)
        
        [Header(Flow Animation)]
        _FillProgress ("Fill Progress", Range(0, 1)) = 0
        _FlowSpeed ("Flow Speed", Range(0, 10)) = 2
        _FlowDirection ("Flow Direction", Range(-1, 1)) = 1
        _FlowIntensity ("Flow Intensity", Range(0, 1)) = 1
        
        [Header(Flow Appearance)]
        _FlowEdgeSoftness ("Edge Softness", Range(0.01, 0.3)) = 0.05
        
        [Header(Emission)]
        [HDR] _FlowEmissionColor ("Flow Emission", Color) = (0.2, 0.5, 1, 1)
        _FlowEmission ("Emission Intensity", Range(0, 3)) = 0.3
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
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float fogFactor : TEXCOORD3;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _Metallic;
                float _Smoothness;
                float4 _FlowColor;
                float4 _FlowColorDeep;
                float _FillProgress;
                float _FlowSpeed;
                float _FlowDirection;
                float _FlowIntensity;
                float _FlowEdgeSoftness;
                float4 _FlowEmissionColor;
                float _FlowEmission;
            CBUFFER_END

            // Simple noise function
            float simpleNoise(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }

            float smoothNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);
                
                float a = simpleNoise(i);
                float b = simpleNoise(i + float2(1.0, 0.0));
                float c = simpleNoise(i + float2(0.0, 1.0));
                float d = simpleNoise(i + float2(1.0, 1.0));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float time = _Time.y;
                
                // Sample base texture
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                
                // View direction
                float3 viewDir = normalize(GetWorldSpaceViewDir(input.positionWS));
                float3 normalWS = normalize(input.normalWS);
                
                // Water fill calculation
                // UV.y: 0 = one end, 1 = other end
                // Fill progress determines how much of pipe is filled
                float fillCoord = 1.0 - input.uv.y; // Invert for top-to-bottom fill
                
                // Add slight noise to edge
                float edgeNoise = smoothNoise(input.uv * 10.0 + time * 0.3) * 0.03;
                float fillEdge = _FillProgress + edgeNoise;
                
                // Water mask - smooth transition at fill edge
                float waterMask = 1.0 - smoothstep(fillEdge - _FlowEdgeSoftness, fillEdge + _FlowEdgeSoftness, fillCoord);
                waterMask *= _FlowIntensity;
                
                // Animated flow pattern
                float2 flowUV = input.uv;
                flowUV.y += time * _FlowSpeed * _FlowDirection;
                
                // Flow pattern noise
                float flowPattern = smoothNoise(flowUV * 8.0);
                flowPattern = smoothNoise(flowUV * 4.0 + flowPattern * 0.5);
                
                // Water color with variation
                float4 waterColor = lerp(_FlowColor, _FlowColorDeep, flowPattern * 0.4);
                
                // Add some caustic-like highlights
                float caustic = smoothNoise(flowUV * 12.0 + time * 0.5);
                caustic = pow(caustic, 3.0) * 0.3;
                waterColor.rgb += caustic * _FlowColor.rgb;
                
                // Fresnel effect on water
                float NdotV = saturate(dot(normalWS, viewDir));
                float fresnel = pow(1.0 - NdotV, 3.0);
                waterColor.rgb += fresnel * 0.2 * waterMask;
                
                // Blend pipe and water
                float3 finalColor = lerp(baseColor.rgb, waterColor.rgb, waterMask);
                
                // Emission for water glow
                float3 emission = _FlowEmissionColor.rgb * _FlowEmission * waterMask * (0.7 + flowPattern * 0.3);
                
                // Lighting
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                float3 diffuse = finalColor * mainLight.color * (NdotL * 0.5 + 0.5);
                
                // Specular
                float3 halfDir = normalize(mainLight.direction + viewDir);
                float NdotH = saturate(dot(normalWS, halfDir));
                float spec = pow(NdotH, _Smoothness * 128.0) * _Metallic;
                spec = lerp(spec, spec * 1.5, waterMask); // Shinier on water
                
                float3 result = diffuse + spec * mainLight.color + emission;
                result = MixFog(result, input.fogFactor);
                
                return half4(result, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
