Shader "Custom/Decal/Hidden Mark Darkness Red"
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
        _DrawOrder ("Draw Order", Int) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "IgnoreProjector" = "True"
        }

        HLSLINCLUDE

        #pragma target 3.0
        #pragma multi_compile_instancing

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DecalInput.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderVariablesDecal.hlsl"

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
            int _DrawOrder;
        CBUFFER_END

        struct Attributes
        {
            float4 positionOS : POSITION;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
        };

        half Luma(half3 color)
        {
            return dot(color, half3(0.2126, 0.7152, 0.0722));
        }

        half LightPresence(Light light)
        {
            return Luma(light.color) * light.distanceAttenuation * light.shadowAttenuation;
        }

        Varyings DecalVert(Attributes input)
        {
            Varyings output = (Varyings)0;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_TRANSFER_INSTANCE_ID(input, output);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
            return output;
        }

        float LoadDecalDepth(float2 positionCS)
        {
        #if UNITY_REVERSED_Z
            return LoadSceneDepth(uint2(positionCS));
        #else
            return lerp(UNITY_NEAR_CLIP_VALUE, 1.0, LoadSceneDepth(uint2(positionCS)));
        #endif
        }

        void BuildDecalProjection(Varyings input, out float3 positionWS, out half3 decalNormalWS, out float2 uv, out half fadeFactor)
        {
            float2 positionSS = FoveatedRemapNonUniformToLinearCS(input.positionCS.xy) * _ScreenSize.zw;
            float depth = LoadDecalDepth(input.positionCS.xy);
            positionWS = ComputeWorldSpacePosition(positionSS, depth, UNITY_MATRIX_I_VP);

            float3 positionDS = TransformWorldToObject(positionWS) * float3(1.0, -1.0, 1.0);
            clip(0.5 - max(max(abs(positionDS.x), abs(positionDS.y)), abs(positionDS.z)));

            half4x4 normalToWorld = UNITY_ACCESS_INSTANCED_PROP(Decal, _NormalToWorld);
            fadeFactor = saturate(normalToWorld[0][3]);

            float2 decalScale = float2(normalToWorld[3][0], normalToWorld[3][1]);
            float2 decalOffset = float2(normalToWorld[3][2], normalToWorld[3][3]);
            uv = TRANSFORM_TEX(positionDS.xz + float2(0.5, 0.5), _MainTex);
            uv = uv * decalScale + decalOffset;

            decalNormalWS = normalize(half3(normalToWorld[2].xyz));
        }

        half4 SampleHiddenMark(float2 uv, half fadeFactor, out half alpha)
        {
            half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
            alpha = tex.a * _BaseColor.a * fadeFactor;
            return half4(tex.rgb * _BaseColor.rgb, alpha);
        }

        half SampleDarkAlpha(float2 uv, half fadeFactor)
        {
            half textureAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
            return textureAlpha * _DarkColor.a * fadeFactor;
        }

        InputData BuildInputData(Varyings input, float3 positionWS, half3 normalWS)
        {
            InputData inputData = (InputData)0;
            inputData.positionWS = positionWS;
            inputData.normalWS = normalWS;
            inputData.viewDirectionWS = SafeNormalize(GetWorldSpaceViewDir(positionWS));
            inputData.shadowCoord = TransformWorldToShadowCoord(positionWS);
            inputData.fogCoord = ComputeFogFactor(input.positionCS.z);
            inputData.vertexLighting = half3(0, 0, 0);
            inputData.bakedGI = SampleSH(normalWS);
            inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
            inputData.shadowMask = half4(1, 1, 1, 1);
            return inputData;
        }

        half CalculateDarkness(InputData inputData)
        {
            half lightAmount = 0;
            AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData.normalizedScreenSpaceUV, half(1));

            Light mainLight = GetMainLight(inputData, inputData.shadowMask, aoFactor);
            lightAmount += LightPresence(mainLight);

            uint pixelLightCount = GetAdditionalLightsCount();
        #if USE_CLUSTER_LIGHT_LOOP
            LIGHT_LOOP_BEGIN(pixelLightCount)
                Light light = GetAdditionalLight(lightIndex, inputData, inputData.shadowMask, aoFactor);
                lightAmount += LightPresence(light);
            LIGHT_LOOP_END
        #else
            for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
            {
                Light light = GetAdditionalLight(lightIndex, inputData, inputData.shadowMask, aoFactor);
                lightAmount += LightPresence(light);
            }
        #endif

            half ambientAmount = Luma(inputData.bakedGI) * _AmbientInfluence;
            half darknessLight = (lightAmount + ambientAmount) * _LightSensitivity;
            half darkness = half(1.0) - smoothstep(_DarkThreshold, _DarkThreshold + max(_DarkSoftness, half(0.001)), darknessLight);
            return saturate(darkness * _DarkStrength);
        }

        ENDHLSL

        Pass
        {
            Name "DBufferProjector"
            Tags { "LightMode" = "DBufferProjector" }

            Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
            Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
            Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
            Cull Front
            ZTest Greater
            ZWrite Off

            HLSLPROGRAM

            #pragma vertex DecalVert
            #pragma fragment DecalDBufferFrag
            #pragma multi_compile_fragment _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"

            void DecalDBufferFrag(Varyings input, OUTPUT_DBUFFER(outDBuffer))
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 positionWS;
                half3 decalNormalWS;
                float2 uv;
                half fadeFactor;
                BuildDecalProjection(input, positionWS, decalNormalWS, uv, fadeFactor);

                half alpha;
                half4 decalColor = SampleHiddenMark(uv, fadeFactor, alpha);

                DecalSurfaceData surfaceData = (DecalSurfaceData)0;
                surfaceData.baseColor = half4(decalColor.rgb, alpha);
                surfaceData.normalWS = half4(decalNormalWS, 0);
                surfaceData.emissive = 0;
                surfaceData.metallic = 0;
                surfaceData.occlusion = 1;
                surfaceData.smoothness = 0;
                surfaceData.MAOSAlpha = alpha;

                ENCODE_INTO_DBUFFER(surfaceData, outDBuffer);
            }

            ENDHLSL
        }

        Pass
        {
            Name "DecalProjectorForwardEmissive"
            Tags { "LightMode" = "DecalProjectorForwardEmissive" }

            Blend 0 SrcAlpha One
            Cull Front
            ZTest Greater
            ZWrite Off

            HLSLPROGRAM

            #pragma vertex DecalVert
            #pragma fragment DecalForwardEmissiveFrag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP

            half4 DecalForwardEmissiveFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 positionWS;
                half3 decalNormalWS;
                float2 uv;
                half fadeFactor;
                BuildDecalProjection(input, positionWS, decalNormalWS, uv, fadeFactor);

                half alpha = SampleDarkAlpha(uv, fadeFactor);

                InputData inputData = BuildInputData(input, positionWS, decalNormalWS);
                half darkness = CalculateDarkness(inputData);
                half3 darkColor = _DarkColor.rgb * _DarkBrightness * darkness;

                return half4(darkColor * GetCurrentExposureMultiplier(), alpha * darkness);
            }

            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
