Shader "Hidden/Runtime Gizmo Filled" {
  Properties {
    _Color ("Color", Color) = (1,1,1,1)
  }
  SubShader {
    Tags { "RenderType"="Opaque" }
    LOD 100

    Pass {
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      
      #include "UnityCG.cginc"

      struct appdata {
        float4 vertex : POSITION;
        float3 normal : NORMAL;
      };

      struct v2f {
        float4 vertex : SV_POSITION;
        float fresnelValue : TEXCOORD0;
      };
      
      v2f vert (appdata v) {
        v2f o;
        o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
        float3 viewDir = normalize(ObjSpaceViewDir(v.vertex));
        o.fresnelValue = saturate(dot(v.normal, viewDir));
        return o;
      }

      fixed4 _Color;
      
      fixed4 frag (v2f i) : SV_Target {
        return _Color * i.fresnelValue;
      }
      ENDCG
    }
  }
}
