#define MAX_NUMBER_OF_GLOBAL_TEXTURES 6 // note that this number should be the same as MAX_NUMBER_OF_GLOBAL_TEXTURES in LeapImageRetriever.cs

UNITY_DECLARE_TEX2DARRAY(_LeapGlobalRawTexture);

uniform float4 _LeapGlobalTextureSizes[MAX_NUMBER_OF_GLOBAL_TEXTURES];

UNITY_DECLARE_TEX2DARRAY(_LeapGlobalDistortion);

uniform float2 _LeapGlobalRawPixelSize;

// X, Y are the image center coordinates. Z, W are the image aspect ratio.
//uniform float4 _LeapGlobalProjection;

uniform float _LeapGlobalGammaCorrectionExponent;

uniform float2 _LeapImageLeftUVOffset;
uniform float2 _LeapImageRightUVOffset;

uniform float4x4 _LeapGlobalWarpedOffset;

uniform float4x4 _LeapHandTransforms[2];

#include "UnityCG.cginc"

/*** LEAP UNDISTORTION ***/

#define USE_GLOBAL_PROJECTION 0

float2 LeapGetUndistortedUVWithOffset(float4 screenPos, float2 uvOffset) {
	float2 screenUV = screenPos.xy / screenPos.w;
#if UNITY_SINGLE_PASS_STEREO
	float4 scaleOffset = unity_StereoScaleOffset[unity_StereoEyeIndex];
	screenUV = (screenUV - scaleOffset.zw) / scaleOffset.xy;
#endif

	screenUV = screenUV * 2 - float2(1, 1);

	// Removed: Replaced with below
#if USE_GLOBAL_PROJECTION
  // float2 tangent = (screenUV + _LeapGlobalProjection.xy) / _LeapGlobalProjection.zw;
#else
  // No need for _LeapGlobalProjection because Unity provides it via UnityCG.cginc: -NB
	float4 projection;
	projection.x = UNITY_MATRIX_P[0][2];
	projection.y = UNITY_MATRIX_P[1][2];
	projection.z = UNITY_MATRIX_P[0][0];
	projection.w = UNITY_MATRIX_P[1][1];

  // Flip vertically.
  // Multiplication by _ProjectionParams.x is here for the case where this is negative
	projection.y = -projection.y;
	projection.w = -projection.w * _ProjectionParams.x;

	float2 tangent = (screenUV + projection.xy) / projection.zw;
#endif

	// Magic number alert: 0.125 == 1/8 is due to the fact that the distortion ray angles
	// in the distortion texture are also multiplied by 1/8.
	// It is also possible to query for the rayscale in LeapC, but platform doesn't do this,
	// so apparently we won't either.
	float2 distortionUV = saturate(0.125 * tangent + float2(0.5, 0.5))
		* float2(1, 0.5) + uvOffset;
	float4 distortionAmount = UNITY_SAMPLE_TEX2DARRAY(_LeapGlobalDistortion, float3(distortionUV, screenPos.z));

	// DecodeFloatRG decodes two 8-bit/channel RG values into a single float between [0..1).
	// More magic number alerts: I have no idea why 2.3 - float2(0.6, 0.6)
	float2 leapUV = float2(DecodeFloatRG(distortionAmount.xy), DecodeFloatRG(distortionAmount.zw)) * 2.3 - float2(0.6, 0.6);
	return saturate(leapUV) * float2(1.0, 0.5 - _LeapGlobalRawPixelSize.y) + uvOffset;
}

float2 LeapGetLeftUndistortedUV(float4 screenPos) {
	return LeapGetUndistortedUVWithOffset(screenPos, float2(0.0, 0.0));
}

float2 LeapGetRightUndistortedUV(float4 screenPos) {
	return LeapGetUndistortedUVWithOffset(screenPos, float2(0.0, 0.5));
}

float2 LeapGetStereoUndistortedUV(float4 screenPos) {
	float offset = unity_StereoEyeIndex == 0 ? 0 : 0.5;
	return LeapGetUndistortedUVWithOffset(screenPos, float2(0.0, offset));
}


/*** LEAP TEMPORAL WARPING ***/

