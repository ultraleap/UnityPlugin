Shader "Ultraleap/GenericHandShader"
{
    Properties
    {
        [NoScaleOffset] _MainTex("Texture", 2D) = "white" {}
        [HDR]_MainColor("Main Color", Color) = (0,0,0,1)

        [MaterialToggle] _useOutline("Use Outline", Float) = 0
        [HDR]_OutlineColor("Outline Color", Color) = (0,0,0,1)
        _Outline("Outline width", Range(0,.2)) = .01

        [MaterialToggle] _useLighting("Use Lighting", Float) = 0
        _LightIntensity("Light Intensity", Range(0,1)) = 1

        [MaterialToggle] _useFresnel("Use Fresnel", Float) = 0
        [HDR]_FresnelColor("Fresnel Color", Color) = (1,1,1,0)
        _FresnelPower("Fresnel Power", Range(0,1)) = 1
    }

    CGINCLUDE

    #include "UnityCG.cginc"   // for & UNITY_VERTEX_OUTPUT_STEREO UnityObjectToWorldNormal() 
    #include "AutoLight.cginc" // for UNITY_SHADOW_COORDS() & UNITY_TRANSFER_SHADOW()
    
    float4 _MainColor;
    float _Outline;
    float4 _OutlineColor;
    sampler2D _MainTex;
    float4 _FresnelColor;
    float _FresnelPower;
    float _LightIntensity;
    float _useLighting;
    float _useFresnel;
    float _useOutline;

    struct v2f
    {
        float2 uv : TEXCOORD0;
        float4 pos : POSITION;
        float3 worldNormal : NORMAL;
        float3 viewDir : TEXCOORD1;
        fixed4 diff : COLOR0;

        UNITY_VERTEX_OUTPUT_STEREO
        UNITY_SHADOW_COORDS(2) // Uses TEXCOORD2
    };

    ENDCG

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "LightMode" = "ForwardBase"
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
                o.uv = v.texcoord;
                o.pos.xy = _useOutline ? o.pos.xy + offset * _Outline : o.pos.xy;
                return o;
            }

            half4 frag(v2f i) :COLOR
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _OutlineColor;
                return col;
            }
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
            #include "UnityLightingCommon.cginc" // for _LightColor0

            v2f vert(appdata_base v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                half3 worldNormal = UnityObjectToWorldNormal(v.normal);
                half nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));
                o.diff = nl * _LightColor0;
                o.diff.rgb += ShadeSH9(half4(worldNormal, 1));

                o.worldNormal = worldNormal;
                o.viewDir = WorldSpaceViewDir(v.vertex);
                UNITY_TRANSFER_SHADOW(o, o.uv);
                return o;
            }

            //This is the fresnel function that shader graph uses
            float Unity_FresnelEffect_float(float3 Normal, float3 ViewDir, float Power)
            {
                return pow((1.0 - saturate(dot(normalize(Normal), normalize(ViewDir)))), Power);
            }

            half4 frag(v2f i) :COLOR
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                col *= _MainColor;

                if (_useLighting)
                    col.rgb *= (i.diff * _LightIntensity);
                if (_useFresnel)
                    col.rgb *= _FresnelColor * Unity_FresnelEffect_float(i.worldNormal, i.viewDir, _FresnelPower) *
                        _FresnelColor.a;

                return col;
            }
            ENDCG
        }

        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }

    Fallback "Diffuse"
}