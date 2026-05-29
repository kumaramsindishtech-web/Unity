// Industrial Reactor Simulator - Tank Cutaway Shader (URP)
// Copyright (c) 2024 Industrial Reactor Simulator. MIT License.
// Single shader that handles both exterior (metal look) and cutaway clipping

Shader "Industrial Reactor/Tank Cutaway"
{
    Properties
    {
        [Header(Metal Appearance)]
        _BaseColor ("Color", Color) = (0.8, 0.8, 0.85, 1)
        _BaseMap ("Albedo", 2D) = "white" {}
        _Metallic ("Metallic", Range(0, 1)) = 0.7
        _Smoothness ("Smoothness", Range(0, 1)) = 0.8
        
        [Header(Cutaway Settings)]
        _ClipPlane ("Clip Plane (xyz=normal, w=distance)", Vector) = (1, 0, 0, 0)
        _ClipDir ("Clip Direction", Vector) = (1, 0, 0, 0)
        [Toggle] _EnableClip ("Enable Clipping", Float) = 0
        
        [Header(Interior)]
        _InteriorColor ("Interior Color", Color) = (0.4, 0.4, 0.45, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        LOD 200

        // Main pass - exterior surface with clipping
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
                float4 _ClipPlane;
                float4 _ClipDir;
                float _EnableClip;
                float4 _InteriorColor;
            CBUFFER_END

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
                // Clip if enabled
                if (_EnableClip > 0.5)
                {
                    float clipDist = dot(input.positionWS, _ClipPlane.xyz) + _ClipPlane.w;
                    clip(clipDist);
                }

                // Metal appearance
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                float3 normalWS = normalize(input.normalWS);
                float3 viewDir = normalize(GetWorldSpaceViewDir(input.positionWS));
                
                // Lighting
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                float3 diffuse = baseColor.rgb * mainLight.color * (NdotL * 0.5 + 0.5);
                
                // Specular for metallic look
                float3 halfDir = normalize(mainLight.direction + viewDir);
                float NdotH = saturate(dot(normalWS, halfDir));
                float spec = pow(NdotH, _Smoothness * 128.0) * _Metallic;
                
                // Fresnel rim
                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDir)), 4.0);
                
                float3 finalColor = diffuse + spec * mainLight.color + fresnel * 0.1;
                return half4(MixFog(finalColor, input.fogFactor), 1.0);
            }
            ENDHLSL
        }

        // Interior pass - back faces visible when cut
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
                float4 _ClipPlane;
                float4 _ClipDir;
                float _EnableClip;
                float4 _InteriorColor;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = -TransformObjectToWorldNormal(input.normalOS); // Flip for interior
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Clip if enabled
                if (_EnableClip > 0.5)
                {
                    float clipDist = dot(input.positionWS, _ClipPlane.xyz) + _ClipPlane.w;
                    clip(clipDist);
                }

                float3 normalWS = normalize(input.normalWS);
                
                // Simple interior lighting
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                float3 diffuse = _InteriorColor.rgb * mainLight.color * (NdotL * 0.3 + 0.7);
                
                return half4(diffuse, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
