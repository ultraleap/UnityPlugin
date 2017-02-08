Shader "LeapGui/Defaults/DynamicTransparent" {
  Properties {
    _MainTex ("Texture", 2D) = "white" {}
  }
  SubShader {
    Tags {"Queue"="Transparent" "RenderType"="Transparent" }

    Blend SrcAlpha OneMinusSrcAlpha
    ZTest On
    ZWrite Off

    Pass {
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag

      #pragma shader_feature _ LEAP_GUI_CYLINDRICAL LEAP_GUI_SPHERICAL
      #pragma shader_feature _ LEAP_GUI_VERTEX_NORMALS
      #pragma shader_feature _ LEAP_GUI_VERTEX_UV_0
      #pragma shader_feature _ LEAP_GUI_VERTEX_UV_1
      #pragma shader_feature _ LEAP_GUI_VERTEX_UV_2
      #pragma shader_feature _ LEAP_GUI_VERTEX_COLORS
      #pragma shader_feature _ LEAP_GUI_MOVEMENT_TRANSLATION LEAP_GUI_MOVEMENT_FULL
      #pragma shader_feature _ LEAP_GUI_TINTING
      #pragma shader_feature _ LEAP_GUI_BLEND_SHAPES
      #include "Assets/LeapMotionModules/UI/LeapGui/Resources/DynamicRenderer.cginc"
      #include "UnityCG.cginc"

      sampler2D _MainTex;
      
      v2f_gui_dynamic vert (appdata_gui_dynamic v) {
        return ApplyDynamicGui(v);
      }
      
      fixed4 frag (v2f_gui_dynamic i) : SV_Target {
#ifdef LEAP_GUI_VERTEX_UV_0
        fixed4 color = tex2D(_MainTex, i.uv0);
#ifdef GUI_ELEMENTS_HAVE_COLOR
        color *= i.color;
#endif
        return color;
#else
#ifdef GUI_ELEMENTS_HAVE_COLOR
        return i.color;
#endif
#endif
        return fixed4(1,1,1,1);
      }
      ENDCG
    }
  }
}
