Shader "Leap Motion/Graphic Renderer/Examples/Bake 1" {
  Properties {
    _Color("Color", Color) = (1,1,1,1)
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

    #pragma shader_feature _ GRAPHIC_RENDERER_CYLINDRICAL GRAPHIC_RENDERER_SPHERICAL
    #include "Assets/LeapMotionModules/GraphicRenderer/Resources/BakedRenderer.cginc"
    #include "UnityCG.cginc"

    struct Input {
      SURF_INPUT_GRAPHICAL
      float2 uv_MainTex;
    };

    sampler2D _MainTex;

    void vert(inout appdata_graphic_baked v, out Input o) {
      UNITY_INITIALIZE_OUTPUT(Input, o);
      BEGIN_V2F(v);
      APPLY_BAKED_GRAPHICS_STANDARD(v, o);   
    }

    void surf (Input IN, inout SurfaceOutputStandard o) {
      o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb;
    }
    ENDCG
  }
  FallBack "Diffuse"
}
