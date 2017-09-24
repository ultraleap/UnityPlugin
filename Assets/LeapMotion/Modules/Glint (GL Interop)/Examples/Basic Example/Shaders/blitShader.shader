Shader "LeapMotion/Glint/Examples/blitShader" {
	Properties {
    _MainTex("Texture", 2D) = "white" {}
  }
	SubShader {
		Tags { "RenderType"="Opaque" }

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f {
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};
			
			v2f vert (appdata v) {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target {
				// start with white
        fixed4 col = 1;

        // add some sine-on-time bullshit
        float tC = 0.2;
        col.r *= (0.8 * sin((_Time.x * 100 * tC + i.uv.x * 10) * 4) + 0.7);
        col.g *= (0.6 * sin((_Time.x * 98  * tC + i.uv.x * 8 ) * 2) + 0.7);
        col.b *= (0.8 * sin((_Time.x * 77  * tC + i.uv.x * 5 ) * 2) + 0.7);

        col.r *= (0.8 * sin((_Time.x * 89  * tC + i.uv.y * 10) * 4) + 0.7);
        col.g *= (0.6 * sin((_Time.x * 99  * tC + i.uv.y * 8 ) * 2) + 0.7);
        col.b *= (0.8 * sin((_Time.x * 64  * tC + i.uv.y * 5 ) * 2) + 0.7);

				return col;
			}
			ENDCG
		}
	}
}
