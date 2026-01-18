Shader "Custom/URPSuperSprite"
{
    Properties
    {
        [MainTexture] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Outline Settings)]
        [Toggle(_OUTLINE_ON)] _UseOutline ("Use Outline", Float) = 0
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineWidth ("Outline Width", Range(0, 5)) = 1
        _OutlineSmoothness ("Outline Smoothness", Range(0, 1)) = 0.5
        _AlphaThreshold ("Alpha Threshold", Range(0, 1)) = 0.5
        
        [Header(Inner Outline Settings)]
        [Toggle(_INNER_OUTLINE_ON)] _UseInnerOutline ("Use Inner Outline", Float) = 0
        _InnerOutlineColor ("Inner Outline Color", Color) = (1,1,0,1)
        _InnerOutlineWidth ("Inner Outline Width", Range(0, 5)) = 1

        [Header(Color Adjustments)]
        _Brightness ("Brightness", Range(0, 2)) = 1
        _Contrast ("Contrast", Range(0, 2)) = 1
        _Saturation ("Saturation", Range(0, 2)) = 1
        _Grayscale ("Grayscale Amount", Range(0, 1)) = 0
        
        [Header(Emission and Glow)]
        [HDR] _EmissionColor ("Emission Color", Color) = (0,0,0,0)
        _EmissionPower ("Emission Power", Range(0, 10)) = 0

        [Header(Flash Effect)]
        _FlashColor ("Flash Color", Color) = (1,1,1,1)
        _FlashAmount ("Flash Amount", Range(0, 1)) = 0

        [Header(Ghost Alpha)]
        _AlphaMultiplier ("Global Alpha Multiplier", Range(0, 1)) = 1
        
        [Header(Distortion)]
        [Toggle(_DISTORTION_ON)] _UseDistortion ("Use Distortion", Float) = 0
        _DistortSpeed ("Distort Speed", Range(0, 10)) = 1
        _DistortAmount ("Distort Amount", Range(0, 0.1)) = 0.01
        _DistortFreq ("Distort Freq", Range(0, 50)) = 10

        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma shader_feature _OUTLINE_ON
            #pragma shader_feature _INNER_OUTLINE_ON
            #pragma shader_feature _DISTORTION_ON
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
                float4 screenPos  : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MainTex_TexelSize;
                float4 _Color;
                
                float4 _OutlineColor;
                float _OutlineWidth;
                float _OutlineSmoothness;
                float _AlphaThreshold;
                
                float4 _InnerOutlineColor;
                float _InnerOutlineWidth;

                float _Brightness;
                float _Contrast;
                float _Saturation;
                float _Grayscale;
                
                float4 _EmissionColor;
                float _EmissionPower;
                
                float4 _FlashColor;
                float _FlashAmount;
                
                float _AlphaMultiplier;
                
                float _DistortSpeed;
                float _DistortAmount;
                float _DistortFreq;
            CBUFFER_END

            float4 _RendererColor;
            float4 _Flip;

            static const float3 lum = float3(0.299, 0.587, 0.114);

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float4 positionOS = input.positionOS;
                positionOS.xy *= _Flip.xy;

                output.positionCS = TransformObjectToHClip(positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color * _Color * _RendererColor;
                output.screenPos = ComputeScreenPos(output.positionCS);

                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float2 uv = input.uv;

                #if _DISTORTION_ON
                    float distort = sin(_Time.y * _DistortSpeed + uv.y * _DistortFreq) * _DistortAmount;
                    uv.x += distort;
                #endif

                float4 mainTexColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                float alpha = mainTexColor.a;

                float outlineFactor = 0;
                #if _OUTLINE_ON
                    float2 texelSize = _MainTex_TexelSize.xy * _OutlineWidth;
                    
                    // Sample more directions (16-tap) for smoother results
                    float alphaMax = 0;
                    
                    // Define sampling directions
                    float2 directions[16] = {
                        float2(1, 0), float2(-1, 0), float2(0, 1), float2(0, -1),
                        float2(0.7, 0.7), float2(-0.7, 0.7), float2(0.7, -0.7), float2(-0.7, -0.7),
                        float2(1, 0.5), float2(1, -0.5), float2(-1, 0.5), float2(-1, -0.5),
                        float2(0.5, 1), float2(0.5, -1), float2(-0.5, 1), float2(-0.5, -1)
                    };

                    for (int i = 0; i < 16; i++) {
                        alphaMax = max(alphaMax, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + directions[i] * texelSize).a);
                    }

                    // Smoothstep creates a soft gradient instead of a hard pixel edge
                    float edgeWidth = max(0.01, _OutlineSmoothness);
                    outlineFactor = smoothstep(_AlphaThreshold - edgeWidth, _AlphaThreshold, alphaMax) * (1.0 - alpha);
                #endif

                float innerOutlineFactor = 0;
                #if _INNER_OUTLINE_ON
                    float2 innerTexelSize = _MainTex_TexelSize.xy * _InnerOutlineWidth;
                    float innerAlphaMin = 1;
                    innerAlphaMin = min(innerAlphaMin, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(innerTexelSize.x, 0)).a);
                    innerAlphaMin = min(innerAlphaMin, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-innerTexelSize.x, 0)).a);
                    innerAlphaMin = min(innerAlphaMin, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0, innerTexelSize.y)).a);
                    innerAlphaMin = min(innerAlphaMin, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0, -innerTexelSize.y)).a);
                    innerOutlineFactor = smoothstep(0.5 + _OutlineSmoothness, 0.5, innerAlphaMin) * step(0.1, alpha);
                #endif

                float3 baseRGB = mainTexColor.rgb;
                baseRGB *= _Brightness;
                baseRGB = (baseRGB - 0.5) * _Contrast + 0.5;
                float g = dot(baseRGB, lum);
                float3 grayscaleColor = float3(g, g, g);
                baseRGB = lerp(baseRGB, grayscaleColor, _Grayscale);
                baseRGB = lerp(grayscaleColor, baseRGB, _Saturation);
                baseRGB = lerp(baseRGB, _FlashColor.rgb, _FlashAmount);
                baseRGB += _EmissionColor.rgb * _EmissionPower;

                float finalAlpha = alpha * input.color.a * _AlphaMultiplier;
                float4 spriteLayer = float4(baseRGB * input.color.rgb, finalAlpha);
                
                #if _INNER_OUTLINE_ON
                    float4 innerColor = _InnerOutlineColor;
                    innerColor.rgb *= innerColor.a;
                    spriteLayer = lerp(spriteLayer, innerColor, innerOutlineFactor);
                #endif

                float4 finalColor = spriteLayer;
                finalColor.rgb *= finalColor.a;

                #if _OUTLINE_ON
                    float4 outColor = _OutlineColor;
                    outColor.rgb *= outColor.a;
                    finalColor = lerp(finalColor, outColor, outlineFactor);
                #endif

                return finalColor;
            }
            ENDHLSL
        }
    }
}
