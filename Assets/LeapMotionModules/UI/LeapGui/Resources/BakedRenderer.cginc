#include "Assets/LeapMotionModules/UI/LeapGui/Resources/LeapGui.cginc"

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
 ***********************************/

#ifdef LEAP_GUI_CYLINDRICAL
#define LEAP_GUI_WARPING
#include "Assets/LeapMotionModules/UI/LeapGui/Resources/CylindricalSpace.cginc"

float4 _LeapGuiCylindrical_ElementParameters[ELEMENT_MAX];

void ApplyGuiWarping(inout float4 vert, int elementId) {
  float4 elementParams = _LeapGuiCylindrical_ElementParameters[elementId];
  Cylindrical_LocalToWorld(vert.xyz, elementParams);
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

float4 _LeapGui_Tints[ELEMENT_MAX];

float4 GetElementTint(int elementId) {
  return _GuiSpace_Tints[elementId];
}
#endif

#ifdef LEAP_GUI_VERTEX_COLORS
#ifndef GUI_ELEMENTS_HAVE_COLOR
#define GUI_ELEMENTS_HAVE_COLOR
#endif
#endif

struct appdata_gui_baked {
  float4 vertex : POSITION;

#ifdef LEAP_GUI_NORMALS
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

#ifdef LEAP_GUI_NORMALS
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

#ifdef LEAP_GUI_WARPING
  ApplyGuiWarping(v.vertex, elementId);
#endif

  v2f_gui_baked o;
  o.vertex = UnityObjectToClipPos(v.vertex);

#ifdef LEAP_GUI_VERTEX_UV_0
  o.uv0 = v.uv0;
#endif

#ifdef LEAP_GUI_VERTEX_COLORS
  o.color = v.color;
#endif

  return o;
}
