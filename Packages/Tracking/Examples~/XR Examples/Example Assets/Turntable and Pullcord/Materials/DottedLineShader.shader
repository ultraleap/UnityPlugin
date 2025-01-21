Shader "Ultraleap/DottedLineShader"
{
    Properties
    {
        _RepeatCount("Repeat Count", float) = 5
        _Spacing("Spacing", float) = 0.5
        _Offset("Offset", float) = 0
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Tags
            {
                "LightMode" = "SRPDefaultUnlit"
            }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #pragma multi_compile_instancing

            #pragma vertex Vert
            #pragma fragment Frag

            CBUFFER_START(UnityPerMaterial)
                float _RepeatCount;
                float _Spacing;
                float _Offset;
            CBUFFER_END

            struct Attributes
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                half4 color : COLOR0;

                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = TransformObjectToHClip(v.vertex);
                o.uv = v.uv;
                o.uv.x = (o.uv.x + _Offset) * _RepeatCount * (1.0f + _Spacing);
                o.color = v.color;

                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                i.uv.x = fmod(i.uv.x, 1.0f + _Spacing);
                float r = length(i.uv - float2(1.0f + _Spacing, 1.0f) * 0.5f) * 2.0f;

                half4 color = i.color;
                color.a *= saturate((0.99f - r) * 100.0f);

                return color;
            }
            ENDHLSL
        }
    }
}