Shader "LeapMotion/FramerateGraph" {
	Properties {
    _GraphTexture ("Time Data",  2D) = "white" {}
    _LineA ("Line A", Float) = 0
    _LineB ("Line B", Float) = 0
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
    o.uv = v.uv;
    return o;
  }

  sampler2D _GraphTexture;
  float _LineA, _LineB;

  float4 frag(frag_in input) : COLOR {
    fixed percent = tex2D(_GraphTexture, float2(input.uv.x, 0.5)).a;
    fixed graphColor = step(input.uv.y, percent);

    fixed distToLine = min(abs(_LineA - input.uv.y), abs(_LineB - input.uv.y));
    fixed lineColor = smoothstep(0.006, 0.005, distToLine);

    fixed color = (graphColor + 1) * 0.5 - lineColor;

    //fixed color = lerp(graphColor, 1 - graphColor, lineColor);
    return float4(color, color, color, 1);
  }
  ENDCG

	SubShader {
		Tags {"Queue"="Transparent"}

    Cull Off 
    Blend SrcAlpha OneMinusSrcAlpha

    Pass {
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      ENDCG
    }
	} 

	FallBack off
}
