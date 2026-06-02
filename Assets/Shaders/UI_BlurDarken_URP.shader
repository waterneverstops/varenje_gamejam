Shader "UI/BlurSmoothDarkenNoise_URP"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

        // --- Blur ---
        _BlurRadius ("Blur Radius (px)", Range(0, 15)) = 6

        // --- Darken & Tint ---
        _Darken ("Darken", Range(0, 1)) = 0.65
        _Tint ("Tint Color", Color) = (0,0,0,1)

        // --- Noise ---
        _NoiseStrength ("Noise Strength", Range(0, 0.5)) = 0.05
        _NoiseScale ("Noise Scale", Float) = 120.0
        _NoiseSpeed ("Noise Animation Speed", Float) = 0.0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "UIBlurSmooth"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                float4 color : COLOR;
            };

            TEXTURE2D(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);
            float4 _CameraOpaqueTexture_TexelSize;

            float _BlurRadius;
            float _Darken;
            float4 _Tint;
            float _NoiseStrength;
            float _NoiseScale;
            float _NoiseSpeed;

            // Процедурный шум
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float noise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.screenPos = ComputeScreenPos(OUT.positionCS);
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;

                // Честное размытие: много выборок с шагом 1 пиксель
                int radius = (int)clamp(_BlurRadius, 0, 15);
                float2 pixelSize = _CameraOpaqueTexture_TexelSize.xy;

                half4 blurred = half4(0,0,0,0);
                int sampleCount = 0;

                for (int x = -radius; x <= radius; x++)
                {
                    for (int y = -radius; y <= radius; y++)
                    {
                        float2 offset = float2(x, y) * pixelSize;
                        blurred += SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV + offset);
                        sampleCount++;
                    }
                }

                blurred /= (float)sampleCount;

                // Затемнение + оттенок
                half4 darkened = lerp(blurred, _Tint, _Darken);

                // Зернистость
                float2 noiseUV = screenUV * _NoiseScale;
                noiseUV.y += _NoiseSpeed * _Time.y;
                float grain = noise(noiseUV);
                darkened.rgb += (grain - 0.5) * _NoiseStrength;

                darkened.a = 1.0;
                darkened *= IN.color;

                return darkened;
            }
            ENDHLSL
        }
    }
}