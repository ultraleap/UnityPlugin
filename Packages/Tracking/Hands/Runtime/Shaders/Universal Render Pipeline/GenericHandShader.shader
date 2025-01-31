Shader "Ultraleap/Universal Render Pipeline/GenericHandShader"
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
    ENDHLSL

    SubShader
    {
        PackageRequirements
+       {
+           "com.unity.render-pipelines.universal"
+       }

        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

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

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_Position;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirectionWS : TEXCOORD2;

                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes i)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.uv = i.uv;
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.viewDirectionWS = normalize(GetWorldSpaceViewDir(TransformObjectToWorld(i.positionOS.xyz)));
                return o;
            }

            float4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                half4 color = _Color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);

                // Use alpha pre-multiplication
                color.rgb *= color.a;

                #if _USELIGHTING_ON
                Light mainLight = GetMainLight();
                half3 lambertTerm = LightingLambert(mainLight.color, mainLight.direction, i.normalWS);
                color.rgb *= lambertTerm.rgb * _LightIntensity;
                #endif

                #if _USEFRESNEL_ON
                float fresnel = pow(1.0 - saturate(dot(i.normalWS, i.viewDirectionWS)), _FresnelPower);
                color.rgb += _FresnelColor.rgb * fresnel * _FresnelColor.a;
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

            #if _USEOUTLINE_ON
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;

                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes i)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.positionCS = TransformObjectToHClip(i.positionOS.xyz + normalize(i.normalOS) * _Outline);
                o.uv = i.uv;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                // Fade the outline to match the rest of the hand by sampling the hand texture.
                half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).a;
                return half4(_OutlineColor.xyz, alpha);
            }
            #else

            float4 Vert(float4 positionOS : POSITION) : SV_POSITION
            {
                return positionOS;
            }

            half4 Frag() : SV_Target
            {
                return 0;
            }

            #endif
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

    Fallback "Ultraleap/LegacyHandShader"
}