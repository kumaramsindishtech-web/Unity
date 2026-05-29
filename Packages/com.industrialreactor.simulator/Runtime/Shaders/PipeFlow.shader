// Industrial Reactor Simulator - Pipe Flow Shader (URP)
// Shows water flow through opaque pipe

Shader "Industrial Reactor/Pipe Flow"
{
    Properties
    {
        _BaseColor ("Pipe Color", Color) = (0.5, 0.5, 0.55, 1)
        _Metallic ("Metallic", Range(0, 1)) = 0.7
        _Smoothness ("Smoothness", Range(0, 1)) = 0.6
        
        [Header(Water Flow)]
        _WaterColor ("Water Color", Color) = (0.2, 0.5, 0.8, 1)
        _FillProgress ("Fill Progress", Range(0, 1)) = 0
        _FlowSpeed ("Flow Speed", Range(0, 5)) = 2.0
        _FlowIntensity ("Flow Intensity", Range(0, 1)) = 1.0
        _FillDirection ("Fill Direction", Float) = 1
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline" 
        }

        Pass
        {
            Name "PipeFlow"
            Tags { "LightMode" = "UniversalForward" }

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
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Metallic;
                half _Smoothness;
                half4 _WaterColor;
                half _FillProgress;
                half _FlowSpeed;
                half _FlowIntensity;
                half _FillDirection;
            CBUFFER_END

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }
            
            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(hash(i), hash(i + float2(1, 0)), f.x),
                    lerp(hash(i + float2(0, 1)), hash(i + float2(1, 1)), f.x),
                    f.y);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 normalWS = normalize(IN.normalWS);
                float3 viewDir = normalize(GetWorldSpaceViewDir(IN.positionWS));
                float time = _Time.y;
                
                // Water fill mask
                float fillCoord = _FillDirection > 0 ? IN.uv.y : (1.0 - IN.uv.y);
                float waterMask = smoothstep(_FillProgress - 0.02, _FillProgress + 0.02, fillCoord);
                waterMask = (1.0 - waterMask) * _FlowIntensity;
                
                // Animated flow
                float2 flowUV = IN.uv;
                flowUV.y += time * _FlowSpeed * 0.1 * _FillDirection;
                
                float flowNoise = noise(flowUV * 8.0 + time * 0.3);
                flowNoise += noise(flowUV * 16.0 - time * 0.2) * 0.5;
                
                // Blend pipe and water color
                half4 pipeColor = _BaseColor;
                half4 waterColor = _WaterColor;
                waterColor.rgb += flowNoise * 0.1 * _WaterColor.rgb;
                
                half3 finalColor = lerp(pipeColor.rgb, waterColor.rgb, waterMask);
                
                // Lighting
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                finalColor *= mainLight.color * (NdotL * 0.5 + 0.5);
                
                // Specular
                float3 halfVec = normalize(mainLight.direction + viewDir);
                float spec = pow(saturate(dot(normalWS, halfVec)), _Smoothness * 100.0);
                finalColor += spec * mainLight.color * _Metallic * 0.5;
                
                // Extra shine on water
                float waterSpec = pow(saturate(dot(normalWS, halfVec)), 60.0) * waterMask * 0.3;
                finalColor += waterSpec * mainLight.color;
                
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
    
    FallBack "Universal Render Pipeline/Unlit"
}
