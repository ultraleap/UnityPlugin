// Unlit alpha-blended shader.
// - no lighting
// - no lightmap support
// - no per-material color

Shader "Unlit/AlphaMask" {
	Properties{
		_AlphaVal("AlphaVal", Range(0, 1)) = 1.0
		_MainTex("MainTex (Sprite)", 2D) = "white" {}
		_AlphaTex("AlphaTex (R)", 2D) = "white" {}
	}

		SubShader{
		Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		LOD 100

		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass{
			CGPROGRAM
#pragma vertex vert
#pragma fragment frag

#include "UnityCG.cginc"

			struct appdata_t {
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
				float2 texcoordA : TEXCOORD1; // alpha uv
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				half2 texcoord : TEXCOORD0;
				half2 texcoordA : TEXCOORD1; // alpha uv
			};

			sampler2D _MainTex;
			sampler2D _AlphaTex;
			float _AlphaVal;

			float4 _MainTex_ST;
			float4 _AlphaTex_ST; // for alpha uv

			v2f vert(appdata_t v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.texcoordA = TRANSFORM_TEX(v.texcoordA, _AlphaTex); // note texcoordA
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 main = tex2D(_MainTex, i.texcoord);
				fixed4 alph = tex2D(_AlphaTex, i.texcoordA);

				return fixed4(main.r, main.g, main.b, (main.a*alph.r*_AlphaVal));
			}
				ENDCG
		}
	}

}