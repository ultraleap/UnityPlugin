Shader "LeapMotion/VertexOffsetShader" {
  Properties{
    _Color("Color", Color) = (1, 1, 1, 1)
    _MainTex("Albedo (RGB)", 2D) = "white" {}
    _Glossiness("Smoothness", Range(0, 1)) = 0.5
    _Metallic("Metallic", Range(0, 1)) = 0.0
    [MaterialToggle] _isLeftHand("Is Left Hand?", Int) = 0
  }
  SubShader{
    Tags{ "RenderType" = "Opaque" }
    LOD 200

    CGPROGRAM
    #pragma surface surf Standard fullforwardshadows vertex:vert
    #include "Assets/Plugins/LeapMotion/Core/Resources/LeapCG.cginc"
    #pragma target 3.0

    int _isLeftHand;
    void vert(inout appdata_full v) {
      v.vertex = LeapGetLateVertexPos(v.vertex, _isLeftHand);
    }

    sampler2D _MainTex;

    struct Input {
      float2 uv_MainTex;
    };

    half _Glossiness;
    half _Metallic;
    fixed4 _Color;

    void surf(Input IN, inout SurfaceOutputStandard o) {
      // Albedo comes from a texture tinted by color
      fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
      o.Albedo = c.rgb;
      // Metallic and smoothness come from slider variables
      o.Metallic = _Metallic;
      o.Smoothness = _Glossiness;
      o.Alpha = c.a;
    }
    ENDCG
  }
  FallBack "Diffuse"
}