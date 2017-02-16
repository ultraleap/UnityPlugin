#include "Assets/LeapMotionModules/ElementRenderer/Resources/LeapGui.cginc"

/************************************************************************* 
 * Movement name:
 *  _ (none)
 *    no movement, position is baked into mesh
 *  LEAP_GUI_MOVEMENT_TRANSLATION
 *    elements can move around in rect space, but no rotation or scaling
 *  LEAP_GUI_MOVEMENT_FULL
 *    each element has a float4x4 to describe it's motion
 *************************************************************************/

#ifdef LEAP_GUI_MOVEMENT_TRANSLATION
#define LEAP_GUI_MOVEMENT
void ApplyElementMotion(inout float4 vert, int elementId) { }
#endif

#ifdef LEAP_GUI_MOVEMENT_FULL
#define LEAP_GUI_MOVEMENT
float4x4 _GuiElementMovement_Transform[ELEMENT_MAX];

void ApplyElementMotion(inout float4 vert, int elementId) {
  vert = mul(_GuiElementMovement_Transform[elementId], vert);
}
#endif

#ifdef LEAP_GUI_MOVEMENT
#ifndef GUI_ELEMENTS_HAVE_ID
#define GUI_ELEMENTS_HAVE_ID
#endif
#endif

/***********************************
 * Space name:
 *  _ (none)
 *    rect space, with no distortion
 *  LEAP_GUI_CYLINDRICAL
 *    cylindrical space
 *  LEAP_GUI_SPHERICAL
 *    spherical space
 ***********************************/

#ifdef LEAP_GUI_CYLINDRICAL
#define LEAP_GUI_WARPING
#include "Assets/LeapMotionModules/ElementRenderer/Resources/CylindricalSpace.cginc"

float4 _LeapGuiCurved_ElementParameters[ELEMENT_MAX];

#ifdef LEAP_GUI_VERTEX_NORMALS
void ApplyGuiWarping(inout float4 vert, inout float4 normal, int elementId) {
  float4 elementParams = _LeapGuiCurved_ElementParameters[elementId];
  Cylindrical_LocalToWorld(vert.xyz, normal.xyz, elementParams);
}
#else
void ApplyGuiWarping(inout float4 vert, int elementId) {
  float4 elementParams = _LeapGuiCurved_ElementParameters[elementId];
  Cylindrical_LocalToWorld(vert.xyz, elementParams);
}
#endif
#endif

#ifdef LEAP_GUI_SPHERICAL
#define LEAP_GUI_WARPING
#include "Assets/LeapMotionModules/ElementRenderer/Resources/SphericalSpace.cginc"

float4 _LeapGuiCurved_ElementParameters[ELEMENT_MAX];

#ifdef LEAP_GUI_VERTEX_NORMALS
void ApplyGuiWarping(inout float4 vert, inout float4 normal, int elementId) {
  float4 elementParams = _LeapGuiCurved_ElementParameters[elementId];
  Spherical_LocalToWorld(vert.xyz, elementParams);
}
#else
void ApplyGuiWarping(inout float4 vert, int elementId) {
  float4 elementParams = _LeapGuiCurved_ElementParameters[elementId];
  Spherical_LocalToWorld(vert.xyz, elementParams);
}
#endif
#endif

//Base-case fallback, rect transformations
#ifndef LEAP_GUI_WARPING
#define LEAP_GUI_WARPING
float3 _LeapGuiRect_ElementPositions[ELEMENT_MAX];

void ApplyGuiWarping(inout float4 vert, int elementId) {
  vert.xyz += _LeapGuiRect_ElementPositions[elementId];
}
#endif

#ifdef LEAP_GUI_WARPING
#ifndef GUI_ELEMENTS_HAVE_ID
#define GUI_ELEMENTS_HAVE_ID
#endif
#endif



/***********************************
 * Feature name:
 *  _ (none)
 *    no runtime tinting, base color only
 *  LEAP_GUI_TINTING
 *    runtime tinting on a per-element basis
 ***********************************/

