Shader "Custom/Baked_Standard_TEMP" {
  Properties {
    _MainTex ("Albedo (RGB)", 2D) = "white" {}
    _Glossiness ("Smoothness", Range(0,1)) = 0.5
    _Metallic ("Metallic", Range(0,1)) = 0.0
  }
  SubShader {
    Tags { "RenderType"="Opaque" }
    LOD 200
    
    CGPROGRAM
    #pragma surface surf Standard fullforwardshadows
    #pragma target 3.0

    sampler2D _MainTex;

    struct Input {
      float2 uv_MainTex;
      float4 color : COLOR;
    };

    half _Glossiness;
    half _Metallic;

    void surf (Input IN, inout SurfaceOutputStandard o) {
      fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * IN.color;
      o.Albedo = c.rgb;
      o.Metallic = _Metallic;
      o.Smoothness = _Glossiness;
      o.Alpha = c.a;
    }
    ENDCG
  }
  FallBack "Diffuse"
}
