Shader "Custom/URPSpriteOutline"
{
    Properties
    {
        [MainTexture] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Outline Settings)]
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineThickness ("Outline Thickness", Range(0, 10)) = 1
        _AlphaThreshold ("Alpha Threshold", Range(0, 1)) = 0.01
        
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaScan ("AlphaScan", Float) = 0.0
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

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MainTex_TexelSize;
                float4 _Color;
                float4 _OutlineColor;
                float _OutlineThickness;
                float _AlphaThreshold;
            CBUFFER_END

            float4 _RendererColor;
            float4 _Flip;

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

                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float2 uv = input.uv;
                float4 mainColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                
                // Determine if this pixel is part of the sprite
                float alpha = mainColor.a;
                
                // Sampling offset based on texel size and thickness
                float2 texelSize = _MainTex_TexelSize.xy * _OutlineThickness;

                // 8-tap sampling for outline detection
                float alphaSum = 0;
                alphaSum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(texelSize.x, 0)).a;
                alphaSum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-texelSize.x, 0)).a;
                alphaSum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0, texelSize.y)).a;
                alphaSum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0, -texelSize.y)).a;
                
                alphaSum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(texelSize.x, texelSize.y)).a;
                alphaSum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-texelSize.x, texelSize.y)).a;
                alphaSum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(texelSize.x, -texelSize.y)).a;
                alphaSum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-texelSize.x, -texelSize.y)).a;

                // Simple check: if current pixel is transparent but there are opaque neighbors
                float outlineFactor = step(0.001, alphaSum) * step(alpha, _AlphaThreshold);
                
                // Combine main color with outline
                float4 finalColor = mainColor;
                finalColor.rgb *= alpha; // Premultiply alpha for Sprite compliance
                
                float4 outColor = _OutlineColor;
                outColor.rgb *= outColor.a; // Premultiply outline alpha
                
                // Apply outline where necessary
                finalColor = lerp(finalColor, outColor, outlineFactor);
                
                return finalColor * input.color;
            }
            ENDHLSL
        }
    }
}
