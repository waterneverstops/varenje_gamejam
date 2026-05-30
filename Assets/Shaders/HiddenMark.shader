Shader "Custom/3D/Hidden Mark Darkness Red"
{
    Properties
    {
        [MainTexture] _MainTex ("Texture", 2D) = "white" {}
        [MainColor] _BaseColor ("Base Color", Color) = (1, 1, 1, 1)

        [HDR] _DarkColor ("Dark Color", Color) = (1, 0, 0, 1)
        _DarkBrightness ("Dark Brightness", Range(0, 10)) = 1.5
        _DarkThreshold ("Dark Threshold", Range(0, 5)) = 0.15
        _DarkSoftness ("Dark Softness", Range(0.001, 5)) = 0.2
        _DarkStrength ("Dark Strength", Range(0, 1)) = 1
        _LightSensitivity ("Light Sensitivity", Range(0, 20)) = 1

        _AmbientInfluence ("Ambient Influence", Range(0, 1)) = 0.15
        _MinVisibleLight ("Minimum Visible Light", Range(0, 1)) = 0.03
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "UniversalMaterialType" = "SimpleLit"
            "IgnoreProjector" = "True"
        }

        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForwardOnly" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM

            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _BaseColor;
                half4 _DarkColor;
                half _DarkBrightness;
                half _DarkThreshold;
                half _DarkSoftness;
                half _DarkStrength;
                half _LightSensitivity;
                half _AmbientInfluence;
                half _MinVisibleLight;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
                half fogFactor : TEXCOORD4;
                half3 vertexLighting : TEXCOORD5;
                float2 screenUV : TEXCOORD6;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            half Luma(half3 color)
            {
                return dot(color, half3(0.2126, 0.7152, 0.0722));
            }

            half3 Lambert(Light light, half3 normalWS)
            {
                half ndotl = saturate(dot(normalWS, light.direction));
                return light.color * (light.distanceAttenuation * light.shadowAttenuation * ndotl);
            }

            half LightPresence(Light light)
            {
                return Luma(light.color) * light.distanceAttenuation * light.shadowAttenuation;
            }

            InputData BuildInputData(Varyings input, half3 normalWS)
            {
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));
                inputData.shadowCoord = input.shadowCoord;
                inputData.fogCoord = input.fogFactor;
                inputData.vertexLighting = input.vertexLighting;
                inputData.bakedGI = SampleSH(normalWS);
                inputData.normalizedScreenSpaceUV = input.screenUV;
                inputData.shadowMask = half4(1, 1, 1, 1);
                return inputData;
            }

            void AddLightContribution(
                Light light,
                half3 normalWS,
                inout half3 diffuseLighting,
                inout half lightAmount)
            {
                diffuseLighting += Lambert(light, normalWS);
                lightAmount += LightPresence(light);
            }

            void CalculateLightingForDarkness(InputData inputData, out half3 diffuseLighting, out half lightAmount)
            {
                diffuseLighting = half3(0, 0, 0);
                lightAmount = 0;

                AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData.normalizedScreenSpaceUV, half(1));
                Light mainLight = GetMainLight(inputData, inputData.shadowMask, aoFactor);
                AddLightContribution(mainLight, inputData.normalWS, diffuseLighting, lightAmount);

                uint pixelLightCount = GetAdditionalLightsCount();

                #if USE_CLUSTER_LIGHT_LOOP
                    LIGHT_LOOP_BEGIN(pixelLightCount)
                        Light light = GetAdditionalLight(lightIndex, inputData, inputData.shadowMask, aoFactor);
                        AddLightContribution(light, inputData.normalWS, diffuseLighting, lightAmount);
                    LIGHT_LOOP_END
                #else
                    for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
                    {
                        Light light = GetAdditionalLight(lightIndex, inputData, inputData.shadowMask, aoFactor);
                        AddLightContribution(light, inputData.normalWS, diffuseLighting, lightAmount);
                    }
                #endif
            }

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = NormalizeNormalPerVertex(normalInputs.normalWS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.shadowCoord = GetShadowCoord(positionInputs);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                output.screenUV = GetNormalizedScreenSpaceUV(output.positionCS);

                #if defined(_ADDITIONAL_LIGHTS_VERTEX)
                    output.vertexLighting = VertexLighting(output.positionWS, output.normalWS);
                #else
                    output.vertexLighting = half3(0, 0, 0);
                #endif

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half3 normalWS = NormalizeNormalPerPixel(input.normalWS);
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half alpha = tex.a * _BaseColor.a;
                half3 albedo = tex.rgb * _BaseColor.rgb;

                InputData inputData = BuildInputData(input, normalWS);

                half3 diffuseLighting;
                half lightAmount;
                CalculateLightingForDarkness(inputData, diffuseLighting, lightAmount);

                half ambientAmount = Luma(inputData.bakedGI) * _AmbientInfluence;
                half darknessLight = (lightAmount + ambientAmount) * _LightSensitivity;
                half darkness = half(1.0) - smoothstep(_DarkThreshold, _DarkThreshold + max(_DarkSoftness, half(0.001)), darknessLight);
                darkness = saturate(darkness * _DarkStrength);

                half3 visibleLighting = max(diffuseLighting + inputData.bakedGI, half3(_MinVisibleLight, _MinVisibleLight, _MinVisibleLight));
                half3 litColor = albedo * visibleLighting;
                half3 darkColor = _DarkColor.rgb * _DarkBrightness;
                half3 finalColor = lerp(litColor, darkColor, darkness);

                finalColor = MixFog(finalColor, inputData.fogCoord);
                return half4(finalColor, alpha);
            }

            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM

            #pragma target 2.0
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            ShadowVaryings ShadowPassVertex(ShadowAttributes input)
            {
                ShadowVaryings output = (ShadowVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                output.positionCS = ApplyShadowClamping(output.positionCS);
                return output;
            }

            half4 ShadowPassFragment(ShadowVaryings input) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return 0;
            }

            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM

            #pragma target 2.0
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct DepthAttributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            DepthVaryings DepthOnlyVertex(DepthAttributes input)
            {
                DepthVaryings output = (DepthVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 DepthOnlyFragment(DepthVaryings input) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return 0;
            }

            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
