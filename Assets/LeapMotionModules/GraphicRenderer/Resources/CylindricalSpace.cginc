
float _GraphicRendererRadialSpace_Radius;

void Cylindrical_LocalToWorld(inout float3 localVert, float3 elementPos) {
  float3 worldPos = elementPos;
  worldPos.x += localVert.x / elementPos.z;
  worldPos.yz += localVert.yz;

  localVert.x = sin(worldPos.x) * worldPos.z;
  localVert.y = worldPos.y;
  localVert.z = cos(worldPos.x) * worldPos.z - _GraphicRendererRadialSpace_Radius;
}

//Cylindrical parameters:
// x : degree offset
// y : total height offset
// z : total radius offset
// w: degrees per meter
void Cylindrical_LocalToWorld(inout float3 localVert, float4 parameters) {
  float angle = localVert.x * parameters.w + parameters.x;
  float height = parameters.y + localVert.y;
  float radius = parameters.z + localVert.z;

  localVert.x = sin(angle) * radius;
  localVert.y = height;
  localVert.z = cos(angle) * radius - _GraphicRendererRadialSpace_Radius;
}

void Cylindrical_LocalToWorld(inout float3 localVert, inout float3 localNormal, float4 parameters) {
  float angle = localVert.x * parameters.w + parameters.x;
  float height = parameters.y + localVert.y;
  float radius = parameters.z + localVert.z;

  float s, c;
  sincos(angle, s, c);

  localVert.x = s * radius;
  localVert.y = height;
  localVert.z = c * radius - _GraphicRendererRadialSpace_Radius;
  
  float tX = c * localNormal.x + s * localNormal.z;
  localNormal.z = c * localNormal.z - s * localNormal.x;
  localNormal.x = tX;
}




