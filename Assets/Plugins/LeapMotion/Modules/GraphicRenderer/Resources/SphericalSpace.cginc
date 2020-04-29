
float _GraphicRendererRadialSpace_Radius;

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
  localVert.z = cos(angleX) * temp.z - _GraphicRendererRadialSpace_Radius;
}

void Spherical_LocalToWorld(inout float3 localVert, inout float3 localNorm, float4 parameters) {
  float angleX = parameters.x + localVert.x * parameters.w;
  float angleY = parameters.y + localVert.y * parameters.w;
  float radius = parameters.z + localVert.z;

  float ay_s, ay_c;
  sincos(angleY, ay_s, ay_c);
  float ax_s, ax_c;
  sincos(angleX, ax_s, ax_c);

  float3 temp;
  temp.x = 0;
  temp.y = ay_s * radius;
  temp.z = ay_c * radius;

  localVert.x = ax_s * temp.z;
  localVert.y = temp.y;
  localVert.z = ax_c * temp.z - _GraphicRendererRadialSpace_Radius;

  temp.x = localNorm.x;
  temp.y = localNorm.y * ay_c + localNorm.z * ay_s;
  temp.z = localNorm.z * ay_c - localNorm.y * ay_s;

  localNorm.x = temp.x * ax_c + temp.z * ax_s;
  localNorm.y = temp.y;
  localNorm.z = temp.z * ax_c - temp.x * ax_s;
}

