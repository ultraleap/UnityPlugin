Shader "Ultraleap/Simple Outline"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Outline("Outline width", Range(0,0.2)) = 0.005
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        ZWrite On
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Tags
            {
                "LightMode" = "SRPDefaultUnlit"
            }

            Cull Back
            ColorMask 0
        }

        Pass
        {
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Cull Front

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #pragma multi_compile_instancing

            #pragma vertex Vert
            #pragma fragment Frag

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Outline;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 normalOS : NORMAL;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;

                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.positionCS = TransformObjectToHClip(v.positionOS + v.normalOS* _Outline);
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i)

                return _Color;
            }
            ENDHLSL
        }
    }
}