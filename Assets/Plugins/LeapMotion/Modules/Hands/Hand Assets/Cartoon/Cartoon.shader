//http://wiki.unity3d.com/index.php?title=Translucent_Shader&oldid=17771

Shader "Custom/Translucent" {
	Properties{
		_MainTex("Base (RGB)", 2D) = "white" {}
		_BumpMap("Normal (Normal)", 2D) = "bump" {}
		_Color("Main Color", Color) = (1,1,1,1)
		_SpecColor("Specular Color", Color) = (0.5, 0.5, 0.5, 1)

		_Thickness("Thickness (R)", 2D) = "bump" {}
		_Power("Subsurface Power", Float) = 1.0
		_Distortion("Subsurface Distortion", Float) = 0.0
		_Scale("Subsurface Scale", Float) = 0.5
		_SubColor("Subsurface Color", Color) = (1.0, 1.0, 1.0, 1.0)

			_Outline("Outline Color", Color) = (0,0,0,1)
			_Size("Outline Thickness", Float) = 1.5
	}
		SubShader{
			Tags { "RenderType" = "Opaque" }
			LOD 200

			 Pass {
				Stencil {
					Ref 1
					Comp NotEqual
				}

				Cull Off
				ZWrite Off

					CGPROGRAM
					#pragma vertex vert
					#pragma fragment frag
					#include "UnityCG.cginc"
					half _Size;
					fixed4 _Outline;
					struct v2f {
						float4 pos : SV_POSITION;
					};
					v2f vert(appdata_base v) {
						v2f o;
						v.vertex.xyz += v.normal * _Size;
						o.pos = UnityObjectToClipPos(v.vertex);
						return o;
					}
					half4 frag(v2f i) : SV_Target
					{
						return _Outline;
					}
					ENDCG
				}


			CGPROGRAM
			#pragma surface surf Translucent
			#pragma target 3.0

			sampler2D _MainTex, _BumpMap, _Thickness;
			float _Scale, _Power, _Distortion;
			fixed4 _Color, _SubColor;

			struct Input {
				float2 uv_MainTex;
				float4 screenPos;
				float eyeDepth;
			};


			void vert(inout appdata_full v, out Input o)
			{
				UNITY_INITIALIZE_OUTPUT(Input, o);
				COMPUTE_EYEDEPTH(o.eyeDepth);
			}

			void surf(Input IN, inout SurfaceOutput o) {
				fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
				o.Albedo = tex.rgb * _Color.rgb;
				o.Alpha = tex2D(_Thickness, IN.uv_MainTex);
				//o.Gloss = tex.a;
				//o.Specular = _Shininess;
				o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
			}

			inline fixed4 LightingTranslucent(SurfaceOutput s, fixed3 lightDir, fixed3 viewDir, fixed atten)
			{
				// Translucency.
				half3 transLightDir = lightDir + s.Normal * _Distortion;
				float transDot = pow(max(0, dot(viewDir, -transLightDir)), _Power) * _Scale;
				fixed3 transLight = (atten * 2) * (transDot)* s.Alpha * _SubColor.rgb;
				fixed3 transAlbedo = s.Albedo * _LightColor0.rgb * transLight;

				// Regular BlinnPhong.
				half3 h = normalize(lightDir + viewDir);
				fixed diff = max(0, dot(s.Normal, lightDir));
				float nh = max(0, dot(s.Normal, h));
				float spec = pow(nh, s.Specular * 128.0) * s.Gloss;
				fixed3 diffAlbedo = (s.Albedo * _LightColor0.rgb * diff + _LightColor0.rgb * _SpecColor.rgb * spec) * (atten * 2);

				// Add the two together.
				fixed4 c;
				c.rgb = diffAlbedo + transAlbedo;
				c.a = _LightColor0.a * _SpecColor.a * spec * atten;
				return c;
			}

			ENDCG
			}
				FallBack "Bumped Diffuse"
}