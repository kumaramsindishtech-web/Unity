// Industrial Reactor Simulator - Water Shader (URP)
// Copyright (c) 2024 Industrial Reactor Simulator. MIT License.
// Object-space based water fill from bottom (Z-axis)

Shader "Industrial Reactor/Water"
{
    Properties
    {
        [Header(Water Colors)]
        _ShallowColor ("Shallow Color", Color) = (0.3, 0.7, 0.9, 0.7)
        _DeepColor ("Deep Color", Color) = (0.1, 0.3, 0.6, 0.9)
        
        [Header(Fill Settings - Object Space)]
        _WaterLevel ("Water Level", Range(0, 1)) = 0.5
        _FillMin ("Fill Min (Object Z)", Float) = -0.5
        _FillMax ("Fill Max (Object Z)", Float) = 0.5
        
        [Header(Appearance)]
        _Transparency ("Transparency", Range(0, 1)) = 0.75
        _FresnelPower ("Fresnel Power", Range(1, 10)) = 3.0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.9
        
        [Header(Waves)]
        _WaveSpeed ("Wave Speed", Range(0, 5)) = 1.0
        _WaveScale ("Wave Scale", Range(1, 30)) = 10.0
        _WaveAmplitude ("Wave Amplitude", Range(0, 0.05)) = 0.01
        
        [Header(Swirl from Agitator)]
        _SwirlSpeed ("Swirl Speed", Range(0, 360)) = 0
        _SwirlStrength ("Swirl Strength", Range(0, 1)) = 0.3
        
        [Header(Emission)]
        [HDR] _EmissionColor ("Emission Color", Color) = (0.2, 0.5, 1, 1)
        _EmissionIntensity ("Emission Intensity", Range(0, 3)) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        LOD 200

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
                float3 positionOS : TEXCOORD2;
                float3 normalWS : TEXCOORD3;
                float fogFactor : TEXCOORD4;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor;
                float4 _DeepColor;
                float _WaterLevel;
                float _FillMin;
                float _FillMax;
                float _Transparency;
                float _FresnelPower;
                float _Smoothness;
                float _WaveSpeed;
                float _WaveScale;
                float _WaveAmplitude;
                float _SwirlSpeed;
                float _SwirlStrength;
                float4 _EmissionColor;
                float _EmissionIntensity;
            CBUFFER_END

            // Simple noise
            float noise(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }

            float smoothNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);
                float a = noise(i);
                float b = noise(i + float2(1, 0));
                float c = noise(i + float2(0, 1));
                float d = noise(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                float3 posOS = input.positionOS.xyz;
                output.positionOS = posOS;
                
                // Wave animation on surface
                float time = _Time.y * _WaveSpeed;
                float wave = sin(posOS.x * _WaveScale + time) * cos(posOS.y * _WaveScale * 0.8 + time * 0.9);
                wave += sin(posOS.x * _WaveScale * 2.0 + time * 1.3) * 0.5;
                wave *= _WaveAmplitude;
                
                // Apply wave to top surface (high Z in object space)
                float normalizedZ = (posOS.z - _FillMin) / (_FillMax - _FillMin);
                float isTop = smoothstep(0.8, 1.0, normalizedZ);
                posOS.z += wave * isTop * _WaterLevel;
                
                VertexPositionInputs posInputs = GetVertexPositionInputs(posOS);
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float time = _Time.y;
                
                // Object-space Z position for fill calculation
                // Fill from bottom (low Z) to top (high Z)
                float fillRange = _FillMax - _FillMin;
                float currentFillHeight = _FillMin + fillRange * _WaterLevel;
                
                // Clip pixels above current water level
                // Water fills from _FillMin (bottom) upward
                clip(currentFillHeight - input.positionOS.z);
                
                float3 normalWS = normalize(input.normalWS);
                float3 viewDir = normalize(GetWorldSpaceViewDir(input.positionWS));
                
                // Depth-based color (deeper = darker)
                float depth = saturate((currentFillHeight - input.positionOS.z) / fillRange);
                float4 waterColor = lerp(_ShallowColor, _DeepColor, depth * 0.5);
                
                // Swirl effect on UV
                float2 centeredUV = input.uv - 0.5;
                float swirlAngle = _SwirlSpeed * time * 0.0174533 * _SwirlStrength;
                float dist = length(centeredUV);
                float swirl = swirlAngle * (1.0 - dist);
                float cs = cos(swirl);
                float sn = sin(swirl);
                float2 swirlUV = float2(centeredUV.x * cs - centeredUV.y * sn, centeredUV.x * sn + centeredUV.y * cs) + 0.5;
                
                // Caustic-like pattern
                float caustic = smoothNoise(swirlUV * 8.0 + time * 0.3);
                caustic = smoothNoise(swirlUV * 4.0 + caustic * 0.5 + time * 0.2);
                waterColor.rgb += caustic * 0.15 * _ShallowColor.rgb;
                
                // Fresnel
                float NdotV = saturate(dot(normalWS, viewDir));
                float fresnel = pow(1.0 - NdotV, _FresnelPower);
                waterColor.rgb += fresnel * 0.2;
                
                // Lighting
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                float3 diffuse = waterColor.rgb * mainLight.color * (NdotL * 0.4 + 0.6);
                
                // Specular
                float3 halfDir = normalize(mainLight.direction + viewDir);
                float NdotH = saturate(dot(normalWS, halfDir));
                float spec = pow(NdotH, _Smoothness * 128.0) * 0.5;
                
                // Emission
                float pulse = sin(time * 2.0) * 0.5 + 0.5;
                float3 emission = _EmissionColor.rgb * _EmissionIntensity * (0.7 + pulse * 0.3);
                
                float3 finalColor = diffuse + spec * mainLight.color + emission;
                finalColor = MixFog(finalColor, input.fogFactor);
                
                // Alpha
                float alpha = _Transparency;
                alpha = lerp(alpha, min(alpha + 0.3, 1.0), fresnel);
                
                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
