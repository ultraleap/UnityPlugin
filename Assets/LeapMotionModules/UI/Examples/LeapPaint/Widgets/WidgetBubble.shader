Shader "LeapMotion/Examples/WidgetBubble" {
	Properties {
    _Color ("Albedo Tint", Color) = (1, 1, 1, 1)
    _EmissionColor ("Glow Tint", Color) = (1, 1, 1, 1)
    _Fade ("Fade Amount", Range(0, 1)) = 0
    _Glow ("Activation Glow", Range(0, 1)) = 0
    _Checker ("Checker Amount", Range(0, 1)) = 0
    _Liquid ("Liquid Distortion Amount", Range(0, 1)) = 0
	}
	SubShader {
		Tags { "Queue"="Transparent" "RenderType"="Transparent" }
    Blend One One
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Standard noshadow alpha:blend vertex:vert
		#pragma target 3.0

    // TODO: To reduce the passes needed to render, add "noforwardadd"
    // and use a simple cubemap to reproduce the reflection effect

    // TODO: Remove liquid distortion and checkering effects to
    // simplify the shader
    
    // TODO: Try to simplify the fragment shader

		struct Input {
      float3 viewDir;
			float3 worldNormal;
      float4 screenPos;
		};

    float rand(float x) {
      return frac(sin(x) * 43758.5453);
    }

    half _Liquid;

    void vert (inout appdata_full v) {
      // "Liquid" Distortion
      half liquid = _Liquid;
      float time = _Time.x;
      v.vertex.xyz += v.normal * sin((v.vertex.x + v.vertex.y + v.vertex.z) * 50 + (time * 20)) * liquid * 0.05;
    }

    fixed4 _Color;
    fixed4 _EmissionColor;

    half _Fade;
    half _Glow;
    half _Checker;

		void surf (Input IN, inout SurfaceOutputStandard o) {

      fixed4 color = _Color;
      fixed4 emissionColor = _EmissionColor;
      half edge = (1 - dot(IN.viewDir, IN.worldNormal));

      // Alpha
      half fade = _Fade;
      half alpha = (edge * edge + 0.1 + (_Glow * 1));

      // Glow
      half glow = _Glow;
      half emissionEdgeGlow1 = edge * edge / 4;
      half fourthPower = edge * edge * edge * edge;
      half emissionEdgeGlow2 = saturate((fourthPower * fourthPower) * 4) / 4;
      
      // Checkering
      half checker = _Checker;
      half2 screenPos = half2(IN.screenPos.y / IN.screenPos.w, IN.screenPos.x / IN.screenPos.w);
      half osc = sin((screenPos.x) * 3000) + sin((screenPos.y) * 3000);
      
      fixed3 albedo = lerp(fixed3(1,1,1), color.rgb, edge*edge);
      o.Albedo = fixed4(albedo.x, albedo.y, albedo.z, edge * edge * color.a);
      o.Metallic = 0.4;
      o.Smoothness = 0.8;
      o.Alpha = alpha * 0.8 * (1 - (osc * checker)) * (1 - fade) + 0.2F;
      o.Emission = (((emissionColor + glow) * glow * glow * 1)
                 + (emissionColor * emissionEdgeGlow1 * 1 + emissionEdgeGlow1 * glow * 0.2)
                 + (emissionColor * emissionEdgeGlow2 * 7 + emissionEdgeGlow2 * glow * glow * 0.2));
    }
		ENDCG
	}
	FallBack Off
}
