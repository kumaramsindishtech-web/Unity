// Industrial Reactor Simulator - Tank Cutaway Shader (URP)
// Copyright (c) 2024 Industrial Reactor Simulator. MIT License.

Shader "Industrial Reactor/Tank Cutaway"
{
    Properties
    {
        [Header(Base Properties)]
        _BaseColor ("Color", Color) = (0.8, 0.8, 0.85, 1)
        _BaseMap ("Albedo", 2D) = "white" {}
        _Metallic ("Metallic", Range(0, 1)) = 0.5
        _Smoothness ("Smoothness", Range(0, 1)) = 0.7
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Range(0, 2)) = 1
        
        [Header(Cutaway Settings)]
        _ClipPlane ("Clip Plane (xyz=normal, w=distance)", Vector) = (1, 0, 0, 0)
        _ClipDir ("Clip Direction", Vector) = (1, 0, 0, 0)
        [Toggle] _EnableClip ("Enable Clipping", Float) = 1
        
        [Header(Interior Color)]
        _InteriorColor ("Interior Color", Color) = (0.3, 0.3, 0.35, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        LOD 300

        // Front faces
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 positionWS : TEXCOORD1; float3 normalWS : TEXCOORD2; float fogFactor : TEXCOORD3; };

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST; float4 _BaseColor; float _Metallic; float _Smoothness; float _BumpScale;
                float4 _ClipPlane; float4 _ClipDir; float _EnableClip; float4 _InteriorColor;
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
                if (_EnableClip > 0.5) { float dist = dot(input.positionWS, _ClipPlane.xyz) + _ClipPlane.w; clip(dist); }
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                float3 normal = normalize(input.normalWS);
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(normal, mainLight.direction));
                float3 diffuse = baseColor.rgb * mainLight.color * (NdotL * 0.5 + 0.5);
                float3 viewDir = normalize(GetWorldSpaceViewDir(input.positionWS));
                float3 halfDir = normalize(mainLight.direction + viewDir);
                float spec = pow(saturate(dot(normal, halfDir)), _Smoothness * 128.0) * _Metallic;
                float3 finalColor = diffuse + spec * mainLight.color;
                return half4(MixFog(finalColor, input.fogFactor), 1.0);
            }
            ENDHLSL
        }

        // Back faces (interior)
        Pass
        {
            Name "Interior"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST; float4 _BaseColor; float _Metallic; float _Smoothness; float _BumpScale;
                float4 _ClipPlane; float4 _ClipDir; float _EnableClip; float4 _InteriorColor;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = -GetVertexNormalInputs(input.normalOS).normalWS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                if (_EnableClip > 0.5) { float dist = dot(input.positionWS, _ClipPlane.xyz) + _ClipPlane.w; clip(dist); }
                float3 normal = normalize(input.normalWS);
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(normal, mainLight.direction));
                float3 diffuse = _InteriorColor.rgb * mainLight.color * (NdotL * 0.3 + 0.7);
                return half4(diffuse, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
