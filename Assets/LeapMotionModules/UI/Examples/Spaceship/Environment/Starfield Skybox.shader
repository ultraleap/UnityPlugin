// http://answers.unity3d.com/answers/924794/view.html
Shader "LeapMotion/Skybox/Starfield" {
	Properties {
    
	}
	SubShader {
		Tags { "RenderType" = "Skybox" "Queue" = "Background" }
		Pass {
			ZWrite Off
			Cull Off
			Fog { Mode Off }

			CGPROGRAM

        #pragma target 2.0
			  //#pragma fragmentoption ARB_precision_hint_fastest
			  #pragma vertex vert
			  #pragma fragment frag

        #include "UnityCG.cginc"
        #include "classicnoise4d.cginc"

		    struct appdata {
		      float4 position : POSITION;
		      float3 texcoord : TEXCOORD0;
	      };

	      struct v2f {
		      float4 position : SV_POSITION;
		      float3 worldPos : TEXCOORD0;
	      };

	      v2f vert(appdata v) {
		      v2f fragData;
		      fragData.position = mul(UNITY_MATRIX_MVP, v.position);
          fragData.worldPos = v.position;
		      return fragData;
	      }

	      half4 frag(v2f fragData) : COLOR {
		      float3 v = normalize(fragData.worldPos);
          float freq = 2.3;
          float amp = 0.2;
          v = v + v * amp * cnoise(float4(v.x * freq, v.y * freq, v.z * freq, 39));

          float base_frequency = 200;
          float3 base_star_in = v * base_frequency;
          half base_star_out = cnoise(float4(base_star_in.x, base_star_in.y, base_star_in.z, _Time.x * 0.1));
          half base_starlight = base_star_out * base_star_out * base_star_out * base_star_out;
          base_starlight = base_starlight * base_starlight;
          
          float theta = acos(v.z/1);
          float phi   = atan(v.y/v.x);
          float polar_frequency = 5;
          float polar_star_theta = theta * polar_frequency;
          float polar_star_phi   = phi * polar_frequency;
          half polar_star_out = cnoise(float4(polar_star_theta, polar_star_phi, 0, 0));
          half polar_starlight = polar_star_out * polar_star_out * polar_star_out * polar_star_out;

          half4 white = half4(1, 1, 1, 1);
          return white * base_starlight;
	      }
			ENDCG
		}
	}
}