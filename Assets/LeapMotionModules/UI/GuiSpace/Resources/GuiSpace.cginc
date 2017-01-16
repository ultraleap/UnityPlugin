// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'


uniform float4x4 _GuiSpaceTransform;
uniform float4 _GuiSpaceParams; 

// Takes an object space vertex and converts it to a clip space vertex
float4 GuiVertToClipSpace(float4 vert) {
	float4 worldVert = mul(unity_ObjectToWorld, vert);

	return mul(UNITY_MATRIX_VP, worldVert);
}
