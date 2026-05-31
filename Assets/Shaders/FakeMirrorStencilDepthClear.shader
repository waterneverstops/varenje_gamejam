Shader "Hidden/Fake Mirror/Stencil Depth Clear"
{
    Properties
    {
        [IntRange] _StencilRef ("Stencil Ref", Range(0, 255)) = 64
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

        Pass
        {
            Name "StencilDepthClear"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Stencil
            {
                Ref [_StencilRef]
                Comp Equal
                Pass Keep
            }

            ColorMask 0
            Cull Off
            ZWrite On
            ZTest Always

            HLSLPROGRAM

            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _StencilRef;
            CBUFFER_END

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID, UNITY_RAW_FAR_CLIP_VALUE);
                return output;
            }

            float Frag(Varyings input) : SV_Depth
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return UNITY_RAW_FAR_CLIP_VALUE;
            }

            ENDHLSL
        }
    }

    FallBack Off
}
