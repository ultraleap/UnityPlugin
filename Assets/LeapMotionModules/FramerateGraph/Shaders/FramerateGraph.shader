Shader "LeapMotion/FramerateGraph" {
	Properties {
    _GraphTexture ("Time Data",  2D) = "white" {}
    _Gradient     ("Color Ramp", 2D) = "white" {}
    _GradientScale ("Gradient Scale", Float) = 0
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
  sampler2D _Gradient;
  float _GradientScale;

  float4 frag(frag_in input) : COLOR {
    fixed4 color = tex2D(_GraphTexture, float2(input.uv.x, 0.5));
    float alpha = step(input.uv.y, color.a);
    return float4(tex2D(_Gradient, color.a * _GradientScale).rgb, alpha);
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
