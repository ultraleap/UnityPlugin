Shader "LeapMotion/Examples/Space Rock" {
	Properties {
	 _Color ("Color", Color) = (1,1,1,1)
   _Seed ("Seed", Float) = 0.0
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
      #pragma surface surf Standard fullforwardshadows// vertex:vert
      #pragma target 3.0

      struct Input {
        float3 objNormal;
      };

      half _Seed;
      float4 _Color;

      #include "classicnoise4d.cginc"

      void vert(inout appdata_full v, out Input surfIn) {
        surfIn.objNormal = v.normal;

        v.vertex.xyz += v.normal * 0.1;
      }

      void surf (Input surfIn, inout SurfaceOutputStandard o) {
        o.Metallic = 0.2;
        o.Smoothness = 0.0;
        o.Albedo = float4(1, 1, 1, 1);
      }
		ENDCG
	}
}
