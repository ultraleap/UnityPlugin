Shader "LeapMotion/GraphicRenderer/Unlit/Baked" {
  Properties {
    _Color("Color", Color) = (1,1,1,1)
    _MainTex ("Texture", 2D) = "white" {}
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
      #pragma shader_feature _ GRAPHIC_RENDERER_ENABLE_CUSTOM_CHANNELS
      #include "Assets/LeapMotion/Modules/GraphicRenderer/Resources/BakedRenderer.cginc"
      #include "UnityCG.cginc"

      sampler2D _MainTex;
      sampler2D _MainTex2;
      
      v2f_graphic_baked vert (appdata_graphic_baked v) {
        BEGIN_V2F(v);

        v2f_graphic_baked o;
        APPLY_BAKED_GRAPHICS(v,o);

        return o;
      }
      
      fixed4 frag (v2f_graphic_baked i) : SV_Target {
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
