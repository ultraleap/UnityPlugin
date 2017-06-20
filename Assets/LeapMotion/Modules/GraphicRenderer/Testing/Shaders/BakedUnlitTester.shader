// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "LeapMotion/GraphicRenderer/Testing/Unlit/Baked" {
  Properties {
    _Color("Color", Color) = (1,1,1,1)
    _MainTex ("Texture", 2D) = "white" {}
  }
  SubShader {
    Tags {"Queue"="Geometry" "RenderType"="Opaque" }

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
      #pragma shader_feature _ GRAPHIC_RENDERER_MOVEMENT_TRANSLATION GRAPHIC_RENDERER_MOVEMENT_FULL
      #pragma shader_feature _ GRAPHIC_RENDERER_TINTING
      #pragma shader_feature _ GRAPHIC_RENDERER_BLEND_SHAPES
      #include "Assets/LeapMotion/Modules/GraphicRenderer/Resources/BakedRenderer.cginc"
      #include "UnityCG.cginc"

      sampler2D _MainTex;
      sampler2D _MainTex2;

      RWStructuredBuffer<float3> _FinalVertexPositions : register(u1);
      
      struct appdata_graphic_baked_test {
        float4 vertex : POSITION;

        uint id : SV_VertexID;

#ifdef GRAPHIC_RENDERER_VERTEX_NORMALS
        float3 normal : NORMAL;
#endif

#ifdef GRAPHICS_HAVE_ID
        float4 vertInfo : TANGENT;
#endif

#ifdef GRAPHIC_RENDERER_VERTEX_UV_0
        float2 texcoord : TEXCOORD0;
#endif

#ifdef GRAPHIC_RENDERER_VERTEX_UV_1
        float2 texcoord1 : TEXCOORD1;
#endif

#ifdef GRAPHIC_RENDERER_VERTEX_UV_2
        float2 texcoord2 : TEXCOORD2;
#endif

#ifdef GRAPHIC_RENDERER_VERTEX_UV_3
        float2 texcoord3 : TEXCOORD3;
#endif

#ifdef GRAPHIC_RENDERER_VERTEX_COLORS
        float4 color : COLOR;
#endif
      };

      v2f_graphic_baked vert (appdata_graphic_baked_test v) {
        BEGIN_V2F(v);

        v2f_graphic_baked o;
        APPLY_BAKED_GRAPHICS(v,o);

        float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
        _FinalVertexPositions[v.id] = worldPos;

        return o;
      }
      
      fixed4 frag (v2f_graphic_baked i) : SV_Target {
        return fixed4(0,1,0,1);
      }
      ENDCG
    }
  }
}
