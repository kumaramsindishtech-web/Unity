// ============================================================================
// Industrial Reactor Simulator - Water Shader (URP)
// Version: 1.0.0
// Description: Animated water with swirl, emission, and turbulence effects
// ============================================================================

Shader "Industrial Reactor/Water"
{
    Properties
    {
        [Header(Base Color)]
        _BaseColor ("Water Color", Color) = (0.2, 0.5, 0.8, 0.8)
        _BaseMap ("Base Texture", 2D) = "white" {}
        
        [Header(Water Properties)]
        _WaterLevel ("Water Level", Range(0, 1)) = 1.0
        _Transparency ("Transparency", Range(0, 1)) = 0.7
        _FresnelPower ("Fresnel Power", Range(0.1, 5)) = 2.0
        
        [Header(Swirl Effect)]
        _SwirlSpeed ("Swirl Speed", Range(0, 360)) = 0
        _SwirlStrength ("Swirl Strength", Range(0, 1)) = 0.5
        _SwirlCenter ("Swirl Center", Vector) = (0.5, 0.5, 0, 0)
        
        [Header(Turbulence)]
        _Turbulence ("Turbulence", Range(0, 1)) = 0
        _TurbulenceScale ("Turbulence Scale", Range(0.1, 10)) = 2
        _TurbulenceSpeed ("Turbulence Speed", Range(0, 5)) = 1
        
        [Header(Emission)]
        [HDR] _EmissionColor ("Emission Color", Color) = (0.2, 0.5, 1, 1)
        _EmissionIntensity ("Emission Intensity", Range(0, 5)) = 0
        _EmissionPulseSpeed ("Pulse Speed", Range(0, 5)) = 1
        
        [Header(Surface)]
        _Smoothness ("Smoothness", Range(0, 1)) = 0.9
        _Metallic ("Metallic", Range(0, 1)) = 0.0
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Range(0, 2)) = 1
    }


    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        
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
                float3 viewDirWS : TEXCOORD3;
                float fogFactor : TEXCOORD4;
            };
            
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _WaterLevel;
                float _Transparency;
                float _FresnelPower;
                float _SwirlSpeed;
                float _SwirlStrength;
                float4 _SwirlCenter;
                float _Turbulence;
                float _TurbulenceScale;
                float _TurbulenceSpeed;
                float4 _EmissionColor;
                float _EmissionIntensity;
                float _EmissionPulseSpeed;
                float _Smoothness;
                float _Metallic;
                float _BumpScale;
            CBUFFER_END


            
            // Simple noise function
            float noise(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }
            
            // Swirl UV transformation
            float2 SwirlUV(float2 uv, float2 center, float angle, float strength)
            {
                float2 delta = uv - center;
                float dist = length(delta);
                float falloff = 1.0 - saturate(dist * 2.0);
                float rotation = angle * strength * falloff;
                
                float s = sin(rotation);
                float c = cos(rotation);
                
                float2x2 rotMatrix = float2x2(c, -s, s, c);
                delta = mul(rotMatrix, delta);
                
                return center + delta;
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                output.viewDirWS = GetWorldSpaceViewDir(posInputs.positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // Apply swirl effect to UVs
                float swirlAngle = _SwirlSpeed * _Time.y * 0.0174533; // Convert to radians
                float2 swirlUV = SwirlUV(input.uv, _SwirlCenter.xy, swirlAngle, _SwirlStrength);
                
                // Add turbulence
                float2 turbulenceOffset = float2(
                    noise(swirlUV * _TurbulenceScale + _Time.y * _TurbulenceSpeed),
                    noise(swirlUV * _TurbulenceScale + _Time.y * _TurbulenceSpeed + 100)
                ) * _Turbulence * 0.1;
                
                float2 finalUV = swirlUV + turbulenceOffset;
                
                // Sample textures
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, finalUV) * _BaseColor;
                
                // Fresnel effect
                float3 viewDir = normalize(input.viewDirWS);
                float3 normal = normalize(input.normalWS);
                float fresnel = pow(1.0 - saturate(dot(viewDir, normal)), _FresnelPower);
                
                // Emission with pulse
                float pulse = sin(_Time.y * _EmissionPulseSpeed) * 0.5 + 0.5;
                float3 emission = _EmissionColor.rgb * _EmissionIntensity * (0.5 + pulse * 0.5);
                
                // Basic lighting
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(normal, mainLight.direction));
                float3 diffuse = baseColor.rgb * mainLight.color * (NdotL * 0.5 + 0.5);
                
                // Combine
                float3 finalColor = diffuse + emission;
                finalColor = lerp(finalColor, finalColor + fresnel * 0.3, fresnel);
                
                // Apply fog
                finalColor = MixFog(finalColor, input.fogFactor);
                
                // Alpha with transparency
                float alpha = baseColor.a * _Transparency;
                alpha = lerp(alpha, 1.0, fresnel * 0.5);
                
                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }
    
    FallBack "Universal Render Pipeline/Lit"
}
