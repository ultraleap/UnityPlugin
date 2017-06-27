// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "LeapMotion/Examples/Skybox/Starfield" {
	Properties { }
	SubShader {
		Tags { "RenderType" = "Skybox" "Queue" = "Background" }

		Pass {
			ZWrite Off
			Cull Off
			Fog { Mode Off }

			CGPROGRAM
        #pragma target 2.0
			  #pragma fragmentoption ARB_precision_hint_fastest
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
		      fragData.position = UnityObjectToClipPos(v.position);
          fragData.worldPos = v.position;
		      return fragData;
	      }

        fixed3 hsv2rgb(fixed3 c) {
          fixed4 K = fixed4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
          fixed3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
          return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
        }

        half4 starfield(fixed3 lookDir) {
		      fixed3 v = lookDir;
          fixed freq = 2.3;
          fixed amp = 0.2;
          fixed t = 39; // this can be anything; can even change continuously!
          
          fixed noiseV = cnoise(fixed4(v.x * freq, v.y * freq, v.z * freq, t));
          
          //fixed4 starColor = fixed4(1, 1, 1, 1);
          fixed colorFreq = 40;
          fixed colorT = 39;
          colorT = _Time.x;
          fixed colorV = cnoise(fixed4(v.x * colorFreq, v.y * colorFreq, v.z * colorFreq, colorT));
          fixed3 rgb = hsv2rgb(fixed3(colorV, lerp(0.8, 0.0, noiseV), 0.9));
          fixed4 starColor = fixed4(rgb.x, rgb.y, rgb.z, 1);

          v = v + v * amp * noiseV;

          fixed base_frequency = 200;
          fixed3 base_star_in = v * base_frequency;
          fixed base_star_out = cnoise(fixed4(base_star_in.x, base_star_in.y, base_star_in.z, _Time.x * 0.1));
          fixed base_starlight = base_star_out * base_star_out * base_star_out * base_star_out;
          base_starlight = base_starlight * base_starlight;
          
          fixed theta = acos(v.z/1);
          fixed phi   = atan(v.y/v.x);
          fixed polar_frequency = 5;
          fixed polar_star_theta = theta * polar_frequency;
          fixed polar_star_phi   = phi * polar_frequency;
          fixed polar_star_out = cnoise(fixed4(polar_star_theta, polar_star_phi, 0, 0));
          fixed polar_starlight = polar_star_out * polar_star_out * polar_star_out * polar_star_out;

          return starColor * base_starlight;
        }

	      half4 frag(v2f fragData) : COLOR {
          return starfield(normalize(fragData.worldPos));
	      }
			ENDCG

		}

	}
}