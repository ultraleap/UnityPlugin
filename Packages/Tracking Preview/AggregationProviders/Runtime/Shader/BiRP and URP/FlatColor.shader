Shader "Ultraleap/FlatColor"
{
    Properties
    {
        _Color("Color", Color) = (1.0,1.0,1.0,1.0)
    }

    // Universal Render Pipeline
    SubShader
    {
        PackageRequirements
        {
            "com.unity.render-pipelines.universal"
        }

        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }

        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
            CBUFFER_END

            float4 Vert(float4 positionOS: POSITION) : SV_POSITION
            {
                return  TransformObjectToHClip(positionOS.xyz);
            }

            float4 Frag(float4 positionCS : SV_POSITION) : SV_Target
            {
                return _Color;
            }
            ENDHLSL
        }
    }

    // Built-in Render Pipeline
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
        }

        Pass
        {
            CGPROGRAM
            // pragmas
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"


            // base structs
            struct vertexInput
            {
                float4 vertex: POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct vertexOutput
            {
                float4 pos: SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
            UNITY_INSTANCING_BUFFER_END(Props)

            // vertex function
            vertexOutput vert(vertexInput v)
            {
                vertexOutput o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.pos = UnityObjectToClipPos(v.vertex);

                return o;
            }

            // fragment function
            float4 frag(vertexOutput i) : COLOR
            {
                UNITY_SETUP_INSTANCE_ID(i);
                return UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
            }
            ENDCG
        }
    }

}