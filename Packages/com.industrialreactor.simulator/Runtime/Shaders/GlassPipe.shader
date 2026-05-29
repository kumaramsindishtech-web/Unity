// Industrial Reactor Simulator - Glass Pipe Shader (URP)
// Transparent glass with water flow inside

Shader "Industrial Reactor/Glass Pipe"
{
    Properties
    {
        [Header(Glass)]
        _GlassColor ("Glass Tint", Color) = (0.95, 0.97, 1.0, 0.1)
        _GlassSmoothness ("Glass Smoothness", Range(0, 1)) = 0.95
        
        [Header(Water)]
        _WaterColor ("Water Color", Color) = (0.2, 0.5, 0.85, 0.8)
        _FillProgress ("Fill Progress", Range(0, 1)) = 0
        _FlowSpeed ("Flow Speed", Range(0, 5)) = 2.0
        _FlowIntensity ("Flow Intensity", Range(0, 1)) = 1.0
        _FillDirection ("Fill Direction (1=Y+, -1=Y-)", Float) = 1
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline" 
        }

        Pass
        {
            Name "GlassWater"
            Tags { "LightMode" = "UniversalForward" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
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
                half4 _GlassColor;
                half _GlassSmoothness;
                half4 _WaterColor;
                half _FillProgress;
                half _FlowSpeed;
                half _FlowIntensity;
                half _FillDirection;
            CBUFFER_END

            // Simple noise
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
                
                // Fresnel for glass
                float NdotV = saturate(dot(normalWS, viewDir));
                float fresnel = pow(1.0 - NdotV, 4.0);
                
                // Water fill based on UV.y
                // If FillDirection > 0: fill from bottom (UV.y=0) upward
                // If FillDirection < 0: fill from top (UV.y=1) downward
                float fillCoord = _FillDirection > 0 ? IN.uv.y : (1.0 - IN.uv.y);
                float waterMask = step(fillCoord, _FillProgress) * _FlowIntensity;
                
                // Animated water pattern
                float2 flowUV = IN.uv;
                flowUV.y += time * _FlowSpeed * 0.1 * _FillDirection;
                
                float waterNoise = noise(flowUV * 10.0 + time * 0.5);
                waterNoise += noise(flowUV * 20.0 - time * 0.3) * 0.5;
                waterNoise = waterNoise / 1.5;
                
                // Water color with noise
                half4 waterColor = _WaterColor;
                waterColor.rgb += waterNoise * 0.15 * _WaterColor.rgb;
                
                // Glass color
                half4 glassColor = _GlassColor;
                glassColor.rgb += fresnel * 0.2;
                
                // Combine: show water where filled, glass where empty
                half4 finalColor;
                finalColor.rgb = lerp(glassColor.rgb, waterColor.rgb, waterMask);
                finalColor.a = lerp(glassColor.a + fresnel * 0.2, waterColor.a, waterMask);
                
                // Lighting
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                finalColor.rgb *= mainLight.color * (NdotL * 0.3 + 0.7);
                
                // Specular
                float3 halfVec = normalize(mainLight.direction + viewDir);
                float spec = pow(saturate(dot(normalWS, halfVec)), _GlassSmoothness * 200.0);
                finalColor.rgb += spec * mainLight.color * 0.4;
                
                // Extra water specular
                float waterSpec = pow(saturate(dot(normalWS, halfVec)), 50.0) * waterMask * 0.2;
                finalColor.rgb += waterSpec * mainLight.color;
                
                return finalColor;
            }
            ENDHLSL
        }
    }
    
    FallBack "Universal Render Pipeline/Unlit"
}
