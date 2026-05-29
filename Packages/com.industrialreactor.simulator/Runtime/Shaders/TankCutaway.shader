// Industrial Reactor Simulator - Tank Cutaway Shader (URP)
// Copyright (c) 2024 Industrial Reactor Simulator. MIT License.
// Supports dual material setup: Slot 0 = Metal (visible on remaining half), Slot 1 = Cutaway

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
        _InteriorMetallic ("Interior Metallic", Range(0, 1)) = 0.3
        _InteriorSmoothness ("Interior Smoothness", Range(0, 1)) = 0.4
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        LOD 300

        // Front faces - exterior surface
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

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
                float4 _ClipPlane;
                float4 _ClipDir;
                float _EnableClip;
                float4 _InteriorColor;
                float _InteriorMetallic;
                float _InteriorSmoothness;
            CBUFFER_END

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
                // Clip based on plane
                if (_EnableClip > 0.5)
                {
                    float dist = dot(input.positionWS, _ClipPlane.xyz) + _ClipPlane.w;
                    clip(dist);
                }

                // Sample textures
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                float3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv), _BumpScale);
                
                // Normal mapping
                float3 bitangent = cross(input.normalWS, input.tangentWS.xyz) * input.tangentWS.w;
                float3x3 TBN = float3x3(input.tangentWS.xyz, bitangent, input.normalWS);
                float3 normalWS = normalize(mul(normalTS, TBN));

                // Lighting
                Light mainLight = GetMainLight();
                float3 viewDir = normalize(GetWorldSpaceViewDir(input.positionWS));
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                
                // Diffuse
                float3 diffuse = baseColor.rgb * mainLight.color * (NdotL * 0.5 + 0.5);
                
                // Specular
                float3 halfDir = normalize(mainLight.direction + viewDir);
                float NdotH = saturate(dot(normalWS, halfDir));
                float spec = pow(NdotH, _Smoothness * 128.0) * _Metallic;
                
                float3 finalColor = diffuse + spec * mainLight.color;
                return half4(MixFog(finalColor, input.fogFactor), 1.0);
            }
            ENDHLSL
        }

        // Back faces (interior) - shows cut cross-section
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

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _Metallic;
                float _Smoothness;
                float _BumpScale;
                float4 _ClipPlane;
                float4 _ClipDir;
                float _EnableClip;
                float4 _InteriorColor;
                float _InteriorMetallic;
                float _InteriorSmoothness;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                // Flip normal for interior
                output.normalWS = -GetVertexNormalInputs(input.normalOS).normalWS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Clip based on plane
                if (_EnableClip > 0.5)
                {
                    float dist = dot(input.positionWS, _ClipPlane.xyz) + _ClipPlane.w;
                    clip(dist);
                }

                float3 normalWS = normalize(input.normalWS);
                float3 viewDir = normalize(GetWorldSpaceViewDir(input.positionWS));
                
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                
                // Softer lighting for interior
                float3 diffuse = _InteriorColor.rgb * mainLight.color * (NdotL * 0.3 + 0.7);
                
                // Subtle specular
                float3 halfDir = normalize(mainLight.direction + viewDir);
                float NdotH = saturate(dot(normalWS, halfDir));
                float spec = pow(NdotH, _InteriorSmoothness * 64.0) * _InteriorMetallic * 0.5;
                
                return half4(diffuse + spec * mainLight.color, 1.0);
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

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _Metallic;
                float _Smoothness;
                float _BumpScale;
                float4 _ClipPlane;
                float4 _ClipDir;
                float _EnableClip;
                float4 _InteriorColor;
                float _InteriorMetallic;
                float _InteriorSmoothness;
            CBUFFER_END

            float3 _LightDirection;

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(output.positionWS, normalWS, _LightDirection));
                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target
            {
                if (_EnableClip > 0.5)
                {
                    float dist = dot(input.positionWS, _ClipPlane.xyz) + _ClipPlane.w;
                    clip(dist);
                }
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
