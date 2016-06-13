Shader "Character Indicator"
{
	Properties
	{
		_Color("Main Color", Color) = (1, 1, 1, 1)
		_MainTex("Base (RGB)", 2D) = "white" {}
		_Indicator("Indicator Color", Color) = (1, 1, 1, 1)
			_Cutoff("Alpha cutoff", Range(0, 1)) = 0.0
	}

	SubShader
		{
			Tags{ "Queue" = "Geometry+1" "RenderType" = "Opaque" }

			CGPROGRAM
#pragma surface surf BlinnPhong alphatest:_Cutoff

			uniform float4 _Color;
			uniform float4 _Indicator;
			uniform sampler2D _MainTex;

			struct Input
			{
				float2 uv_MainTex;
				float3 viewDir;
			};

			void surf(Input IN, inout SurfaceOutput o)
			{
				o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb * _Color;
			}
			ENDCG

				// Pass: Surface SHADER

				//      ZWrite Off
				//      ZTest Greater
				//      Blend DstColor Zero
				//     
				//      CGPROGRAM
				//      #pragma surface surf BlinnPhong
				//      uniform float4 _Color;
				//      uniform float4 _Indicator;
				//      uniform sampler2D _MainTex;
				//
				//      struct Input
				//      {
				//          float2 uv_MainTex;
				//          float3 viewDir;
				//      };
				//     
				//      void surf (Input IN, inout SurfaceOutput o)
				//      {
				//          //o.Albedo =_Indicator;
				//          o.Albedo = tex2D ( _MainTex, IN.uv_MainTex).rgb * _Indicator;
				//      }
				//      ENDCG  

				// Pass: CG SHADER
				//      Pass
				//        {
				//          Tags { "LightMode" = "Always" }
				//          AlphaTest Greater [_Cutoff]
				//          ZWrite Off
				//          ZTest Greater
				//         
				//            CGPROGRAM
				//            #pragma vertex vert
				//            #pragma fragment frag
				//            #pragma fragmentoption ARB_precision_hint_fastest
				//            #include "UnityCG.cginc"
				//
				//          sampler2D _MainTex;
				//          float4 _MainTex_ST;
				//          uniform float4 _Indicator;
				//         
				//            struct v2f
				//            {
				//                float4 pos          : POSITION;
				//                float2 uv           : TEXCOORD1;
				//            };
				//
				//            v2f vert (appdata_full v)
				//            {
				//                v2f o;
				//                o.pos = mul(UNITY_MATRIX_MVP, v.vertex);    
				//                o.uv = TRANSFORM_TEX (v.texcoord, _MainTex);
				//                return o;
				//            }
				//            
				//            half4 frag( v2f i ) : COLOR
				//            {
				//              half4 texcol = tex2D (_MainTex, i.uv);
				//                  return texcol * _Indicator;
				//            }
				//              ENDCG          
				//        }

				// Pass: Fixed pipeline
				//        Pass
				//      {          
				//          Tags { "LightMode" = "Always" }
				//          AlphaTest Greater [_Cutoff]
				//          ZWrite Off
				//          ZTest Greater
				// 
				//          SetTexture [_MainTex]
				//          {
				//              constantColor [_Indicator]
				//              //combine constant, texture
				//              combine constant* texture
				//          }
				//      }

		}



		Fallback " Glossy", 0

}