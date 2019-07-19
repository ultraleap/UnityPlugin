Shader "LeapMotion/GraphicRenderer/Text/Dynamic" {
  Properties {
    _Color("Color", Color) = (1,1,1,1)
    _MainTex ("Texture", 2D) = "white" {}
  }
  SubShader {
    Tags {"Queue"="Transparent" "RenderType"="Transparent" }

    Blend SrcAlpha OneMinusSrcAlpha
    ZWrite Off
    ZTest On
    Offset -1, -10000
    Cull Off

    Pass {
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag

      #define GRAPHIC_RENDERER_VERTEX_UV_0
      #define GRAPHIC_ID_FROM_UV0

      #pragma shader_feature _ GRAPHIC_RENDERER_CYLINDRICAL GRAPHIC_RENDERER_SPHERICAL
      #pragma shader_feature _ GRAPHIC_RENDERER_VERTEX_COLORS
      #include "Assets/LeapMotion/Modules/GraphicRenderer/Resources/DynamicRenderer.cginc"
      #include "UnityCG.cginc"

      sampler2D _MainTex;
      
      v2f_graphic_dynamic vert (appdata_graphic_dynamic v) {
        BEGIN_V2F(v);

        v2f_graphic_dynamic o;
        APPLY_DYNAMIC_GRAPHICS(v, o);

        return o;
      }
      
      fixed4 frag (v2f_graphic_dynamic i) : SV_Target {
#ifdef GRAPHICS_HAVE_COLOR
        fixed4 color = i.color;
        color.a *= tex2D(_MainTex, i.uv_0).a;
#else
        fixed4 color = tex2D(_MainTex, i.uv_0);
#endif

        return color;
      }
      ENDCG
    }
  }
}
