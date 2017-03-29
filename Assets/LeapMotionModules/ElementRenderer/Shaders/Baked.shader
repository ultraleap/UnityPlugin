Shader "LeapGui/Defaults/Baked" {
  Properties {
    _MainTex ("Texture", 2D) = "white" {}
    _MainTex2 ("Texture", 2D) = "white" {}
  }
  SubShader {
    Tags {"Queue"="Geometry" "RenderType"="Opaque" }

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
      #include "Assets/LeapMotionModules/ElementRenderer/Resources/BakedRenderer.cginc"
      #include "UnityCG.cginc"

      sampler2D _MainTex;
      sampler2D _MainTex2;

      DEFINE_FLOAT_CHANNEL(_Wiggle);
      
      v2f_gui_baked vert (appdata_gui_baked v) {
        BEGIN_V2F(v);

        v.vertex.x += getChannel(_Wiggle) * sin(_Time.z * 7 + v.vertex.y);

        v2f_gui_baked o;
        APPLY_BAKED_GUI(v,o);

        return o;
      }
      
      fixed4 frag (v2f_gui_baked i) : SV_Target {
        fixed4 color = fixed4(1,1,1,1);

#if LEAP_GUI_VERTEX_UV_0
        color *= tex2D(_MainTex, i.uv0);
#endif

#if LEAP_GUI_VERTEX_UV_0
        color *= tex2D(_MainTex2, i.uv0);
#endif

#ifdef GUI_ELEMENTS_HAVE_COLOR
        color *= i.color;
#endif

        return color;
      }
      ENDCG
    }
  }
}
