Shader "Fake Mirror/Clip Box Lit"
{
    Properties
    {
        [MainTexture] _MainTex ("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Range(0, 2)) = 1
        _SpecGlossMap ("Specular Map", 2D) = "white" {}
        _SpecColor ("Specular Color", Color) = (0.2, 0.2, 0.2, 1)
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
        _UvTiling ("UV Tiling", Vector) = (2, 2, 0, 0)
        _UvOffset ("UV Offset", Vector) = (0, 0, 0, 0)
        [Toggle] _ClipBoxEnabled ("Clip Box Enabled", Float) = 1
        _ClipBoxCenterWS ("Clip Box Center WS", Vector) = (0, 0, 0, 0)
        _ClipBoxSizeWS ("Clip Box Size WS", Vector) = (1, 1, 1, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM

            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);
            TEXTURE2D(_SpecGlossMap);
            SAMPLER(sampler_SpecGlossMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _BaseColor;
                half4 _SpecColor;
                half _BumpScale;
                half _Smoothness;
                float4 _UvTiling;
                float4 _UvOffset;
                float _ClipBoxEnabled;
                float4 _ClipBoxCenterWS;
                float4 _ClipBoxSizeWS;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half3 tangentWS : TEXCOORD2;
                half3 bitangentWS : TEXCOORD3;
                float2 uv : TEXCOORD4;
                float4 shadowCoord : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            half3 ApplyLight(Light light, half3 albedo, half3 normalWS, half3 viewDirWS, half3 specularColor, half smoothness)
            {
                half lightAmount = light.distanceAttenuation * light.shadowAttenuation;
                half diffuseAmount = saturate(dot(normalWS, light.direction)) * lightAmount;

                half3 halfDir = normalize(light.direction + viewDirWS);
                half specPower = exp2(7 + smoothness * 8);
                half specularAmount = pow(saturate(dot(normalWS, halfDir)), specPower) * smoothness * lightAmount;

                return (albedo * diffuseAmount + specularColor * specularAmount) * light.color;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.tangentWS = normalInputs.tangentWS;
                output.bitangentWS = normalInputs.bitangentWS;
                output.uv = TRANSFORM_TEX(input.uv * _UvTiling.xy + _UvOffset.xy, _MainTex);
                output.shadowCoord = GetShadowCoord(positionInputs);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                if (_ClipBoxEnabled > 0.5)
                {
                    float3 halfSize = max(abs(_ClipBoxSizeWS.xyz) * 0.5, float3(0.0005, 0.0005, 0.0005));
                    float3 boxDistance = abs(input.positionWS - _ClipBoxCenterWS.xyz) - halfSize;
                    clip(max(max(boxDistance.x, boxDistance.y), boxDistance.z));
                }

                half4 baseSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half3 albedo = baseSample.rgb * _BaseColor.rgb;

                half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv), _BumpScale);
                half3 normalWS = TransformTangentToWorld(
                    normalTS,
                    half3x3(normalize(input.tangentWS), normalize(input.bitangentWS), normalize(input.normalWS)));
                normalWS = normalize(normalWS);

                half3 viewDirWS = normalize(GetWorldSpaceViewDir(input.positionWS));
                half3 specSample = SAMPLE_TEXTURE2D(_SpecGlossMap, sampler_SpecGlossMap, input.uv).rgb;
                half3 specularColor = _SpecColor.rgb * specSample;

                half3 color = albedo * SampleSH(normalWS);
                Light mainLight = GetMainLight(input.shadowCoord);
                color += ApplyLight(mainLight, albedo, normalWS, viewDirWS, specularColor, _Smoothness);

            #if defined(_ADDITIONAL_LIGHTS)
                uint pixelLightCount = GetAdditionalLightsCount();
                for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
                {
                    Light light = GetAdditionalLight(lightIndex, input.positionWS);
                    color += ApplyLight(light, albedo, normalWS, viewDirWS, specularColor, _Smoothness);
                }
            #endif

                return half4(color, baseSample.a * _BaseColor.a);
            }

            ENDHLSL
        }
    }

    FallBack Off
}
