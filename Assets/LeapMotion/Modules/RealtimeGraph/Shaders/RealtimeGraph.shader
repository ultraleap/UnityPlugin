Shader "LeapMotion/RealtimeGraph" {
  Properties {
    _GraphTexture ("Time Data",  2D)     = "white" {}
    _LineTexture  ("Line Texture", 2D)   = "white" {}
    _GraphScale   ("_GraphScale", Float) = 0
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
    o.position = UnityObjectToClipPos(v.vertex);
    o.uv = v.uv;
    return o;
  }

  sampler2D _GraphTexture;
  sampler2D _LineTexture;
  half _GraphScale;

  float4 frag(frag_in input) : COLOR {
    fixed percent = tex2D(_GraphTexture, input.uv).a;
    fixed graphColor = step(input.uv.y, percent);

    fixed lineColor = tex2D(_LineTexture, input.uv * _GraphScale);

    fixed color = graphColor * lineColor + lineColor;

    return color;
  }
  ENDCG

  SubShader {
    Tags {"Queue"="Geometry"}

    Cull Off 
    Blend One Zero

    Pass {
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      ENDCG
    }
  } 

  FallBack off
}
