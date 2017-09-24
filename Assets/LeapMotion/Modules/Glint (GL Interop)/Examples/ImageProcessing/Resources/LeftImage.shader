Shader "LeapMotion/Glint/Examples/LeftImage"
{
	Properties
	{
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile LEAP_FORMAT_IR LEAP_FORMAT_RGB
			#include "../../../../../Core/Resources/LeapCG.cginc"
			#include "UnityCG.cginc"

			struct appdata
			{
      	float2 uv : TEXCOORD0;
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the rectified texture
				return length(tex2D(_LeapGlobalBrightnessTexture, LeapGetRightUndistortedUV(float4(i.uv.x, i.uv.y, 0.0, 1.0))));
			}
			ENDCG
		}
	}
}
