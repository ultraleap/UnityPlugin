Shader "Custom/CustomGuiShader_Surface_2" {
  Properties {
    _MainTex ("Albedo (RGB)", 2D) = "white" {}
  }
  SubShader {
    Tags { "RenderType"="Opaque" }
    LOD 200
    
    CGPROGRAM
    #pragma surface surf Standard fullforwardshadows vertex:vert addshadow 
    #pragma target 3.0

    //Our graphics always has the following features
    #define GRAPHICS_HAVE_ID
    #define GRAPHIC_RENDERER_VERTEX_UV_0
    #define GRAPHIC_RENDERER_VERTEX_NORMALS
    #define GRAPHIC_RENDERER_ENABLE_CUSTOM_CHANNELS

    #pragma shader_feature _ GRAPHIC_RENDERER_CYLINDRICAL GRAPHIC_RENDERER_SPHERICAL
    #include "Assets/LeapMotionModules/GraphicRenderer/Resources/DynamicRenderer.cginc"
    #include "UnityCG.cginc"

    struct Input {
      float2 uv_MainTex;
      float2 metalAndGloss;
    };

    DEFINE_FLOAT_CHANNEL(_Metallic);
    DEFINE_FLOAT_CHANNEL(_Glossiness);
    sampler2D _MainTex;

    void vert(inout appdata_graphic_dynamic v, out Input o) {
      UNITY_INITIALIZE_OUTPUT(Input, o);
      BEGIN_V2F(v);

      APPLY_DYNAMIC_GRAPHICS_STANDARD(v, o);

      o.metalAndGloss.x = getChannel(_Metallic);
      o.metalAndGloss.y = getChannel(_Glossiness);
    }

    void surf(Input IN, inout SurfaceOutputStandard o) {
      o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb;
      o.Metallic = IN.metalAndGloss.x;
      o.Smoothness = IN.metalAndGloss.y;
      o.Alpha = 1;
    }
    ENDCG
  }
  FallBack "Diffuse"
}
