Shader "LeapMotion/GraphicRenderer/Testing/Unlit/Dynamic" {
  Properties {
    _Color   ("Color", Color) = (1,1,1,1)
    _MainTex ("Texture", 2D) = "white" {}
  }
  SubShader {
    Tags {"Queue"="Geometry" "RenderType"="Opaque" }

    Cull Off

    Pass {
      CGPROGRAM
      #pragma target 5.0
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
      #include "Assets/LeapMotion/Modules/GraphicRenderer/Resources/DynamicRenderer.cginc"
      #include "UnityCG.cginc"

      sampler2D _MainTex;
      
      RWStructuredBuffer<float3> _FinalVertexPositions : register(u1);

      struct appdata_graphic_dynamic_testing {
        float4 vertex : POSITION;

        uint id : SV_VertexID;

#ifdef GRAPHIC_RENDERER_VERTEX_NORMALS
        float3 normal : NORMAL;
#endif

#ifdef GRAPHICS_HAVE_ID
        float4 vertInfo : TANGENT;
#endif

#ifdef GRAPHIC_RENDERER_VERTEX_UV_0
        float4 texcoord : TEXCOORD0;
#endif

#ifdef GRAPHIC_RENDERER_VERTEX_UV_1
        float4 texcoord1 : TEXCOORD1;
#endif

#ifdef GRAPHIC_RENDERER_VERTEX_UV_2
        float4 texcoord2 : TEXCOORD2;
#endif

#ifdef GRAPHIC_RENDERER_VERTEX_UV_3
        float4 texcoord3 : TEXCOORD3;
#endif

#ifdef GRAPHIC_RENDERER_VERTEX_COLORS
        float4 color : COLOR;
#endif
      };

      v2f_graphic_dynamic vert (appdata_graphic_dynamic_testing v) {
        BEGIN_V2F(v);

        v2f_graphic_dynamic o;
        APPLY_DYNAMIC_GRAPHICS(v, o);

#ifdef GRAPHICS_NEED_ANCHOR_SPACE
        float4 worldPos = v.vertex;
#else
        float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
#endif
        
        _FinalVertexPositions[v.id] = worldPos;

        return o;
      }
      
      fixed4 frag (v2f_graphic_dynamic i) : SV_Target {
        return fixed4(0, 1, 0, 1);
      }
      ENDCG
    }
  }
}
