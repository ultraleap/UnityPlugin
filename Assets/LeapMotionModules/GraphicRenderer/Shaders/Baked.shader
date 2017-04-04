Shader "Leap Motion/Graphic Renderer/Defaults/Baked" {
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

      #pragma shader_feature _ GRAPHIC_RENDERER_CYLINDRICAL GRAPHIC_RENDERER_SPHERICAL
      #pragma shader_feature _ GRAPHIC_RENDERER_VERTEX_NORMALS
      #pragma shader_feature _ GRAPHIC_RENDERER_VERTEX_UV_0
      #pragma shader_feature _ GRAPHIC_RENDERER_VERTEX_UV_1
      #pragma shader_feature _ GRAPHIC_RENDERER_VERTEX_UV_2
      #pragma shader_feature _ GRAPHIC_RENDERER_VERTEX_COLORS
      #pragma shader_feature _ GRAPHIC_RENDERER_MOVEMENT_TRANSLATION GRAPHIC_RENDERER_MOVEMENT_FULL
      #pragma shader_feature _ GRAPHIC_RENDERER_TINTING
      #pragma shader_feature _ GRAPHIC_RENDERER_BLEND_SHAPES
      #include "Assets/LeapMotionModules/GraphicRenderer/Resources/BakedRenderer.cginc"
      #include "UnityCG.cginc"

      sampler2D _MainTex;
      sampler2D _MainTex2;

      DEFINE_FLOAT_CHANNEL(_Wiggle);
      
      v2f_graphic_baked vert (appdata_graphic_baked v) {
        BEGIN_V2F(v);

        v.vertex.x += getChannel(_Wiggle) * sin(_Time.z * 7 + v.vertex.y);

        v2f_graphic_baked o;
        APPLY_BAKED_GRAPHICS(v,o);

        return o;
      }
      
      fixed4 frag (v2f_graphic_baked i) : SV_Target {
        fixed4 color = fixed4(1,1,1,1);

#if GRAPHIC_RENDERER_VERTEX_UV_0
        color *= tex2D(_MainTex, i.uv0);
#endif

#if GRAPHIC_RENDERER_VERTEX_UV_0
        color *= tex2D(_MainTex2, i.uv0);
#endif

#ifdef GRAPHICS_HAVE_COLOR
        color *= i.color;
#endif

        return color;
      }
      ENDCG
    }
  }
}
