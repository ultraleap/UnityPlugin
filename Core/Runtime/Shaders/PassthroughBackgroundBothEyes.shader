Shader "LeapMotion/Passthrough/BackgroundBothEyes" {
  SubShader {
    Tags {"Queue"="Background" "IgnoreProjector"="True"}

    Cull Off
    Zwrite Off
    Blend One Zero

    Pass{
    CGPROGRAM
    #include "../Resources/LeapCG.cginc"
    #include "UnityCG.cginc"
    
    #pragma target 3.0
    
    #pragma vertex vert
    #pragma fragment frag
    
    uniform float _LeapGlobalColorSpaceGamma;

    struct frag_in{
      float4 position : SV_POSITION;
      float4 screenPos  : TEXCOORD1;
    };

    frag_in vert(appdata_img v){
      frag_in o;
      o.position = UnityObjectToClipPos(v.vertex);
      o.screenPos = LeapGetWarpedScreenPos(o.position);
      return o;
    }

    float4 frag (frag_in i) : COLOR {
      const float2 uv = i.screenPos.xy / i.screenPos.w;
      const float4 doublePos = float4((i.screenPos.x * 2.0) % i.screenPos.w, i.screenPos.y, i.screenPos.zw);
      return uv.x < 0.5
        ? float4(LeapGetLeftColor (doublePos), 1)
        : float4(LeapGetRightColor(doublePos), 1);
    }

    ENDCG
    }
  } 
  Fallback off
}
