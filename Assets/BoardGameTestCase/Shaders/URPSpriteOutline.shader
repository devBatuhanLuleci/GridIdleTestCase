Shader "Custom/URPSuperSprite"
{
    Properties
    {
        [MainTexture] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Primary Outline)]
        [Toggle(_OUTLINE_ON)] _UseOutline ("Use Primary Outline", Float) = 0
        _OutlineColor ("Primary Color 1", Color) = (1,1,1,1)
        _OutlineColor2 ("Primary Color 2 (Gradient)", Color) = (1,1,1,1)
        _OutlineWidth ("Primary Width", Range(0, 20)) = 2
        _OutlineGlow ("Primary Glow", Range(0, 10)) = 1
        
        [Header(Secondary Outline)]
        [Toggle(_OUTLINE_SECONDARY_ON)] _UseOutlineSecondary ("Use Secondary Outline", Float) = 0
        _OutlineColorSecondary ("Secondary Color", Color) = (0,0,0,1)
        _OutlineWidthSecondary ("Secondary Width", Range(0, 20)) = 1
        _OutlineGlowSecondary ("Secondary Glow", Range(0, 10)) = 1

        [Header(Outline Global Settings)]
        _OutlineSmoothness ("Global Smoothness", Range(0, 1)) = 0.5
        _AlphaThreshold ("Alpha Threshold", Range(0, 1)) = 0.5
        _OutlineOffset ("Outline Offset", Vector) = (0,0,0,0)
        _OutlineGradientSpeed ("Gradient Speed", Range(0, 10)) = 0
        
        [Header(Mesh Boundary Fix)]
        _VerticesExpand ("Expand Mesh", Range(0, 1)) = 0.1

        [Header(Inner Sprite Outline)]
        [Toggle(_INNER_OUTLINE_ON)] _UseInnerOutline ("Use Inner Sprite Outline", Float) = 0
        _InnerOutlineColor ("Inner Color", Color) = (1,1,0,1)
        _InnerOutlineWidth ("Inner Width", Range(0, 5)) = 1

        [Header(Shine Glow Effect)]
        [Toggle(_SHINE_ON)] _UseShine ("Use Shine Effect", Float) = 0
        [HDR] _ShineColor ("Shine Color", Color) = (1,1,1,1)
        _ShineLocation ("Shine Location", Range(-1, 2)) = -0.5
        _ShineWidth ("Shine Width", Range(0, 1)) = 0.1
        _ShineSmoothness ("Shine Smoothness", Range(0.01, 1)) = 0.2
        _ShineSpeed ("Shine Auto Speed", Range(0, 10)) = 1
        _ShinePause ("Shine Pause Duration", Range(0, 10)) = 1
        _ShineAngle ("Shine Angle", Range(0, 360)) = 45
        _ShineTex ("Shine Pattern (Optional)", 2D) = "white" {}

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
            #pragma shader_feature _OUTLINE_SECONDARY_ON
            #pragma shader_feature _INNER_OUTLINE_ON
            #pragma shader_feature _SHINE_ON
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

            TEXTURE2D(_ShineTex);
            SAMPLER(sampler_ShineTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MainTex_TexelSize;
                float4 _Color;
                
                float4 _OutlineColor;
                float4 _OutlineColor2;
                float _OutlineWidth;
                
                float4 _OutlineColorSecondary;
                float _OutlineWidthSecondary;
                float _OutlineGlowSecondary;

                float _OutlineSmoothness;
                float _AlphaThreshold;
                float _OutlineGlow;
                float4 _OutlineOffset;
                float _OutlineGradientSpeed;

                float _VerticesExpand;
                
                float4 _InnerOutlineColor;
                float _InnerOutlineWidth;

                float4 _ShineColor;
                float _ShineLocation;
                float _ShineWidth;
                float _ShineSmoothness;
                float _ShineSpeed;
                float _ShinePause;
                float _ShineAngle;
                float4 _ShineTex_ST;

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

                #if defined(_OUTLINE_ON) || defined(_OUTLINE_SECONDARY_ON)
                    float maxWidth = max(_OutlineWidth, _OutlineWidthSecondary);
                    float2 expansion = sign(input.uv - 0.5) * _VerticesExpand * maxWidth * 0.01;
                    positionOS.xy += expansion;
                #endif

                output.positionCS = TransformObjectToHClip(positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.originalUv = input.uv;
                output.color = input.color * _Color * _RendererColor;

                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float2 uv = input.uv;
                
                #if defined(_OUTLINE_ON) || defined(_OUTLINE_SECONDARY_ON)
                    float maxWidth = max(_OutlineWidth, _OutlineWidthSecondary);
                    float expandFactor = 1.0 + (_VerticesExpand * maxWidth * 0.01) * 2.0;
                    uv = (input.originalUv - 0.5) * expandFactor + 0.5;
                    uv = uv * _MainTex_ST.xy + _MainTex_ST.zw;
                #endif

                #if _DISTORTION_ON
                    float distort = sin(_Time.y * _DistortSpeed + uv.y * _DistortFreq) * _DistortAmount;
                    uv.x += distort;
                #endif

                bool isOutside = uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1;
                float4 mainTexColor = isOutside ? float4(0,0,0,0) : SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                float alpha = mainTexColor.a;

                float3 baseRGB = mainTexColor.rgb;
                baseRGB *= _Brightness;
                baseRGB = (baseRGB - 0.5) * _Contrast + 0.5;
                float greyValue = dot(baseRGB, lum);
                baseRGB = lerp(baseRGB, float3(greyValue, greyValue, greyValue), _Grayscale);
                baseRGB = lerp(float3(greyValue, greyValue, greyValue), baseRGB, _Saturation);
                baseRGB = lerp(baseRGB, _FlashColor.rgb, _FlashAmount);
                baseRGB += _EmissionColor.rgb * _EmissionPower;

                float finalAlpha = alpha * input.color.a * _AlphaMultiplier;
                float4 spriteLayer = float4(baseRGB * input.color.rgb, finalAlpha);
                
                #if _SHINE_ON
                    float angleRad = _ShineAngle * 0.0174533;
                    float2 shineDir = float2(cos(angleRad), sin(angleRad));
                    float shinePos = (uv.x * shineDir.x + uv.y * shineDir.y);
                    
                    // Delay/Pause Logic
                    float totalCycle = 2.0 + _ShinePause; // Total distance + pause
                    float t = fmod(_Time.y * _ShineSpeed, totalCycle);
                    float shineMask = step(t, 2.0); // Only visible during traversal (range 2.0 covers -0.5 to 1.5)
                    
                    float effectiveLocation = _ShineLocation + t;
                    if (_ShineSpeed <= 0) effectiveLocation = _ShineLocation;

                    float shine = smoothstep(effectiveLocation - _ShineWidth - _ShineSmoothness, effectiveLocation - _ShineWidth, shinePos) 
                                - smoothstep(effectiveLocation + _ShineWidth, effectiveLocation + _ShineWidth + _ShineSmoothness, shinePos);
                    
                    float4 shineTexCol = SAMPLE_TEXTURE2D(_ShineTex, sampler_ShineTex, TRANSFORM_TEX(uv, _ShineTex));
                    float3 shineLayer = _ShineColor.rgb * shine * _ShineColor.a * shineTexCol.rgb * shineMask;
                    spriteLayer.rgb += shineLayer * alpha;
                #endif

                float3 outlineRGB = float3(0,0,0);
                float outlineAlpha = 0;

                #if defined(_OUTLINE_ON) || defined(_OUTLINE_SECONDARY_ON)
                    float2 offsetUv = uv + _OutlineOffset.xy * _MainTex_TexelSize.xy;
                    const float2 directions[16] = {
                        float2(1, 0), float2(-1, 0), float2(0, 1), float2(0, -1),
                        float2(0.707, 0.707), float2(-0.707, 0.707), float2(0.707, -0.707), float2(-0.707, -0.707),
                        float2(0.923, 0.382), float2(0.923, -0.382), float2(-0.923, 0.382), float2(-0.923, -0.382),
                        float2(0.382, 0.923), float2(0.382, -0.923), float2(-0.382, 0.923), float2(-0.382, -0.923)
                    };

                    float edgeSmooth = max(0.01, _OutlineSmoothness);
                    
                    float factorPrimary = 0;
                    #if _OUTLINE_ON
                        float alphaMax1 = 0;
                        float2 texelSize1 = _MainTex_TexelSize.xy * _OutlineWidth;
                        for (int i = 0; i < 16; i++) {
                            float2 sUv = offsetUv + directions[i] * texelSize1;
                            float a = (sUv.x < 0 || sUv.x > 1 || sUv.y < 0 || sUv.y > 1) ? 0 : SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, sUv).a;
                            alphaMax1 = max(alphaMax1, a);
                        }
                        factorPrimary = smoothstep(_AlphaThreshold - edgeSmooth, _AlphaThreshold, alphaMax1);
                    #endif

                    float factorSecondary = 0;
                    #if _OUTLINE_SECONDARY_ON
                        float alphaMax2 = 0;
                        float2 texelSize2 = _MainTex_TexelSize.xy * _OutlineWidthSecondary;
                        for (int j = 0; j < 16; j++) {
                            float2 sUv2 = offsetUv + directions[j] * texelSize2;
                            float a2 = (sUv2.x < 0 || sUv2.x > 1 || sUv2.y < 0 || sUv2.y > 1) ? 0 : SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, sUv2).a;
                            alphaMax2 = max(alphaMax2, a2);
                        }
                        factorSecondary = smoothstep(_AlphaThreshold - edgeSmooth, _AlphaThreshold, alphaMax2);
                    #endif

                    float gradient = saturate(sin(uv.y * 3.0 + _Time.y * _OutlineGradientSpeed) * 0.5 + 0.5);
                    float4 colPrimary = lerp(_OutlineColor, _OutlineColor2, gradient);
                    colPrimary.rgb *= colPrimary.a * _OutlineGlow;
                    
                    float4 colSecondary = _OutlineColorSecondary;
                    colSecondary.rgb *= colSecondary.a * _OutlineGlowSecondary;

                    float3 combinedOutline = lerp(colPrimary.rgb, colSecondary.rgb, factorSecondary);
                    outlineAlpha = max(factorPrimary, factorSecondary) * (1.0 - alpha);
                    outlineRGB = combinedOutline;
                #endif

                #if _INNER_OUTLINE_ON
                    float2 innerTexelSize = _MainTex_TexelSize.xy * _InnerOutlineWidth;
                    float innerAlphaMin = 1;
                    innerAlphaMin = min(innerAlphaMin, (uv.x + innerTexelSize.x > 1 ? 0 : SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(innerTexelSize.x, 0)).a));
                    innerAlphaMin = min(innerAlphaMin, (uv.x - innerTexelSize.x < 0 ? 0 : SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-innerTexelSize.x, 0)).a));
                    innerAlphaMin = min(innerAlphaMin, (uv.y + innerTexelSize.y > 1 ? 0 : SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0, innerTexelSize.y)).a));
                    innerAlphaMin = min(innerAlphaMin, (uv.y - innerTexelSize.y < 0 ? 0 : SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0, -innerTexelSize.y)).a));
                    float innerOutlineFactor = smoothstep(0.5 + _OutlineSmoothness, 0.5, innerAlphaMin) * step(0.1, alpha);
                    
                    float4 innerCol = _InnerOutlineColor;
                    innerCol.rgb *= innerCol.a;
                    spriteLayer = lerp(spriteLayer, innerCol, innerOutlineFactor);
                #endif

                float4 finalColor = spriteLayer;
                finalColor.rgb *= finalColor.a;

                float4 outL = float4(outlineRGB, outlineAlpha);
                finalColor = lerp(finalColor, outL, outL.a);

                return finalColor;
            }
            ENDHLSL
        }
    }
}
