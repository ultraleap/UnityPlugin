Shader "Ultraleap/GenericHandShader"
{
    Properties
    {
        [NoScaleOffset] _MainTex("Texture", 2D) = "white" {}
        [HDR]_Color("Main Color", Color) = (0,0,0,1)

        [MaterialToggle] _useOutline("Use Outline", Float) = 0
        [HDR]_OutlineColor("Outline Color", Color) = (0,0,0,1)
        _Outline("Outline Width", Range(0, 0.2)) = 0.01

        [MaterialToggle] _useLighting("Use Lighting", Float) = 0
        _LightIntensity("Light Intensity", Range(0,1)) = 1

        [MaterialToggle] _useFresnel("Use Fresnel", Float) = 0
        [HDR]_FresnelColor("Fresnel Color", Color) = (1,1,1,0)
        _FresnelPower("Fresnel Power", Range(0,5)) = 1
    }

    // Universal Render-Pipeline
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

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            float _LightIntensity;
            float _useLighting;
            float _useFresnel;
            float4 _FresnelColor;
            float _FresnelPower;
            float _useOutline;
            float _Outline;
            float4 _OutlineColor;
        CBUFFER_END

        struct MyAttributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float2 uv : TEXCOORD0;

            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct MyVaryings
        {
            float4 positionCS : SV_Position;
            float2 uv : TEXCOORD0;
            float3 normalWS : TEXCOORD1;
            float3 viewDirectionWS : TEXCOORD2;
            half3 vertexSH : COLOR0;

            UNITY_VERTEX_OUTPUT_STEREO
        };

        ENDHLSL

        ZWrite On
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "DepthPrePass"
            Tags
            {
                "LightMode" = "SRPDefaultUnlit"
            }

            Cull Off
            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma multi_compile_instancing
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "MainPass"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma multi_compile_instancing
            #pragma shader_feature _USELIGHTING_ON
            #pragma shader_feature _USEFRESNEL_ON

            #pragma vertex Vert
            #pragma fragment Frag

            // This is the fresnel function that ShaderGraph uses.
            float Unity_FresnelEffect_float(float3 Normal, float3 ViewDir, float Power)
            {
                return pow((1.0 - saturate(dot(normalize(Normal), normalize(ViewDir)))), Power);
            }

            MyVaryings Vert(MyAttributes i)
            {
                MyVaryings o = (MyVaryings)0;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.uv = i.uv;

                #if _USELIGHTING_ON || _USEFRESNEL_ON
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                #endif

                #if _USEFRESNEL_ON
                o.viewDirectionWS = normalize(GetWorldSpaceViewDir(TransformObjectToWorld(i.positionOS.xyz)));
                #endif

                #if _USELIGHTING_ON
                o.vertexSH = SampleSHVertex(o.normalWS);
                #endif

                return o;
            }

            half4 Frag(MyVaryings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float4 color = _Color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);

                #if _USELIGHTING_ON || _USEFRESNEL_ON
                float3 normalWS = normalize(i.normalWS);
                #endif

                #if _USELIGHTING_ON
                Light mainLight = GetMainLight();
                half3 lambertian = LightingLambert(mainLight.color, mainLight.direction, normalWS);
                half3 ambient = SampleSHPixel(i.vertexSH, normalWS);
                color.rgb *= saturate(lambertian + ambient) * _LightIntensity;
                #endif

                #if _USEFRESNEL_ON
                float fresnel = Unity_FresnelEffect_float(normalWS, i.viewDirectionWS, _FresnelPower);
                color.rgb *= _FresnelColor.rgb * fresnel * _FresnelColor.a;
                #endif

                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "OutlinePass"
            Tags
            {
                "LightMode" = "UniversalForwardOnly"
            }

            Cull Front
            ZWrite Off

            HLSLPROGRAM
            #pragma multi_compile_instancing
            #pragma shader_feature _USEOUTLINE_ON

            #pragma vertex Vert
            #pragma fragment Frag

            MyVaryings Vert(MyAttributes i)
            {
                MyVaryings o = (MyVaryings)0;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                #if _USEOUTLINE_ON
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz + normalize(i.normalOS) * _Outline);
                #else
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                #endif
                o.uv = i.uv;
                return o;
            }

            half4 Frag(MyVaryings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                #if _USEOUTLINE_ON
                // Fade the outline to match the rest of the hand by sampling the hand texture.
                half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).a;
                return half4(_OutlineColor.xyz, alpha);
                #else
                return 0;
                #endif
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }

            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma multi_compile_instancing
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma multi_compile_instancing
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }

    // Built-in Render Pipeline
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "LightMode" = "ForwardBase"
        }

        ZWrite On
        Blend SrcAlpha OneMinusSrcAlpha

        CGINCLUDE
        #include "UnityCG.cginc"   // for & UNITY_VERTEX_OUTPUT_STEREO UnityObjectToWorldNormal()
        #include "AutoLight.cginc" // for UNITY_SHADOW_COORDS() & UNITY_TRANSFER_SHADOW()

        float4 _Color;
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

            #pragma shader_feature _USEOUTLINE_ON

            v2f vert(appdata_base v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;

                #if _USEOUTLINE_ON
                float3 norm = mul((float3x3)UNITY_MATRIX_IT_MV, v.normal);
                float2 offset = TransformViewToProjection(norm.xy);
                o.pos.xy += offset * _Outline;
                #endif

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
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight

            #pragma shader_feature _USELIGHTING_ON
            #pragma shader_feature _USEFRESNEL_ON

            #pragma vertex vert
            #pragma fragment frag

            #if _USELIGHTING_ON
            #include "UnityLightingCommon.cginc" // for _LightColor0
            #endif

            v2f vert(appdata_base v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                half3 worldNormal = UnityObjectToWorldNormal(v.normal);

                #if _USELIGHTING_ON
                half nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));
                o.diff = nl * _LightColor0;
                o.diff.rgb += ShadeSH9(half4(worldNormal, 1));
                #endif

                o.worldNormal = worldNormal;
                o.viewDir = WorldSpaceViewDir(v.vertex);
                UNITY_TRANSFER_SHADOW(o, o.uv);
                return o;
            }

            // This is the fresnel function that shader graph uses
            float Unity_FresnelEffect_float(float3 Normal, float3 ViewDir, float Power)
            {
                return pow((1.0 - saturate(dot(normalize(Normal), normalize(ViewDir)))), Power);
            }

            half4 frag(v2f i) : SV_Target
            {
                fixed4 col = _Color * tex2D(_MainTex, i.uv);

                #if _USELIGHTING_ON
                col.rgb *= (i.diff * _LightIntensity);
                #endif

                #if _USEFRESNEL_ON
                col.rgb *= _FresnelColor * Unity_FresnelEffect_float(i.worldNormal, i.viewDir, _FresnelPower) *
                    _FresnelColor.a;
                #endif

                return col;
            }
            ENDCG
        }

        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
}