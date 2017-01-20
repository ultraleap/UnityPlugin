Shader "Unlit/LeapGuiShader" {
  Properties {
    _MainTex ("Texture", 2D) = "white" {}
    _Value   ("Value", Vector) = (0,0,0,0)
    _Offset  ("Offset", Vector) = (0,0,0,0)
  }
  SubShader {
    Tags { "Queue"="Geometry" "RenderType"="Opaque" }

    Pass {
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #include "Assets/LeapMotionModules/UI/GuiSpace/Resources/GuiSpace.cginc"
      #include "UnityCG.cginc"

      struct appdata {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
        float4 color : COLOR;
        float4 vertInfo : TEXCOORD3;
      };

      struct v2f {
        float2 uv : TEXCOORD0;
        float4 vertex : SV_POSITION;
        float4 color : COLOR;
      };

      sampler2D _MainTex;
      float4 _MainTex_ST;
      
      v2f vert (appdata v) {
        v2f o;
        WarpVert(v.vertex, v.vertInfo);
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.uv = TRANSFORM_TEX(v.uv, _MainTex);
        o.color = v.color;
        return o;
      }

      float4 _Value;
      float4 _Offset;
      
      fixed4 frag (v2f i) : SV_Target {
        return tex2D(_MainTex, i.uv) * i.color;
      }
      ENDCG
    }
  }
}
