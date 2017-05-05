Shader "Leap Motion/Graphic Renderer/Dynamic/Basic" {
  Properties {
    _Color("Color", Color) = (1,1,1,1)
    _MainTex ("Albedo (RGB)", 2D) = "white" {}
  }
  SubShader {
    Tags { "Queue"="Transparent" "RenderType"="Transparent" }
    LOD 200
    
    ZWrite Off
    ZTest On

    CGPROGRAM
    #pragma surface surf Standard alpha:blend fullforwardshadows vertex:vert addshadow 
    #pragma target 3.0

    //Our graphics always has the following features
    #define GRAPHIC_RENDERER_VERTEX_UV_0
    #define GRAPHIC_RENDERER_VERTEX_COLORS
    #define GRAPHIC_RENDERER_VERTEX_NORMALS

    #pragma shader_feature _ GRAPHIC_RENDERER_CYLINDRICAL GRAPHIC_RENDERER_SPHERICAL
    #include "Assets/LeapMotionModules/GraphicRenderer/Resources/DynamicRenderer.cginc"
    #include "UnityCG.cginc"

    struct Input {
      SURF_INPUT_GRAPHICAL
      float2 uv_MainTex;
    };

    sampler2D _MainTex;

    void vert(inout appdata_graphic_dynamic v, out Input o) {
      UNITY_INITIALIZE_OUTPUT(Input, o);
      BEGIN_V2F(v);
      APPLY_DYNAMIC_GRAPHICS_STANDARD(v, o);
    }

    void surf(Input IN, inout SurfaceOutputStandard o) {
      fixed4 color = tex2D(_MainTex, IN.uv_MainTex) * IN.color;
      o.Albedo = color.rgb;
      o.Alpha = color.a;
    }
    ENDCG
  }
  FallBack "Diffuse"
}
