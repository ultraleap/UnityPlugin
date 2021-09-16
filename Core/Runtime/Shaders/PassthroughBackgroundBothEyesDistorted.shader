Shader "LeapMotion/Passthrough/BackgroundBackgroundBothEyesDistorted" {
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

    inline float3 LeapGetRawColorDistortedClamped(const float2 clampedUV, const float lower, const float upper)
    {
      const bool outOfBounds = clampedUV.x < 0 || clampedUV.x > 1 || clampedUV.y < lower || clampedUV.y > upper;
      
      return outOfBounds
        ? float4(0., 0., 0., 0.)
        : LeapGetUVRawColor(clampedUV);
    }
      
    float3 LeapGetLeftColorDistorted(const float2 screenUV){

      //used to select one half of the the image buffer, upper image
      const float lower = 0.0;
      const float upper = 0.5;

      //resizes the width w.r.t. the aspect ratio of the image and uses that to compute a scale factor for the
      //resultant UVs, uses half width as each image only takes half the screen width
      const float pixelAspect = _LeapGlobalRawPixelSize.x / _LeapGlobalRawPixelSize.y;
      const float width = (pixelAspect * _ScreenParams.x) / 2;
      const float height = _ScreenParams.y;
      const float2 scale = width > height
                             ? float2(width / height, 1.)
                             : float2(1., height / width);

      //move the scale point to the centre of the image, apply scaling and move back
      const float2 uv = (screenUV - float2(0.5, 0.25)) * scale + float2(0.5, 0.25);

      return pow(LeapGetRawColorDistortedClamped(uv, lower, upper), _LeapGlobalGammaCorrectionExponent);
    }
    
    float3 LeapGetRightColorDistorted(const float2 screenUV){

      //used to select one half of the the image buffer, lower image
      const float lower = 0.5;
      const float upper = 1.0;

      //resizes the width w.r.t. the aspect ratio of the image and uses that to compute a scale factor for the
      //resultant UVs, uses half width as each image only takes half the screen width
      const float pixelAspect = _LeapGlobalRawPixelSize.x / _LeapGlobalRawPixelSize.y;
      const float width = (pixelAspect * _ScreenParams.x) / 2;
      const float height = _ScreenParams.y;
      const float2 scale = width > height
                             ? float2(width / height, 1.)
                             : float2(1., height / width);

      //move the scale point to the centre of the image, apply scaling and move back
      const float2 uv = (screenUV - float2(0.5, 0.75)) * scale + float2(0.5, 0.75);

      return pow(LeapGetRawColorDistortedClamped(uv, lower, upper), _LeapGlobalGammaCorrectionExponent);
    }
    
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
      //remaps uv such that the x component becomes [0,0.5)->[0,1) and [0.5,1.0)->[0,1)
      const float2 uv = i.screenPos.xy / i.screenPos.w;
      const float4 doublePos = float4(i.screenPos.x * 2.0 % i.screenPos.w, i.screenPos.y * 0.5, i.screenPos.zw);

      return uv.x < 0.5
        ? float4(LeapGetLeftColorDistorted (doublePos.xy / doublePos.w), 1)
        : float4(LeapGetRightColorDistorted(doublePos.xy / doublePos.w + float2(0, 0.5)), 1);
    }

    ENDCG
    }
  } 
  Fallback off
}
