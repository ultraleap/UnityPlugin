
float _LeapGuiRadialSpace_Radius;

void Cylindrical_LocalToWorld(inout float3 localVert, float3 elementPos) {
  float3 worldPos = elementPos;
  worldPos.x += localVert.x / elementPos.z;
  worldPos.yz += localVert.yz;

  localVert.x = sin(worldPos.x) * worldPos.z;
  localVert.y = worldPos.y;
  localVert.z = cos(worldPos.x) * worldPos.z - _LeapGuiRadialSpace_Radius;
}

// x : degree offset
// y : degrees per meter
// z : total height offset
// w : total radius offset
void Cylindrical_LocalToWorld(inout float3 localVert, float4 parameters) {
  float angle = localVert.x * parameters.w + parameters.x;
  float height = parameters.y + localVert.y;
  float radius = parameters.z + localVert.z;

  localVert.x = sin(angle) * radius;
  localVert.y = height;
  localVert.z = cos(angle) * radius - _LeapGuiRadialSpace_Radius;
}
