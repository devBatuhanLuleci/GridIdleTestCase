Shader "Custom/URPSuperSprite"
{
    Properties
    {
        [MainTexture] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Outline Settings)]
        [Toggle(_OUTLINE_ON)] _UseOutline ("Use Outline", Float) = 0
        _OutlineColor ("Outline Color 1", Color) = (1,1,1,1)
        _OutlineColor2 ("Outline Color 2 (Gradient)", Color) = (1,1,1,1)
        _OutlineWidth ("Outline Width", Range(0, 20)) = 1
        _OutlineSmoothness ("Outline Smoothness", Range(0, 1)) = 0.5
        _AlphaThreshold ("Alpha Threshold", Range(0, 1)) = 0.5
        _OutlineGlow ("Outline Glow Power", Range(0, 10)) = 1
        _OutlineOffset ("Outline Offset", Vector) = (0,0,0,0)
        _OutlineGradientSpeed ("Gradient Speed", Range(0, 10)) = 0
        
        [Header(Mesh Boundary Fix)]
        _VerticesExpand ("Expand Mesh (Fix Clipping)", Range(0, 1)) = 0.1

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
                float2 originalUv : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MainTex_TexelSize;
                float4 _Color;
                
                float4 _OutlineColor;
                float4 _OutlineColor2;
                float _OutlineWidth;
                float _OutlineSmoothness;
                float _AlphaThreshold;
                float _OutlineGlow;
                float4 _OutlineOffset;
                float _OutlineGradientSpeed;

                float _VerticesExpand;
                
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
                
                // Flip support
                positionOS.xy *= _Flip.xy;

                // Vertex Inflation to prevent clipping at mesh edges
                #if _OUTLINE_ON
                    float2 expansion = sign(input.uv - 0.5) * _VerticesExpand * _OutlineWidth * 0.01;
                    positionOS.xy += expansion;
                #endif

                output.positionCS = TransformObjectToHClip(positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.originalUv = input.uv; // Keep original for coordinate logic
                output.color = input.color * _Color * _RendererColor;

                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float2 uv = input.uv;
                
                // Correct UV mapping if mesh was expanded
                #if _OUTLINE_ON
                    float expandFactor = 1.0 + (_VerticesExpand * _OutlineWidth * 0.01) * 2.0;
                    uv = (input.originalUv - 0.5) * expandFactor + 0.5;
                    // Apply tiling/offset again after compensation
                    uv = uv * _MainTex_ST.xy + _MainTex_ST.zw;
                #endif

                #if _DISTORTION_ON
                    float distort = sin(_Time.y * _DistortSpeed + uv.y * _DistortFreq) * _DistortAmount;
                    uv.x += distort;
                #endif

                // Safe UV check: if we expanded too much, pixels outside 0-1 should be transparent
                bool isOutside = uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1;
                float4 mainTexColor = isOutside ? float4(0,0,0,0) : SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                float alpha = mainTexColor.a;

                float outlineFactor = 0;
                #if _OUTLINE_ON
                    float2 texelSize = _MainTex_TexelSize.xy * _OutlineWidth;
                    float2 offsetUv = uv + _OutlineOffset.xy * _MainTex_TexelSize.xy;
                    
                    float alphaMax = 0;
                    
                    const float2 directions[16] = {
                        float2(1, 0), float2(-1, 0), float2(0, 1), float2(0, -1),
                        float2(0.707, 0.707), float2(-0.707, 0.707), float2(0.707, -0.707), float2(-0.707, -0.707),
                        float2(0.923, 0.382), float2(0.923, -0.382), float2(-0.923, 0.382), float2(-0.923, -0.382),
                        float2(0.382, 0.923), float2(0.382, -0.923), float2(-0.382, 0.923), float2(-0.382, -0.923)
                    };

                    for (int i = 0; i < 16; i++) {
                        float2 sampleUv = offsetUv + directions[i] * texelSize;
                        // Only sample if inside original texture bounds to prevent bleed
                        float a = (sampleUv.x < 0 || sampleUv.x > 1 || sampleUv.y < 0 || sampleUv.y > 1) ? 0 : SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, sampleUv).a;
                        alphaMax = max(alphaMax, a);
                    }

                    float edgeWidth = max(0.01, _OutlineSmoothness);
                    outlineFactor = smoothstep(_AlphaThreshold - edgeWidth, _AlphaThreshold, alphaMax) * (1.0 - alpha);
                #endif

                float innerOutlineFactor = 0;
                #if _INNER_OUTLINE_ON
                    float2 innerTexelSize = _MainTex_TexelSize.xy * _InnerOutlineWidth;
                    float innerAlphaMin = 1;
                    innerAlphaMin = min(innerAlphaMin, (uv.x + innerTexelSize.x > 1 ? 0 : SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(innerTexelSize.x, 0)).a));
                    innerAlphaMin = min(innerAlphaMin, (uv.x - innerTexelSize.x < 0 ? 0 : SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-innerTexelSize.x, 0)).a));
                    innerAlphaMin = min(innerAlphaMin, (uv.y + innerTexelSize.y > 1 ? 0 : SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0, innerTexelSize.y)).a));
                    innerAlphaMin = min(innerAlphaMin, (uv.y - innerTexelSize.y < 0 ? 0 : SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0, -innerTexelSize.y)).a));
                    innerOutlineFactor = smoothstep(0.5 + _OutlineSmoothness, 0.5, innerAlphaMin) * step(0.1, alpha);
                #endif

                // Base Color Processing
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
                    float gradient = saturate(sin(uv.y * 3.0 + _Time.y * _OutlineGradientSpeed) * 0.5 + 0.5);
                    float4 outColor = lerp(_OutlineColor, _OutlineColor2, gradient);
                    outColor.rgb *= outColor.a * _OutlineGlow;
                    finalColor = lerp(finalColor, outColor, outlineFactor);
                #endif

                return finalColor;
            }
            ENDHLSL
        }
    }
}
