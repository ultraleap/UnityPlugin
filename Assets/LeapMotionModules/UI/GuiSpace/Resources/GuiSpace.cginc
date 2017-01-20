
/* Space name:
 *  _ (none)
 *    rect space, with no distortion
 *  GUI_SPACE_CYLINDRICAL
 *    cylindrical space
 */

/* Movement name:
 *  _ (none)
 *    no movement, position is baked into mesh
 *  GUI_ELEMENT_MOVEMENT_TRANSLATION
 *    elements can move around in rect space, but no rotation or scaling
 *  GUI_ELEMENT_MOVEMENT_FULL
 *    each element has a float4x4 to describe it's motion
 */

#define ELEMENT_MAX 32

#ifdef GUI_ELEMENT_MOVEMENT_TRANSLATION
#define GUI_ELEMENTS_HAVE_MOTION
float3 _ElementPosition[ELEMENT_MAX]

void ApplyElementMotion(inout float4 vert, int elementId) {
  vert.xyz += _ElementPosition[elementId];
}
#endif

#ifdef GUI_ELEMENT_MOVEMENT_FULL
#define GUI_ELEMENTS_HAVE_MOTION
float4x4 _ElementTransform[ELEMENT_MAX]

void ApplyElementMotion(inout float4 vert, int elementId) {
  vert = mul(_ElementTransform[elementId], vert);
}
#endif

#ifdef GUI_SPACE_CYLINDRICAL
float _GuiSpaceCylindrical_ReferenceRadius;
float3 _GuiSpaceCylindrical_ParentPosition[ELEMENT_MAX]

void ApplyGuiWarping(inout float4 vert, int elementId) {
  float3 parentPos = _GuiSpaceCylindrical_ParentPosition[elementId];

  parentPos.x += vert.x / parentPos.z;
  parentPos.yz += vert.yz;

  vert.x = sin(parentPos.x) * parentPos.z;
  vert.y = parentPos.y;
  vert.z = cos(parentPos.x) * parentPos.z - _GuiSpaceCylindrical_ReferenceRadius;
}
#endif

// Takes an object space vertex and converts it to a clip space vertex
float4 WarpVert(float4 vert, int elementId) {

#ifdef GUI_ELEMENTS_HAVE_MOTION
  ApplyGuiWarping(vert, elementId);
  ApplyElementMotion(vert, elementId);
#endif

	return vert;
}
