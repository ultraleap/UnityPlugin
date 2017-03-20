Shader "Custom/Baked_Standard_TEMP" {
  Properties {
    _MainTex ("Albedo (RGB)", 2D) = "white" {}
    _Smoothness ("Smoothness", Range(0, 1)) = 1
    _MetallicGlossMap("Metal Map", 2D) = "white" {}
    _BumpMap ("Normal Map", 2D) = "white" {}
    _BumpScale ("Bump Scale", Float) = 1
  }
  SubShader {
    Tags { "RenderType"="Opaque" }
    LOD 200
    
    CGPROGRAM
    #pragma surface surf Standard fullforwardshadows
    #pragma target 3.0

    sampler2D _MainTex;
    sampler2D _MetallicGlossMap;
    sampler2D _BumpMap;
    fixed _Smoothness;
    fixed _BumpScale;

    struct Input {
      float2 uv_MainTex;
      float4 color : COLOR;
    };

    void surf (Input IN, inout SurfaceOutputStandard o) {
      o.Albedo = tex2D (_MainTex, IN.uv_MainTex);
      o.Normal = UnpackScaleNormal(tex2D(_BumpMap, IN.uv_MainTex), _BumpScale);

      float2 ms = tex2D(_MetallicGlossMap, IN.uv_MainTex).ra;

      o.Smoothness = ms.y * _Smoothness;
      o.Metallic = ms.x;
    }
    ENDCG
  }
  FallBack "Diffuse"
}