float4 LeapGetWarpedScreenPos(float4 transformedVertex) {
	float4 warpedPosition = mul(_LeapGlobalWarpedOffset, transformedVertex);
	return ComputeScreenPos(warpedPosition);
}

float4 LeapGetWarpedAndHorizontallyMirroredScreenPos(float4 transformedVertex){
	// Clip space is from -1 to 1. We remap this value to 0-2, flip the value, then map it back to -1 to 1
	transformedVertex.x = transformedVertex.x + 1;
	transformedVertex.x = 2 - transformedVertex.x;
	transformedVertex.x = transformedVertex.x - 1;
	return LeapGetWarpedScreenPos(transformedVertex);
}


/*** LEAP VERTEX WARPING ***/

float4 LeapGetLateVertexPos(float4 vertex, int isLeft) {
	return mul(unity_WorldToObject, mul(_LeapHandTransforms[isLeft], mul(unity_ObjectToWorld, vertex)));
}


/*** LEAP RAW COLOR ***/

float2 MapUVInPaddedTexture(float2 uv, int idx) {

	if(uv.x > (_LeapGlobalTextureSizes[idx].z - 1) / _LeapGlobalTextureSizes[idx].z + 1 / (2 * _LeapGlobalTextureSizes[idx].z)) {
		uv.x = (_LeapGlobalTextureSizes[idx].z - 1) / _LeapGlobalTextureSizes[idx].z + 1 / (2 * _LeapGlobalTextureSizes[idx].z);
	}
	
	float u = (uv.x - 1 / (2 * _LeapGlobalTextureSizes[idx].z)) 
				* (_LeapGlobalTextureSizes[idx].x - 1) / (_LeapGlobalTextureSizes[idx].z - 1)
				+ 1 / (2 * _LeapGlobalTextureSizes[idx].z);


	if(uv.y > (_LeapGlobalTextureSizes[idx].w - 1) / _LeapGlobalTextureSizes[idx].w + 1 / (2 * _LeapGlobalTextureSizes[idx].w)) {
		uv.y = (_LeapGlobalTextureSizes[idx].w - 1) / _LeapGlobalTextureSizes[idx].w + 1 / (2 * _LeapGlobalTextureSizes[idx].w);
	}

	float v = (uv.y - 1 / (2 * _LeapGlobalTextureSizes[idx].w)) 
				* (_LeapGlobalTextureSizes[idx].y - 1) / (_LeapGlobalTextureSizes[idx].w - 1)
				+ 1 / (2 * _LeapGlobalTextureSizes[idx].w);

	return float2(u, v);
	}

float3 LeapGetUVRawColor(float2 uv, float z) {
	// the textures have padding around them (to be able to use texture2DArray), 
	// that's why we need to adjust the uv value accordingly
	uv = MapUVInPaddedTexture(uv, int(z));
	float color = UNITY_SAMPLE_TEX2DARRAY(_LeapGlobalRawTexture, float3(uv, z)).a;
	return float3(color, color, color);
}


float3 LeapGetLeftRawColor(float4 screenPos) {
	return LeapGetUVRawColor(LeapGetLeftUndistortedUV(screenPos), screenPos.z);
}

float3 LeapGetRightRawColor(float4 screenPos) {
	return LeapGetUVRawColor(LeapGetRightUndistortedUV(screenPos), screenPos.z);
}

float3 LeapGetStereoRawColor(float4 screenPos) {
	return LeapGetUVRawColor(LeapGetStereoUndistortedUV(screenPos), screenPos.z);
}


/*** LEAP COLOR ***/

float3 LeapGetUVColor(float2 uv, float z) {
	return pow(LeapGetUVRawColor(uv, z), _LeapGlobalGammaCorrectionExponent);
}

float3 LeapGetLeftColor(float4 screenPos) {
	return pow(LeapGetLeftRawColor(screenPos), _LeapGlobalGammaCorrectionExponent);
}

float3 LeapGetRightColor(float4 screenPos) {
	return pow(LeapGetRightRawColor(screenPos), _LeapGlobalGammaCorrectionExponent);
}

float3 LeapGetStereoColor(float4 screenPos) {
	return pow(LeapGetStereoRawColor(screenPos), _LeapGlobalGammaCorrectionExponent);
}
