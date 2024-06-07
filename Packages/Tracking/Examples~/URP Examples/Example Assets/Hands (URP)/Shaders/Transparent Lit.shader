Shader "Ultraleap/URP/Transparent Lit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color("Base Color", color) = (1,1,1,1)

        [Space(20)]
        _Smoothness("Smoothness", Range(0,1)) = 0
        _Metallic("Metallic", Range(0,1)) = 0

        [Space(20)]
        [MaterialToggle] _useFresnel("Use Fresnel", Float) = 0
        _FresnelColor("Fresnel Color", Color) = (1,1,1,1)
        _FresnelIntensity("Fresnel Intensity", Range(0, 10)) = 0
    }
    SubShader
    {
        Tags
        { 
            "RenderPipeline" = "UniversalPipeline" 
            "IgnoreProjector" = "True" 
            "Queue" = "Transparent" 
            "RenderType" = "Transparent"
        }

        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha

        // This cuts out transparency overlapping from this object
        Pass
        {
            ZWrite On
            Blend Zero One
            Cull Back
        }

        Pass
        {
            Tags
            { 
              "Lightmode" = "UniversalForward" 
            }

            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"            

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 normal : NORMAL;
                float4 texcoord1 : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 viewDir : TEXCOORD3;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 4);
                UNITY_VERTEX_OUTPUT_STEREO
            };
            CBUFFER_START(UnityPerMaterial)
            sampler2D _MainTex;
            float4 _MainTex_ST;

            float4 _Color;
            float _Smoothness, _Metallic;

            float _useFresnel;
            float4 _FresnelColor;
            float _FresnelIntensity;
            CBUFFER_END

            float Unity_FresnelEffect_float(float3 Normal, float3 ViewDir, float Power)
            {
                return pow((1.0 - saturate(dot(normalize(Normal), normalize(ViewDir)))), Power);
            }

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.positionWS = TransformObjectToWorld(v.vertex.xyz);
                o.normalWS = TransformObjectToWorldNormal(v.normal.xyz);
                o.viewDir = normalize(_WorldSpaceCameraPos - o.positionWS);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.vertex = TransformWorldToHClip(o.positionWS);

                OUTPUT_LIGHTMAP_UV( v.texcoord1, unity_LightmapST, o.lightmapUV );
                OUTPUT_SH(o.normalWS.xyz, o.vertexSH );

                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half4 mainTex = tex2D(_MainTex, i.uv);
                InputData inputdata = (InputData)0;
                inputdata.positionWS = i.positionWS;
                inputdata.normalWS = normalize(i.normalWS);
                inputdata.viewDirectionWS = i.viewDir;
                inputdata.bakedGI = SAMPLE_GI( i.lightmapUV, i.vertexSH, inputdata.normalWS );

                SurfaceData surfacedata;
                surfacedata.albedo = mainTex * _Color;
                surfacedata.specular = 0;
                surfacedata.metallic = _Metallic;
                surfacedata.smoothness = _Smoothness;
                surfacedata.normalTS = 0;
                surfacedata.emission = 0;
                surfacedata.occlusion = 1;
                surfacedata.alpha = mainTex.a * _Color.a;
                surfacedata.clearCoatMask = 0;
                surfacedata.clearCoatSmoothness = 0;

                if (_useFresnel)
                {
                    float3 fresnelColor = _FresnelColor * 
                          Unity_FresnelEffect_float(i.normalWS, i.viewDir, _FresnelIntensity) * _FresnelColor.a;
                    surfacedata.emission = fresnelColor;
                }

                float4 col = UniversalFragmentPBR(inputdata, surfacedata);
                return col;
            }
            ENDHLSL
        }
    }
}