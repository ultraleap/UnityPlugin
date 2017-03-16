Shader "LeapMotion/Examples/GhostableSurfaceShader" {

	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Smoothness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
    _RimPower ("Ghosty Rim Power", Range(1, 5)) = 1.0
	}

	SubShader {
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
		LOD 200
    Blend SrcAlpha OneMinusSrcAlpha
    //ZTest On

    Pass {
        ZWrite On
        ColorMask 0
    }
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
    // fullforwardshadows
		#pragma surface surf Standard noforwardadd alpha:blend

		// Use shader model 3.0 target, to get nicer looking lighting -- NOPE 2.0 for cheapness
		#pragma target 2.0

		sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex;
      float3 viewDir;
      float3 worldNormal;
		};

		half _Smoothness;
		half _Metallic;
		fixed4 _Color;
    float _RimPower;

		void surf (Input IN, inout SurfaceOutputStandard o) {
			o.Metallic = _Metallic;
			o.Smoothness = _Smoothness;
      
      float ghostiness = 1 - _Color.a;
      float rimAmount = 1 - saturate(dot(normalize(IN.viewDir), IN.worldNormal));
      rimAmount = pow(rimAmount, (5 - _RimPower));
      
      float g = ghostiness;
			fixed4 tintedColor = tex2D (_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = lerp(tintedColor.rgb, 0, ghostiness * ghostiness);
      o.Emission = lerp(0, 1, g * g);
			o.Alpha = lerp(1, rimAmount * 0.7, (1 - ((1 - g) * (1 - g))));
		}
		ENDCG
	}
	FallBack "Diffuse"

}
