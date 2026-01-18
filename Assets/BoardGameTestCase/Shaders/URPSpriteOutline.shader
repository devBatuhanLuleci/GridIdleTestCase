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
        
        [Header(Mesh Padding Fix)]
        _MeshExtension ("Outline Space Extension", Range(0, 1)) = 0.1
        
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

        [Header(Fill Tiling)]
        [Toggle(_FILL_TILE_ON)] _UseFillTile ("Use Fill Tiling", Float) = 0
        _FillTex ("Fill Texture Pattern", 2D) = "white" {}
        _FillColor ("Fill Tint", Color) = (1,1,1,1)
        _FillTiling ("Fill Tiling (XY) Scroll (ZW)", Vector) = (1,1,0,0)
        _FillAmount ("Fill Intensity", Range(0, 1)) = 1

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
            #pragma shader_feature _FILL_TILE_ON
            
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
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_ShineTex);
            SAMPLER(sampler_ShineTex);

            TEXTURE2D(_FillTex);
            SAMPLER(sampler_FillTex);

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

                float4 _FillColor;
                float4 _FillTiling;
                float _FillAmount;
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

                // Expand mesh geometry by adding padding
                // This is the clean object-space way that doesn't mess with UV logic
                float expand = 1.0 + _MeshExtension;
                pos.xy *= expand;

                output.positionCS = TransformObjectToHClip(pos.xyz);
                output.uv = input.uv;
                output.color = input.color * _Color * _RendererColor;

                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float expansion = 1.0 + _MeshExtension;
                
                // CRUCIAL: Atlas-Safe Center logic. Unity's _MainTex_ST.zw is the offset, .xy is scale.
                // The center of the sprite in Atlas space is naturally the offset + half the scale.
                float2 centerUV = _MainTex_ST.zw + _MainTex_ST.xy * 0.5;
                
                // Remap the interpolated UV back to original sprite bounds
                // This "shrinks" the sampling to keep the sprite size original while the mesh is bigger.
                float2 uv = (input.uv - centerUV) * expansion + centerUV;

                #if _DISTORTION_ON
                    float distort = sin(_Time.y * _DistortSpeed + uv.y * _DistortFreq) * _DistortAmount;
                    uv.x += distort;
                #endif

                // Alpha masking: If the UV is outside the original sprite rect in the atlas, it must be transparent.
                // This prevents bleeding from other sprites in the atlas when the mesh is expanded.
                float2 minLimit = _MainTex_ST.zw;
                float2 maxLimit = _MainTex_ST.zw + _MainTex_ST.xy;
                bool isOutside = uv.x < minLimit.x || uv.x > maxLimit.x || uv.y < minLimit.y || uv.y > maxLimit.y;
                
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

                // Fill Tiling Logic
                #if _FILL_TILE_ON
                    float2 fillUv = (uv - _MainTex_ST.zw) / _MainTex_ST.xy;
                    fillUv = fillUv * _FillTiling.xy + _FillTiling.zw * _Time.y;
                    float4 fillCol = SAMPLE_TEXTURE2D(_FillTex, sampler_FillTex, fillUv) * _FillColor;
                    baseRGB = lerp(baseRGB, fillCol.rgb, _FillAmount * fillCol.a);
                #endif

                float finalAlpha = alpha * input.color.a * _AlphaMultiplier;
                float4 spriteLayer = float4(baseRGB * input.color.rgb, finalAlpha);
                
                // Shine Logic
                #if _SHINE_ON
                    float angleRad = _ShineAngle * 0.0174533;
                    float2 shineDir = float2(cos(angleRad), sin(angleRad));
                    
                    // Use normalized local UV [0-1] for shine so it stays relative to sprite
                    float2 localUV01 = (uv - _MainTex_ST.zw) / _MainTex_ST.xy;
                    float shinePos = (localUV01.x * shineDir.x + localUV01.y * shineDir.y);
                    
                    float totalCycle = 2.0 + _ShinePause;
                    float t = fmod(_Time.y * _ShineSpeed, totalCycle);
                    float shineMask = step(t, 2.0);
                    
                    float effectiveLocation = _ShineLocation + t;
                    if (_ShineSpeed <= 0) effectiveLocation = _ShineLocation;

                    float shine = smoothstep(effectiveLocation - _ShineWidth - _ShineSmoothness, effectiveLocation - _ShineWidth, shinePos) 
                                - smoothstep(effectiveLocation + _ShineWidth, effectiveLocation + _ShineWidth + _ShineSmoothness, shinePos);
                    
                    float2 sUvTranslated = localUV01 * _ShineTex_ST.xy + _ShineTex_ST.zw;
                    float4 shineTexCol = SAMPLE_TEXTURE2D(_ShineTex, sampler_ShineTex, sUvTranslated);
                    float3 shineLayer = _ShineColor.rgb * shine * _ShineColor.a * shineTexCol.rgb * shineMask;
                    spriteLayer.rgb += shineLayer * alpha;
                #endif

                float3 outlineRGB = float3(0,0,0);
                float outlineAlpha = 0;

                // Outline Logic
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
                        float2 tSize1 = _MainTex_TexelSize.xy * _OutlineWidth;
                        for (int i = 0; i < 16; i++) {
                            float2 sUv = offsetUv + directions[i] * tSize1;
                            // Atlas-safe neighbor sampling
                            float a = (sUv.x < minLimit.x || sUv.x > maxLimit.x || sUv.y < minLimit.y || sUv.y > maxLimit.y) ? 0 : SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, sUv).a;
                            alphaMax1 = max(alphaMax1, a);
                        }
                        factorPrimary = smoothstep(_AlphaThreshold - edgeSmooth, _AlphaThreshold, alphaMax1);
                    #endif

                    float factorSecondary = 0;
                    #if _OUTLINE_SECONDARY_ON
                        float alphaMax2 = 0;
                        float2 tSize2 = _MainTex_TexelSize.xy * _OutlineWidthSecondary;
                        for (int j = 0; j < 16; j++) {
                            float2 sUv2 = offsetUv + directions[j] * tSize2;
                            float a2 = (sUv2.x < minLimit.x || sUv2.x > maxLimit.x || sUv2.y < minLimit.y || sUv2.y > maxLimit.y) ? 0 : SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, sUv2).a;
                            alphaMax2 = max(alphaMax2, a2);
                        }
                        factorSecondary = smoothstep(_AlphaThreshold - edgeSmooth, _AlphaThreshold, alphaMax2);
                    #endif

                    // Gradient for primary outline
                    float2 localUV01_grad = (uv - _MainTex_ST.zw) / _MainTex_ST.xy;
                    float gradient = saturate(sin(localUV01_grad.y * 3.0 + _Time.y * _OutlineGradientSpeed) * 0.5 + 0.5);
                    float4 colPrimary = lerp(_OutlineColor, _OutlineColor2, gradient);
                    colPrimary.rgb *= colPrimary.a * _OutlineGlow;
                    
                    float4 colSecondary = _OutlineColorSecondary;
                    colSecondary.rgb *= colSecondary.a * _OutlineGlowSecondary;

                    float3 combinedOutline = lerp(colPrimary.rgb, colSecondary.rgb, factorSecondary);
                    outlineAlpha = max(factorPrimary, factorSecondary) * (1.0 - alpha);
                    outlineRGB = combinedOutline;
                #endif

                #if _INNER_OUTLINE_ON
                    float2 innerTSize = _MainTex_TexelSize.xy * _InnerOutlineWidth;
                    float innerAlphaMin = 1;
                    float2 n1 = uv + float2(innerTSize.x, 0);
                    innerAlphaMin = min(innerAlphaMin, (n1.x < minLimit.x || n1.x > maxLimit.x || n1.y < minLimit.y || n1.y > maxLimit.y ? 0 : SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, n1).a));
                    float2 n2 = uv + float2(-innerTSize.x, 0);
                    innerAlphaMin = min(innerAlphaMin, (n2.x < minLimit.x || n2.x > maxLimit.x || n2.y < minLimit.y || n2.y > maxLimit.y ? 0 : SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, n2).a));
                    float2 n3 = uv + float2(0, innerTSize.y);
                    innerAlphaMin = min(innerAlphaMin, (n3.x < minLimit.x || n3.x > maxLimit.x || n3.y < minLimit.y || n3.y > maxLimit.y ? 0 : SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, n3).a));
                    float2 n4 = uv + float2(0, -innerTSize.y);
                    innerAlphaMin = min(innerAlphaMin, (n4.x < minLimit.x || n4.x > maxLimit.x || n4.y < minLimit.y || n4.y > maxLimit.y ? 0 : SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, n4).a));
                    
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
