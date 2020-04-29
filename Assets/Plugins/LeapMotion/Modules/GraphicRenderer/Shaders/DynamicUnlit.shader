Shader "LeapMotion/GraphicRenderer/Unlit/Dynamic" {
  Properties {
    _Color   ("Color", Color) = (1,1,1,1)
    _MainTex ("Texture", 2D) = "white" {}
  }
  SubShader {
    Tags {"Queue"="Geometry" "RenderType"="Opaque" }

    Cull Off

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
      #pragma shader_feature _ GRAPHIC_RENDERER_TINTING
      #pragma shader_feature _ GRAPHIC_RENDERER_BLEND_SHAPES
      #pragma shader_feature _ GRAPHIC_RENDERER_ENABLE_CUSTOM_CHANNELS
      #include "Assets/Plugins/LeapMotion/Modules/GraphicRenderer/Resources/DynamicRenderer.cginc"
      #include "UnityCG.cginc"

      sampler2D _MainTex;
      
      v2f_graphic_dynamic vert (appdata_graphic_dynamic v) {
        BEGIN_V2F(v);

        v2f_graphic_dynamic o;
        APPLY_DYNAMIC_GRAPHICS(v, o);

        return o;
      }
      
      fixed4 frag (v2f_graphic_dynamic i) : SV_Target {
        fixed4 color = fixed4(1,1,1,1);

#ifdef GRAPHIC_RENDERER_VERTEX_NORMALS
        color *= abs(dot(normalize(i.normal.xyz), float3(0, 0, 1)));
#endif

#ifdef GRAPHIC_RENDERER_VERTEX_UV_0
        color *= tex2D(_MainTex, i.uv_0);
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
