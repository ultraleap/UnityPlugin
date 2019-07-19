Shader "LeapMotion/Passthrough/ThresholdOverlay" {
  Properties {
    _Min ("Min Brightness", Range(0, 1)) = 0.1
    _Max ("Max Brightness", Range(0, 1)) = 0.3
    _Fade ("Alpha Fade", Float) = 0.5
  }

  SubShader {
    Tags {"Queue"="Overlay" "IgnoreProjector"="True" "RenderType"="Transparent"}

    Lighting Off
    Cull Off
    Zwrite Off
    ZTest Off

    Blend SrcColor DstColor
    BlendOp Max

    Pass{
    CGPROGRAM
    #pragma multi_compile LEAP_FORMAT_IR LEAP_FORMAT_RGB
    #include "Assets/Plugins/LeapMotion/Core/Resources/LeapCG.cginc"
    #include "UnityCG.cginc"
    
    #pragma target 3.0
    
    #pragma vertex vert
    #pragma fragment frag

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

    uniform float _Min;
    uniform float _Max;
    uniform float _Fade;

    float4 frag (frag_in i) : COLOR {
      float3 color = LeapGetStereoColor(i.screenPos);
      float brightness = LeapGetStereoColor(i.screenPos);
      float alpha = _Fade * smoothstep(_Min, _Max, brightness);
      return float4(color, alpha);
    }

    ENDCG
    }
  } 
  Fallback off
}
