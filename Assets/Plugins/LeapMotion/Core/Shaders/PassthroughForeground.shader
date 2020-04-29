Shader "LeapMotion/Passthrough/Foreground" {
  Properties {
  }

  SubShader {
    Tags {"Queue"="Geometry" "IgnoreProjector"="True" "RenderType"="Opaque"}

    Cull Off
    Zwrite On
    Blend One Zero

    Pass{
    CGPROGRAM
    #pragma multi_compile LEAP_FORMAT_IR LEAP_FORMAT_RGB
    #include "Assets/Plugins/LeapMotion/Core/Resources/LeapCG.cginc"
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
      return float4(LeapGetStereoColor(i.screenPos), 1);
    }

    ENDCG
    }
  } 
  Fallback off
}
