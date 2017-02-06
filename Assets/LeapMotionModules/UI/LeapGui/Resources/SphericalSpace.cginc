
float _LeapGuiSpherical_Radius;

// x : degree X offset
// y : degree Y offset
// y : degrees per meter
// w : total radius offset
void Spherical_LocalToWorld(inout float3 localVert, float4 parameters) {
  float angleX = parameters.x + localVert.x * parameters.z;
  float angleY = parameters.y + localVert.y * parameters.z;
  float radius = parameters.w + localVert.z;

  float3 temp;
  temp.x = 0;
  temp.y = sin(angleY) * radius;
  temp.z = cos(angleY) * radius;

  localVert.x = sin(angleX) * temp.z;
  localVert.y = temp.y;
  localVert.z = cos(angleX) * temp.z - _LeapGuiSpherical_Radius;
}
