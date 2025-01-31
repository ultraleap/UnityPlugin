Shader "Ultraleap/Simple Outline"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Outline("Outline width", Range(0,0.2)) = 0.005
    }

    CGINCLUDE
    
    #include "UnityCG.cginc"   // for & UNITY_VERTEX_OUTPUT_STEREO UnityObjectToWorldNormal() 

    fixed4 _Color;
    float _Outline;

    struct v2f
    {
        float4 pos : POSITION;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    ENDCG

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent" "RenderType" = "Transparent"
        }

        ZWrite On
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Cull Back
            Blend Zero One
        }

        Pass
        {
            Cull Front

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            v2f vert(appdata_base v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.pos = UnityObjectToClipPos(v.vertex);
                float3 norm = mul((float3x3)UNITY_MATRIX_IT_MV, v.normal);
                float2 offset = TransformViewToProjection(norm.xy);
                o.pos.xy = o.pos.xy + offset * _Outline;
                return o;
            }

            half4 frag(v2f i) :COLOR
            {
                return _Color;
            }
            ENDCG
        }
    }
}
