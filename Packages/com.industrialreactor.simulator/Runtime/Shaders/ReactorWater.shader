// Industrial Reactor Simulator - Water Shader (URP)
// Simple, working water shader with Y-axis fill (bottom to top)

Shader "Industrial Reactor/Water"
{
    Properties
    {
        _Color ("Water Color", Color) = (0.2, 0.5, 0.8, 0.7)
        _DeepColor ("Deep Color", Color) = (0.1, 0.3, 0.5, 0.9)
        _WaterLevel ("Water Level", Range(0, 1)) = 0.5
        _Smoothness ("Smoothness", Range(0, 1)) = 0.9
        _FresnelPower ("Fresnel Power", Range(1, 5)) = 2.0
        
        [Header(Waves)]
        _WaveSpeed ("Wave Speed", Float) = 1.0
        _WaveStrength ("Wave Strength", Range(0, 0.1)) = 0.02
        
        [Header(Swirl)]
        _SwirlSpeed ("Swirl Speed", Range(0, 100)) = 0
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
            Name "WaterPass"
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
                float3 positionOS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _DeepColor;
                half _WaterLevel;
                half _Smoothness;
                half _FresnelPower;
                half _WaveSpeed;
                half _WaveStrength;
                half _SwirlSpeed;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                
                float3 posOS = IN.positionOS.xyz;
                
                // Add wave motion to top vertices
                float time = _Time.y * _WaveSpeed;
                float wave = sin(posOS.x * 10.0 + time) * cos(posOS.z * 8.0 + time * 0.7);
                wave *= _WaveStrength * saturate(IN.uv.y * 2.0); // Only affect top
                posOS.y += wave;
                
                OUT.positionOS = IN.positionOS.xyz; // Original position for clipping
                OUT.positionCS = TransformObjectToHClip(posOS);
                OUT.positionWS = TransformObjectToWorld(posOS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = IN.uv;
                
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Get object bounds - assume mesh goes from Y=0 to Y=1 in normalized space
                // Or use UV.y as height indicator
                float heightNormalized = IN.uv.y;
                
                // Fill from BOTTOM: discard pixels where height > water level
                // UV.y = 0 is bottom, UV.y = 1 is top
                clip(_WaterLevel - heightNormalized + 0.001);
                
                // Calculate depth for color
                float depth = 1.0 - (heightNormalized / max(_WaterLevel, 0.01));
                half4 waterColor = lerp(_Color, _DeepColor, saturate(depth * 0.5));
                
                // Fresnel
                float3 viewDir = normalize(GetWorldSpaceViewDir(IN.positionWS));
                float3 normalWS = normalize(IN.normalWS);
                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDir)), _FresnelPower);
                waterColor.rgb += fresnel * 0.15;
                
                // Simple lighting
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                waterColor.rgb *= mainLight.color * (NdotL * 0.5 + 0.5);
                
                // Specular
                float3 halfVec = normalize(mainLight.direction + viewDir);
                float spec = pow(saturate(dot(normalWS, halfVec)), _Smoothness * 100.0);
                waterColor.rgb += spec * mainLight.color * 0.3;
                
                // Swirl pattern
                float time = _Time.y;
                float2 centeredUV = IN.uv - 0.5;
                float angle = _SwirlSpeed * time * 0.01;
                float dist = length(centeredUV);
                float swirl = sin(dist * 20.0 - angle) * 0.5 + 0.5;
                waterColor.rgb += swirl * 0.05 * _Color.rgb;
                
                return waterColor;
            }
            ENDHLSL
        }
    }
    
    FallBack "Universal Render Pipeline/Unlit"
}
