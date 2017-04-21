Shader "Custom/CustomGuiShader_Surface" {
  Properties {
    _MainTex ("Albedo (RGB)", 2D) = "white" {}
  }
  SubShader {
    Tags { "RenderType"="Opaque" }
    LOD 200
    
    CGPROGRAM
    #pragma surface surf Standard fullforwardshadows vertex:vert addshadow 
    #pragma target 3.0

    #define GRAPHIC_RENDERER_VERTEX_UV_0
    #define GRAPHIC_RENDERER_VERTEX_NORMALS
    #define GRAPHIC_RENDERER_MOVEMENT_TRANSLATION
    #define GRAPHIC_RENDERER_ENABLE_CUSTOM_CHANNELS

    #pragma shader_feature _ GRAPHIC_RENDERER_CYLINDRICAL GRAPHIC_RENDERER_SPHERICAL
    #include "Assets/LeapMotionModules/GraphicRenderer/Resources/BakedRenderer.cginc"
    #include "UnityCG.cginc"

    struct Input {
      float2 uv_MainTex;
      float2 metalAndGloss;
      float4 emissionColor;
    };

    DEFINE_FLOAT_CHANNEL(_Metallic);
    DEFINE_FLOAT_CHANNEL(_Glossiness);
    DEFINE_FLOAT4_CHANNEL(_EmissionColor);
    sampler2D _MainTex;

    void vert(inout appdata_graphic_baked v, out Input o) {
      UNITY_INITIALIZE_OUTPUT(Input, o);
      BEGIN_V2F(v);

      APPLY_BAKED_GRAPHICS_STANDARD(v, o);   

      o.metalAndGloss.x = getChannel(_Metallic);
      o.metalAndGloss.y = getChannel(_Glossiness);
      o.emissionColor = getChannel(_EmissionColor);
    }

    float3 hsv2rgb(float3 c) {
      float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
      float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
      return c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
    }

    void surf (Input IN, inout SurfaceOutputStandard o) {
      o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb;
      o.Emission = IN.emissionColor;
      o.Metallic = IN.metalAndGloss.x;
      o.Smoothness = IN.metalAndGloss.y;
    }
    ENDCG
  }
  FallBack "Diffuse"
}
