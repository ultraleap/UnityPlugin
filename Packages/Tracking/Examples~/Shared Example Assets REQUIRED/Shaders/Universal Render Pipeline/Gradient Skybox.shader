Shader "Ultraleap/Universal Render Pipeline/Gradient Skybox"
{
    Properties
    {
        _Top("Top", Color) = (1,1,1,0)
        _Bottom("Bottom", Color) = (0,0,0,0)
        _mult("Multiply", Float) = 1
        _pwer("Power", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }

        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #pragma multi_compile_instancing

            #pragma vertex Vert
            #pragma fragment Frag

            CBUFFER_START(UnityPerMaterial)
                uniform float4 _Bottom;
                uniform float4 _Top;
                uniform float _mult;
                uniform float _pwer;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 vertex : SV_POSITION;
                float4 positionOS : TEXCOORD1;

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.positionOS = v.positionOS;
                o.vertex = TransformObjectToHClip(v.positionOS);
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                return lerp(_Bottom, _Top, pow(saturate(i.positionOS.y * _mult), _pwer));
            }
            ENDHLSL
        }
    }
}