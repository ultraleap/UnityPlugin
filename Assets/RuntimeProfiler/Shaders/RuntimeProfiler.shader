Shader "HandyMan/RuntimeProfiler" {
	Properties {
		_MainTex ("Time Data", 2D) = "white" {}
    _Ramp    ("Color Ramp",  2D) = "white" {}
    _Offset  ("Horizontal Offset", Range(0, 1)) = 0
	}

  CGINCLUDE
  #include "UNityCG.cginc"

  struct frag_in {
    float4 position : SV_POSITION;
    float2 uv : TEXCOORD0;
  };

  struct appdata {
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
  };

  fixed _Offset;

  frag_in vert(appdata v){
    frag_in o;
    o.position = mul(UNITY_MATRIX_MVP, v.vertex);
    o.uv = v.uv + fixed2(_Offset, 0);
    return o;
  }

  sampler2D _MainTex;
  sampler2D _Ramp;

  float4 frag(frag_in input) : COLOR {
    fixed4 color = tex2D(_MainTex, input.uv);
    
    fixed alpha = step(color.x, input.uv.y) * step(color.y, 1.0 - input.uv.y);

    return float4(tex2D(_Ramp, color.z).rgb, alpha);
  }
  ENDCG

	SubShader {
		Tags {"Queue"="Transparent"}

    Cull Off 
    Blend OneMinusSrcAlpha SrcAlpha

    Pass {
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      ENDCG
    }
	} 

	FallBack off
}
