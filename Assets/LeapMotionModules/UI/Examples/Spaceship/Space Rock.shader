Shader "LeapMotion/Examples/Space Rock" {
	Properties {
	 _Color ("Color", Color) = (1,1,1,1)
   _BumpinessSeed ("Bumpiness Seed", Float) = 0.0
   _BumpinessFreq ("Bumpiness Frequency", Float) = 1.25
   _BumpinessAmount ("Bumpiness Amount", Float) = 0.5
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
      #pragma surface surf Standard fullforwardshadows vertex:vert
      #pragma target 3.0

      struct Input {
        float3 objNormal;
        float3 objPos;
        //float4 noiseValues0;
        //float4 noiseValues1;

        float3 worldNormal;
        INTERNAL_DATA
      };

      half _BumpinessSeed;
      half _BumpinessAmount;
      half _BumpinessFreq;
      float4 _Color;

      #include "classicnoise4d.cginc"

      float doNoise(Input surfIn, float freq, float amp, float seed) {
        return cnoise(float4(surfIn.objPos.x * freq,
                             surfIn.objPos.y * freq,
                             surfIn.objPos.z * freq, seed)) * amp;
      }

      void vert(inout appdata_base v, out Input surfIn) {
        UNITY_INITIALIZE_OUTPUT(Input, surfIn);
        surfIn.objNormal = v.normal;
        surfIn.objPos = v.vertex;

        // Noise harmonics for pixel albedo
        //float freq = 124;
        //float amp = 0.5;
        //float seed = _BumpinessSeed;
        //surfIn.noiseValues0 = float4(doNoise(surfIn, freq / 1,  amp, seed),
        //                             doNoise(surfIn, freq / 2,  amp, seed + 0.01),
        //                             doNoise(surfIn, freq / 4,  amp, seed + 0.02),
        //                             doNoise(surfIn, freq / 8,  amp, seed + 0.04));
        //surfIn.noiseValues1 = float4(doNoise(surfIn, freq / 16, amp, seed + 0.08),
        //                             doNoise(surfIn, freq / 32, amp, seed + 0.16),
        //                             0,
        //                             0);

        // Vertex bumpiness offset   
        float bumpNoiseValue = cnoise(float4(surfIn.objPos.x * _BumpinessFreq,
                                             surfIn.objPos.y * _BumpinessFreq,
                                             surfIn.objPos.z * _BumpinessFreq, _BumpinessSeed)) * _BumpinessAmount;
        v.vertex.xyz += v.normal * bumpNoiseValue;
      }

      void surf (Input surfIn, inout SurfaceOutputStandard o) {
        o.Metallic = 0.2;
        o.Smoothness = 0.0;

        float freq = 124;
        float amp = 0.5;
        float seed = _BumpinessSeed;
        //float extraBump = noiseValues0.x + noiseValues0.y + noiseValues0.z + noiseValues0.w
        //                + noiseValues1.x + noiseValues1.y + noiseValues1.z + noiseValues1.w;
        float extraBump = doNoise(surfIn, freq, amp, _BumpinessSeed);
        extraBump += doNoise(surfIn, freq / 2,  amp, seed + 0.01);
        extraBump += doNoise(surfIn, freq / 4,  amp, seed + 0.02);
        extraBump += doNoise(surfIn, freq / 8,  amp, seed + 0.04);
        extraBump += doNoise(surfIn, freq / 16, amp, seed + 0.08);
        extraBump += doNoise(surfIn, freq / 32, amp, seed + 0.16);
        extraBump = (0.75 + extraBump);
        extraBump = extraBump * extraBump;

        o.Normal = WorldNormalVector(surfIn, o.Normal); // TODO: Can do better per-pixel normals...?
        o.Albedo = _Color * (extraBump);
      }
		ENDCG
	}
}
