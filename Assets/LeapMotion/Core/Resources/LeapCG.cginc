#include "UnityCG.cginc"

uniform sampler2D _LeapGlobalBrightnessTexture;

uniform sampler2D _LeapGlobalRawTexture;

uniform sampler2D _LeapGlobalDistortion;


uniform float2 _LeapGlobalBrightnessPixelSize;

uniform float2 _LeapGlobalRawPixelSize;

uniform float4 _LeapGlobalProjection;

uniform float _LeapGlobalGammaCorrectionExponent;

uniform float2 _LeapGlobalStereoUVOffset;

uniform float4x4 _LeapGlobalWarpedOffset;

uniform float4x4 _LeapHandTransforms[2];

/////////////// Constants for Dragonfly Color Correction ///////////////
#define RGB_SCALE     1.5 * float3(1.5, 1.0, 0.5)

#define R_OFFSET      (_LeapGlobalRawPixelSize * float2(-0.5, 0.0))
#define G_OFFSET      (_LeapGlobalRawPixelSize * float2(-0.5, 0.5))
#define B_OFFSET      (_LeapGlobalRawPixelSize * float2( 0.0, 0.5))

#define R_BLEED       -0.05
#define G_BLEED       0.001
#define B_BLEED       -0.105
#define IR_BLEED      1.0

#define TRANSFORMATION  transpose(float4x4(5.0670, -1.2312, 0.8625, -0.0507, -1.5210, 3.1104, -2.0194, 0.0017, -0.8310, -0.3000, 13.1744, -0.1052, -2.4540, -1.3848, -10.9618, 1.0000))
#define CONSERVATIVE    transpose(float4x4(5.0670, 0.0000, 0.8625, 0.0000, 0.0000, 3.1104, 0.0000, 0.0017, 0.0000, 0.0000, 13.1744, 0.0000, 0.0000, 0.0000, 0.0000, 1.0000))

#define FUDGE_THRESHOLD 0.5
#define FUDGE_CONSTANT  (1 / (1 - FUDGE_THRESHOLD))
////////////////////////////////////////////////////////////////////////                                       

/*** LEAP UNDISTORTION ***/

float2 LeapGetUndistortedUVWithOffset(float4 screenPos, float2 uvOffset){
  float2 screenUV = (screenPos.xy / screenPos.w) * 2 - float2(1,1);
  float2 tangent = (screenUV + _LeapGlobalProjection.xy) / _LeapGlobalProjection.zw;
  float2 distortionUV = saturate(0.125 * tangent + float2(0.5, 0.5)) * float2(1, 0.5) + uvOffset;

  float4 distortionAmount = tex2D(_LeapGlobalDistortion, distortionUV);
  float2 leapUV = float2(DecodeFloatRG(distortionAmount.xy), DecodeFloatRG(distortionAmount.zw)) * 2.3 - float2(0.6, 0.6);
  return saturate(leapUV) * float2(1.0, 0.5 - _LeapGlobalRawPixelSize.y) + uvOffset;
}

float2 LeapGetLeftUndistortedUV(float4 screenPos){
  return LeapGetUndistortedUVWithOffset(screenPos, float2(0.0, 0.0));
}

float2 LeapGetRightUndistortedUV(float4 screenPos){
  return LeapGetUndistortedUVWithOffset(screenPos, float2(0.0, 0.5));
}

float2 LeapGetStereoUndistortedUV(float4 screenPos){
  return LeapGetUndistortedUVWithOffset(screenPos, _LeapGlobalStereoUVOffset);
}


/*** LEAP TEMPORAL WARPING ***/

float4 LeapGetWarpedScreenPos(float4 transformedVertex){
  float4 warpedPosition = mul(_LeapGlobalWarpedOffset, transformedVertex);
  return ComputeScreenPos(warpedPosition);
}


/*** LEAP VERTEX WARPING ***/

float4 LeapGetLateVertexPos(float4 vertex, int isLeft){
	return mul(unity_WorldToObject, mul(_LeapHandTransforms[isLeft], mul(unity_ObjectToWorld, vertex)));
}


/*** LEAP BRIGHTNESS ***/

float LeapGetUVBrightness(float2 uv){
    return tex2D(_LeapGlobalBrightnessTexture, uv).a;
}

float LeapGetLeftBrightness(float4 screenPos){
  return LeapGetUVBrightness(LeapGetLeftUndistortedUV(screenPos));
}

float LeapGetRightBrightness(float4 screenPos){
  return LeapGetUVBrightness(LeapGetRightUndistortedUV(screenPos));
}

float LeapGetStereoBrightness(float4 screenPos){
  return LeapGetUVBrightness(LeapGetStereoUndistortedUV(screenPos));
}


/*** LEAP RAW COLOR ***/

float3 LeapGetUVRawColor(float2 uv){
  #if LEAP_FORMAT_IR
    float color = tex2D(_LeapGlobalRawTexture, uv).a;
    return float3(color, color, color);
  #else
    float4 input_lf;

    input_lf.a = tex2D(_LeapGlobalRawTexture, uv).a;
    input_lf.r = tex2D(_LeapGlobalRawTexture, uv + R_OFFSET).b;
    input_lf.g = tex2D(_LeapGlobalRawTexture, uv + G_OFFSET).r;
    input_lf.b = tex2D(_LeapGlobalRawTexture, uv + B_OFFSET).g;

    float4 output_lf       = mul(TRANSFORMATION, input_lf);
    float4 output_lf_fudge = mul(CONSERVATIVE,   input_lf);

    float3 fudgeMult = input_lf.rgb * FUDGE_CONSTANT - FUDGE_CONSTANT * FUDGE_THRESHOLD;
    float3 fudge = step(FUDGE_THRESHOLD, input_lf.rgb) * fudgeMult;

    float3 color = (output_lf_fudge.rgb - output_lf.rgb) * fudge * fudge + output_lf.rgb;
    color *= RGB_SCALE;

    return saturate(color);
  #endif
}

float3 LeapGetLeftRawColor(float4 screenPos){
  return LeapGetUVRawColor(LeapGetLeftUndistortedUV(screenPos));
}

float3 LeapGetRightRawColor(float4 screenPos){
  return LeapGetUVRawColor(LeapGetRightUndistortedUV(screenPos));
}

float3 LeapGetStereoRawColor(float4 screenPos){
  return LeapGetUVRawColor(LeapGetStereoUndistortedUV(screenPos));
}


/*** LEAP COLOR ***/

float3 LeapGetUVColor(float2 uv){
  return pow(LeapGetUVRawColor(uv), _LeapGlobalGammaCorrectionExponent);
}

float3 LeapGetLeftColor(float4 screenPos){
  return pow(LeapGetLeftRawColor(screenPos), _LeapGlobalGammaCorrectionExponent);
}

float3 LeapGetRightColor(float4 screenPos){
  return pow(LeapGetRightRawColor(screenPos), _LeapGlobalGammaCorrectionExponent);
}

float3 LeapGetStereoColor(float4 screenPos){
  return pow(LeapGetStereoRawColor(screenPos), _LeapGlobalGammaCorrectionExponent);
}
