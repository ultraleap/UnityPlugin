Shader "Unlit/LeapGuiShader" {
  Properties {
    _MainTex ("Texture", 2D) = "white" {}
  }
  SubShader {
    Tags {"Queue"="Geometry" "RenderType"="Opaque" }

    Cull Off

    Pass {
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag

      #pragma shader_feature GUI_SPACE_NORMALS
      #pragma shader_feature GUI_SPACE_UV_0
      #pragma shader_feature GUI_SPACE_UV_1
      #pragma shader_feature GUI_SPACE_UV_2
      #pragma shader_feature GUI_SPACE_VERTEX_COLORS
      #pragma shader_feature GUI_SPACE_BLEND_SHAPES
      #pragma shader_feature GUI_ELEMENT_MOVEMENT_TRANSLATION GUI_ELEMENT_MOVEMENT_FULL
      #pragma shader_feature GUI_SPACE_CYLINDRICAL
      #include "Assets/LeapMotionModules/UI/GuiSpace/Resources/GuiSpace.cginc"
      #include "UnityCG.cginc"

      sampler2D _MainTex;
      
      v2f_gui vert (appdata_gui v) {
        return ApplyGuiSpace(v);
      }
      
      fixed4 frag (v2f_gui i) : SV_Target {
        fixed4 color;

#ifdef GUI_SPACE_UV_0
        fixed4 color = tex2D(_MainTex, i.uv0);
#ifdef GUI_SPACE_VERTEX_COLORS
        color *= i.color;
#endif
        return color;
#else
#ifdef GUI_SPACE_VERTEX_COLORS
        return i.color;
#elseif
        return fixed4(1,1,1,1);
#endif
#endif
      }
      ENDCG
    }
  }
}
