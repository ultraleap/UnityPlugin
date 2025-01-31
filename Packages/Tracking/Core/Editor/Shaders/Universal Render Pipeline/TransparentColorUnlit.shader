Shader "Ultraleap/Universal Render Pipeline/TransparentColorUnlit"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        PackageRequirements 
        {
            "com.unity.render-pipelines.universal"
        }

        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
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

            #pragma vertex vert
            #pragma fragment frag

            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            CBUFFER_END

            float4 vert(float4 positionOS : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(positionOS);
            }

            half4 frag() : SV_Target
            {
                return _Color;
            }
            ENDHLSL
        }
    }

    Fallback Off
}