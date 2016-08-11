Shader "Hidden/Runtime Gizmos" {
  Properties {
    _Color ("Color", Color) = (1,1,1,1)
  }

  CGINCLUDE
    #include "UnityCG.cginc"

    struct appdata_unlit {
      float4 vertex : POSITION;
      float3 normal : NORMAL;
    };

    struct appdata_shaded {
      float4 vertex : POSITION;
      float3 normal : NORMAL;
    };

    struct v2f_unlit {
      float4 vertex : SV_POSITION;
    };

    struct v2f_shaded {
      float4 vertex : SV_POSITION;
      float fresnelValue : TEXCOORD0;
    };
      
    v2f_shaded vert_shaded (appdata_shaded v) {
      v2f_shaded o;
      o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
      float3 viewDir = normalize(ObjSpaceViewDir(v.vertex));
      o.fresnelValue = lerp(0.39, 0.66, saturate(dot(v.normal, viewDir)));
      return o;
    }

    v2f_unlit vert_unlit (appdata_unlit v) {
      v2f_unlit o;
      o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
      return o;
    }

    fixed4 _Color;
      
    fixed4 frag_shaded (v2f_shaded i) : SV_Target {
      return _Color * i.fresnelValue;
    }

    fixed4 frag_unlit(v2f_unlit i) : SV_Target{
      return _Color;
    }

  ENDCG

  SubShader {

    //Pass 0 : Unlit solid
    Pass {
      Blend One Zero
      ZWrite On
      ZTest On

      CGPROGRAM
      #pragma vertex vert_unlit
      #pragma fragment frag_unlit
      ENDCG
    }

    //Pass 1 : Unlit transparent
    Pass {
      Blend SrcAlpha OneMinusSrcAlpha
      ZWrite Off
      ZTest On

      CGPROGRAM
      #pragma vertex vert_unlit
      #pragma fragment frag_unlit
      ENDCG
    }

    //Pass 2 : Shaded solid
    Pass {
      Blend One Zero
      ZWrite On
      ZTest On

      CGPROGRAM
      #pragma vertex vert_shaded
      #pragma fragment frag_shaded
      ENDCG
    }

    //Pass 3 : Shaded transparent
    Pass {
      Blend SrcAlpha OneMinusSrcAlpha
      ZWrite Off
      ZTest On

      CGPROGRAM
      #pragma vertex vert_shaded
      #pragma fragment frag_shaded
      ENDCG
    }
  }
}
