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
        
        [Header(Mesh Extension Fix)]
        _MeshExtension ("Outline Extension Area", Range(0, 1)) = 0.2
        
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
                float4 localData  : TEXCOORD1; // xy: normalized OS pos, zw: expansion factor
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
                
                float _MeshExtension;
                
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

                float4 pos = input.positionOS;
                pos.xy *= _Flip.xy;

                // Capture original OS position normalized to roughly -0.5 to 0.5
                // Sprites are usually 1 unit wide in OS if not scaled.
                output.localData.xy = input.positionOS.xy;
                
                // Expand the mesh
                float expand = 1.0 + _MeshExtension;
                pos.xy *= expand;
                output.localData.zw = float2(_MeshExtension, expand);

                output.positionCS = TransformObjectToHClip(pos.xyz);
                output.uv = input.uv;
                output.color = input.color * _Color * _RendererColor;

                return output;
            }

            // Helper to get alpha while respecting atlas bounds
            float GetAlpha(float2 uv, float2 localPos)
            {
                // If localPos is outside the original -0.5 to 0.5 range, it's transparent
                if (abs(localPos.x) > 0.5 || abs(localPos.y) > 0.5) return 0;
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
            }

            float4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float2 localPos = input.localData.xy; 
                float expand = input.localData.w;
                
                // Map the interpolated OS position back to original sprite UVs
                // Since pos was multiplied by 'expand', localPos is now in range -(0.5*expand) to +(0.5*expand)
                float2 originalLocalPos = localPos; // This is the OS pos of the current pixel
                float2 normalized01 = (originalLocalPos / 1.0) + 0.5; // Back to 0-1 local space
                
                float2 uv = TRANSFORM_TEX(normalized01, _MainTex);

                #if _DISTORTION_ON
                    float distort = sin(_Time.y * _DistortSpeed + uv.y * _DistortFreq) * _DistortAmount;
                    uv.x += distort;
                #endif

                // Get original sprite color and alpha
                bool isOutsideOriginal = abs(originalLocalPos.x) > 0.5 || abs(originalLocalPos.y) > 0.5;
                float4 mainTexColor = isOutsideOriginal ? float4(0,0,0,0) : SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
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
                    float shinePos = (normalized01.x * shineDir.x + normalized01.y * shineDir.y);
                    
                    float totalCycle = 2.0 + _ShinePause;
                    float t = fmod(_Time.y * _ShineSpeed, totalCycle);
                    float shineMask = step(t, 2.0);
                    
                    float effectiveLocation = _ShineLocation + t;
                    if (_ShineSpeed <= 0) effectiveLocation = _ShineLocation;

                    float shine = smoothstep(effectiveLocation - _ShineWidth - _ShineSmoothness, effectiveLocation - _ShineWidth, shinePos) 
                                - smoothstep(effectiveLocation + _ShineWidth, effectiveLocation + _ShineWidth + _ShineSmoothness, shinePos);
                    
                    float4 shineTexCol = SAMPLE_TEXTURE2D(_ShineTex, sampler_ShineTex, TRANSFORM_TEX(normalized01, _ShineTex));
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
                    
                    // To stay atlas safe while sampling neighbors, we need to know the LOCAL 0-1 of the neighbors too
                    // Fortunately, ddx/ddy of normalized01 gives us the step size.
                    float2 localStep1 = _OutlineWidth * 0.01; // Rough estimation based on OS units
                    
                    float factorPrimary = 0;
                    #if _OUTLINE_ON
                        float alphaMax1 = 0;
                        for (int i = 0; i < 16; i++) {
                            float2 neighborLocalPos = originalLocalPos + directions[i] * localStep1;
                            float2 neighborUV = TRANSFORM_TEX(neighborLocalPos + 0.5, _MainTex);
                            float a = (abs(neighborLocalPos.x) > 0.5 || abs(neighborLocalPos.y) > 0.5) ? 0 : SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, neighborUV).a;
                            alphaMax1 = max(alphaMax1, a);
                        }
                        factorPrimary = smoothstep(_AlphaThreshold - edgeSmooth, _AlphaThreshold, alphaMax1);
                    #endif

                    float factorSecondary = 0;
                    #if _OUTLINE_SECONDARY_ON
                        float2 localStep2 = _OutlineWidthSecondary * 0.01;
                        float alphaMax2 = 0;
                        for (int j = 0; j < 16; j++) {
                            float2 neighborLocalPos2 = originalLocalPos + directions[j] * localStep2;
                            float2 neighborUV2 = TRANSFORM_TEX(neighborLocalPos2 + 0.5, _MainTex);
                            float a2 = (abs(neighborLocalPos2.x) > 0.5 || abs(neighborLocalPos2.y) > 0.5) ? 0 : SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, neighborUV2).a;
                            alphaMax2 = max(alphaMax2, a2);
                        }
                        factorSecondary = smoothstep(_AlphaThreshold - edgeSmooth, _AlphaThreshold, alphaMax2);
                    #endif

                    float gradient = saturate(sin(normalized01.y * 3.0 + _Time.y * _OutlineGradientSpeed) * 0.5 + 0.5);
                    float4 colPrimary = lerp(_OutlineColor, _OutlineColor2, gradient);
                    colPrimary.rgb *= colPrimary.a * _OutlineGlow;
                    
                    float4 colSecondary = _OutlineColorSecondary;
                    colSecondary.rgb *= colSecondary.a * _OutlineGlowSecondary;

                    float3 combinedOutline = lerp(colPrimary.rgb, colSecondary.rgb, factorSecondary);
                    outlineAlpha = max(factorPrimary, factorSecondary) * (1.0 - alpha);
                    outlineRGB = combinedOutline;
                #endif

                #if _INNER_OUTLINE_ON
                    float2 innerLocalStep = _InnerOutlineWidth * 0.01;
                    float innerAlphaMin = 1;

                    // Samples for inner outline using OS-based UV remapping
                    float2 n1 = originalLocalPos + float2(innerLocalStep.x, 0);
                    innerAlphaMin = min(innerAlphaMin, (abs(n1.x) > 0.5 || abs(n1.y) > 0.5 ? 0 : SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, TRANSFORM_TEX(n1 + 0.5, _MainTex)).a));
                    float2 n2 = originalLocalPos + float2(-innerLocalStep.x, 0);
                    innerAlphaMin = min(innerAlphaMin, (abs(n2.x) > 0.5 || abs(n2.y) > 0.5 ? 0 : SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, TRANSFORM_TEX(n2 + 0.5, _MainTex)).a));
                    float2 n3 = originalLocalPos + float2(0, innerLocalStep.y);
                    innerAlphaMin = min(innerAlphaMin, (abs(n3.x) > 0.5 || abs(n3.y) > 0.5 ? 0 : SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, TRANSFORM_TEX(n3 + 0.5, _MainTex)).a));
                    float2 n4 = originalLocalPos + float2(0, -innerLocalStep.y);
                    innerAlphaMin = min(innerAlphaMin, (abs(n4.x) > 0.5 || abs(n4.y) > 0.5 ? 0 : SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, TRANSFORM_TEX(n4 + 0.5, _MainTex)).a));
                    
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
