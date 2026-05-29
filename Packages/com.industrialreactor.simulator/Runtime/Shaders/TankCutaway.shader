// Industrial Reactor Simulator - Tank Cutaway Shader (URP)
// Metal tank with clipping plane support

Shader "Industrial Reactor/Tank Cutaway"
{
    Properties
    {
        _BaseColor ("Color", Color) = (0.7, 0.7, 0.75, 1)
        _Metallic ("Metallic", Range(0, 1)) = 0.8
        _Smoothness ("Smoothness", Range(0, 1)) = 0.7
        
        [Header(Cutaway)]
        _ClipPlane ("Clip Plane", Vector) = (1, 0, 0, 0)
        _ClipDir ("Clip Direction", Vector) = (1, 0, 0, 0)
        [Toggle] _EnableClip ("Enable Clipping", Float) = 0
        
        [Header(Interior)]
        _InteriorColor ("Interior Color", Color) = (0.4, 0.4, 0.45, 1)
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline" 
        }

        // Front faces
        Pass
        {
            Name "Exterior"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back

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
                half4 _BaseColor;
                half _Metallic;
                half _Smoothness;
                float4 _ClipPlane;
                float4 _ClipDir;
                half _EnableClip;
                half4 _InteriorColor;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Clipping
                if (_EnableClip > 0.5)
                {
                    float dist = dot(IN.positionWS, _ClipPlane.xyz) + _ClipPlane.w;
                    clip(dist);
                }
                
                float3 normalWS = normalize(IN.normalWS);
                float3 viewDir = normalize(GetWorldSpaceViewDir(IN.positionWS));
                
                // Lighting
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                half3 diffuse = _BaseColor.rgb * mainLight.color * (NdotL * 0.5 + 0.5);
                
                // Specular
                float3 halfVec = normalize(mainLight.direction + viewDir);
                float spec = pow(saturate(dot(normalWS, halfVec)), _Smoothness * 100.0);
                diffuse += spec * mainLight.color * _Metallic * 0.5;
                
                // Fresnel rim
                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDir)), 4.0);
                diffuse += fresnel * 0.08;
                
                return half4(diffuse, 1.0);
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
                half4 _BaseColor;
                half _Metallic;
                half _Smoothness;
                float4 _ClipPlane;
                float4 _ClipDir;
                half _EnableClip;
                half4 _InteriorColor;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS = -TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Clipping
                if (_EnableClip > 0.5)
                {
                    float dist = dot(IN.positionWS, _ClipPlane.xyz) + _ClipPlane.w;
                    clip(dist);
                }
                
                float3 normalWS = normalize(IN.normalWS);
                
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                half3 color = _InteriorColor.rgb * mainLight.color * (NdotL * 0.3 + 0.7);
                
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
    
    FallBack "Universal Render Pipeline/Unlit"
}
