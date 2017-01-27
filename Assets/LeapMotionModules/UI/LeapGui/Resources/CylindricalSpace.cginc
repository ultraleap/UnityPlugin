
float _LeapGuiCylindrical_Radius;

void Cylindrical_LocalToWorld(float3 elementPos, inout float3 localVert) {
  float3 worldPos = elementPos;
  worldPos.x += localVert.x / elementPos.z;
  worldPos.yz += localVert.yz;

  localVert.x = sin(worldPos.x) * worldPos.z;
  localVert.y = worldPos.y;
  localVert.z = cos(worldPos.x) * worldPos.z - _LeapGuiCylindrical_Radius;
}
