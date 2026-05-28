// Industrial Reactor Simulator - Pipe Flow Shader (URP)
// Copyright (c) 2024 Industrial Reactor Simulator. MIT License.

Shader "Industrial Reactor/Pipe Flow"
{
    Properties
    {
        [Header(Base Properties)]
        _BaseColor ("Pipe Color", Color) = (0.5, 0.5, 0.55, 1)
        _BaseMap ("Base Texture", 2D) = "white" {}
        _Metallic ("Metallic", Range(0, 1)) = 0.7
        _Smoothness ("Smoothness", Range(0, 1)) = 0.6
        
        [Header(Flow Effect)]
        _FlowMap ("Flow Texture", 2D) = "white" {}
        _FlowColor ("Flow Color", Color) = (0.3, 0.6, 1.0, 0.8)
        _FlowSpeed ("Flow Speed", Range(0, 10)) = 2
        _FlowDirection ("Flow Direction", Vector) = (0, 1, 0, 0)
        _FlowIntensity ("Flow Intensity", Range(0, 1)) = 1
        
        [Header(Flow Appearance)]
        _FlowWidth ("Flow Width", Range(0.1, 1)) = 0.5
        _FlowSharpness ("Flow Sharpness", Range(1, 10)) = 3
        _FlowEmission ("Flow Emission", Range(0, 3)) = 1
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

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 positionWS : TEXCOORD1; float3 normalWS : TEXCOORD2; float fogFactor : TEXCOORD3; };

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_FlowMap); SAMPLER(sampler_FlowMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST; float4 _BaseColor; float _Metallic; float _Smoothness;
                float4 _FlowMap_ST; float4 _FlowColor; float _FlowSpeed; float4 _FlowDirection;
                float _FlowIntensity; float _FlowWidth; float _FlowSharpness; float _FlowEmission;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = GetVertexNormalInputs(input.normalOS).normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                float2 flowUV = input.uv + _FlowDirection.xy * _Time.y * _FlowSpeed;
                half4 flowSample = SAMPLE_TEXTURE2D(_FlowMap, sampler_FlowMap, flowUV);
                float flowPattern = saturate(pow(flowSample.r, _FlowSharpness) * _FlowIntensity);
                float3 finalColor = lerp(baseColor.rgb, _FlowColor.rgb, flowPattern * _FlowWidth);
                float3 emission = _FlowColor.rgb * flowPattern * _FlowEmission * _FlowIntensity;

                float3 normal = normalize(input.normalWS);
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(normal, mainLight.direction));
                float3 diffuse = finalColor * mainLight.color * (NdotL * 0.5 + 0.5);
                float3 viewDir = normalize(GetWorldSpaceViewDir(input.positionWS));
                float3 halfDir = normalize(mainLight.direction + viewDir);
                float spec = pow(saturate(dot(normal, halfDir)), _Smoothness * 128.0) * _Metallic;

                float3 result = diffuse + spec * mainLight.color + emission;
                return half4(MixFog(result, input.fogFactor), 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
