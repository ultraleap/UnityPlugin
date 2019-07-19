Shader "Hidden/LeapMotion/GraphicRenderer/Examples/Bake 1" {
  Properties {
    _Color("Color", Color) = (1,1,1,1)
    _MainTex ("Albedo (RGB)", 2D) = "white" {}
  }
  SubShader {
    Tags { "RenderType"="Opaque" }
    LOD 200
    
    CGPROGRAM
    #pragma surface surf StandardSpecular fullforwardshadows vertex:vert addshadow 
    #pragma target 3.0

    #define GRAPHIC_RENDERER_VERTEX_UV_0
    #define GRAPHIC_RENDERER_VERTEX_NORMALS
    #define GRAPHIC_RENDERER_MOVEMENT_TRANSLATION
    #define GRAPHIC_RENDERER_ENABLE_CUSTOM_CHANNELS

    #pragma shader_feature _ GRAPHIC_RENDERER_CYLINDRICAL GRAPHIC_RENDERER_SPHERICAL
    #include "Assets/LeapMotion/Modules/GraphicRenderer/Resources/BakedRenderer.cginc"
    #include "UnityCG.cginc"

    struct Input {
      SURF_INPUT_GRAPHICAL
      float2 uv_MainTex;
      float2 smoothness;
      float4 specularColor;
    };

    DEFINE_FLOAT_CHANNEL(_Smoothness);
    DEFINE_FLOAT4_CHANNEL(_SpecularColor);
    sampler2D _MainTex;

    void vert(inout appdata_graphic_baked v, out Input o) {
      UNITY_INITIALIZE_OUTPUT(Input, o);
      BEGIN_V2F(v);

      APPLY_BAKED_GRAPHICS_STANDARD(v, o);   

      o.smoothness = getChannel(_Smoothness);
      o.specularColor = getChannel(_SpecularColor);
    }

    float3 hsv2rgb(float3 c) {
      float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
      float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
      return c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
    }

    void surf (Input IN, inout SurfaceOutputStandardSpecular o) {
      o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb;
      o.Specular = IN.specularColor;
      o.Smoothness = IN.smoothness;
    }
    ENDCG
  }
  FallBack "Diffuse"
}