#ifdef LEAP_GUI_TINTING
#ifndef GUI_ELEMENTS_HAVE_ID
#define GUI_ELEMENTS_HAVE_ID
#endif
#ifndef GUI_ELEMENTS_HAVE_COLOR
#define GUI_ELEMENTS_HAVE_COLOR
#endif

float4 _LeapGuiTints[ELEMENT_MAX];

float4 GetElementTint(int elementId) {
  return _LeapGuiTints[elementId];
}
#endif

/***********************************
 * Feature name:
 *  _ (none)
 *    no runtime blend shapes
 *  LEAP_GUI_BLEND_SHAPES
 *    runtime application of blend shapes on a per-element basis
 ***********************************/

#ifdef LEAP_GUI_BLEND_SHAPES
#ifndef GUI_ELEMENTS_HAVE_ID
#define GUI_ELEMENTS_HAVE_ID
#endif

float _LeapGuiBlendShapeAmounts[ELEMENT_MAX];

void ApplyBlendShapes(inout float4 vert, float4 uv3, int elementId) {
  vert.xyz += uv3.xyz * _LeapGuiBlendShapeAmounts[elementId];
}
#endif

#ifdef LEAP_GUI_VERTEX_COLORS
#ifndef GUI_ELEMENTS_HAVE_COLOR
#define GUI_ELEMENTS_HAVE_COLOR
#endif
#endif

struct appdata_gui_baked {
  float4 vertex : POSITION;

#ifdef LEAP_GUI_VERTEX_NORMALS
  float4 normal : NORMAL;
#endif

#ifdef LEAP_GUI_VERTEX_UV_0
  float2 uv0 : TEXCOORD0;
#endif

#ifdef LEAP_GUI_VERTEX_UV_1
  float2 uv1 : TEXCOORD1;
#endif

#ifdef LEAP_GUI_VERTEX_UV_2
  float2 uv2 : TEXCOORD2;
#endif

#ifdef GUI_ELEMENTS_HAVE_ID
  float4 vertInfo : TEXCOORD3;
#endif

#ifdef LEAP_GUI_VERTEX_COLORS
  float4 color : COLOR;
#endif
};

struct v2f_gui_baked {
  float4 vertex : SV_POSITION;

#ifdef LEAP_GUI_VERTEX_NORMALS
  float4 normal : NORMAL;
#endif

#ifdef LEAP_GUI_VERTEX_UV_0
  float2 uv0 : TEXCOORD0;
#endif

#ifdef LEAP_GUI_VERTEX_UV_1
  float2 uv1 : TEXCOORD2;
#endif

#ifdef LEAP_GUI_VERTEX_UV_2
  float2 uv2 : TEXCOORD3;
#endif

#ifdef GUI_ELEMENTS_HAVE_COLOR
  float4 color : COLOR;
#endif
};

v2f_gui_baked ApplyBakedGui(appdata_gui_baked v) {
#ifdef GUI_ELEMENTS_HAVE_ID
  int elementId = v.vertInfo.w;
#endif

#ifdef LEAP_GUI_BLEND_SHAPES
  ApplyBlendShapes(v.vertex, v.vertInfo, elementId);
#endif

#ifdef LEAP_GUI_WARPING
#if LEAP_GUI_VERTEX_NORMALS
  ApplyGuiWarping(v.vertex, v.normal, elementId);
#else
  ApplyGuiWarping(v.vertex, elementId);
#endif
#endif

  v2f_gui_baked o;
  o.vertex = UnityObjectToClipPos(v.vertex);

#ifdef LEAP_GUI_VERTEX_NORMALS
  o.normal = v.normal; //TODO object to clip??
#endif

#ifdef LEAP_GUI_VERTEX_UV_0
  o.uv0 = v.uv0;
#endif

#ifdef LEAP_GUI_TINTING
  o.color = GetElementTint(elementId);
#ifdef LEAP_GUI_VERTEX_COLORS
  o.color *= v.color;
#endif
#else
#ifdef LEAP_GUI_VERTEX_COLORS
  o.color = v.color;
#endif
#endif

  return o;
}
