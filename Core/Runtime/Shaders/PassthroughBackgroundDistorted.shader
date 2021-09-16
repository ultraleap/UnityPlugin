Shader "LeapMotion/Passthrough/BackgroundDistorted" {
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

    inline float3 LeapGetRawColorDistortedClamped(const float2 clampedUV, const float lower, const float upper)
    {
      const bool outOfBounds = clampedUV.x < 0 || clampedUV.x > 1 || clampedUV.y < lower || clampedUV.y > upper;
      
      return outOfBounds
        ? float4(0., 0., 0., 0.)
        : LeapGetUVRawColor(clampedUV);
    }
    
    float3 LeapGetStereoColorDistorted(const float2 screenUV){

      //used to select one half of the the image buffer
      const float lower = unity_StereoEyeIndex == 0 ? 0 : 0.5;
      const float upper = unity_StereoEyeIndex == 0 ? 0.5 : 1.0;

      //resizes the width w.r.t. the aspect ratio of the image and uses that to compute a scale factor for the
      //resultant UVs
      const float imageAspect = _LeapGlobalRawPixelSize.x / _LeapGlobalRawPixelSize.y;
      const float width = imageAspect * _ScreenParams.x;
      const float height = _ScreenParams.y;
      const float2 scale = width > height
                             ? float2(width / height, 1.)
                             : float2(1., height / width);

      //move the scale point to the centre of the image, apply scaling and move back
      const float2 uv = (screenUV - float2(0.5, 0.25 + lower)) * scale + float2(0.5, 0.25 + lower);

      return pow(LeapGetRawColorDistortedClamped(uv, lower, upper), _LeapGlobalGammaCorrectionExponent);
    }
  
    frag_in vert(const appdata_img v){
      frag_in o;
      o.position = UnityObjectToClipPos(v.vertex);
      o.screenPos = LeapGetWarpedScreenPos(o.position);
      return o;
    }

    float4 frag (const frag_in i) : COLOR {
      const float4 pos = float4(i.screenPos.x, i.screenPos.y * 0.5, i.screenPos.zw);
      
      //perspective divide
      float offset = unity_StereoEyeIndex == 0 ? 0 : 0.5;
      const float2 screenUV = pos.xy / pos.w + float2(0, offset);
      
      return float4(LeapGetStereoColorDistorted(screenUV), 1);
    }

    ENDCG
    }
  } 
  Fallback off
}
