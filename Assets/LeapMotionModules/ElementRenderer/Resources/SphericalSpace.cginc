
float _LeapGuiRadialSpace_Radius;

//Parameter format:
// x : degree X offset
// y : degree Y offset
// w : total radius offset
// w : degrees per meter
void Spherical_LocalToWorld(inout float3 localVert, float4 parameters) {
  float angleX = parameters.x + localVert.x * parameters.w;
  float angleY = parameters.y + localVert.y * parameters.w;
  float radius = parameters.z + localVert.z;

  float3 temp;
  temp.x = 0;
  temp.y = sin(angleY) * radius;
  temp.z = cos(angleY) * radius;

  localVert.x = sin(angleX) * temp.z;
  localVert.y = temp.y;
  localVert.z = cos(angleX) * temp.z - _LeapGuiRadialSpace_Radius;
}
