#include "Assets/LeapMotionModules/UI/LeapGui/Resources/LeapGui.cginc"

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

float _LeapGuiCylindrical_ElementRadius[ELEMENT_MAX];

void ApplyGuiWarping(inout float4 vert, int elementId) {
  float raius = _LeapGuiCylindrical_ElementRadius[elementId];
  Cylindrical_ApplyCurvature(vert.xyz, raius);
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

struct appdata_gui_dynamic {
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

struct v2f_gui_dynamic {
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

v2f_gui_dynamic ApplyDynamicGui(appdata_gui_dynamic v) {
#ifdef GUI_ELEMENTS_HAVE_ID
  int elementId = v.vertInfo.w;
#endif

#ifdef LEAP_GUI_BLEND_SHAPES
  ApplyBlendShapes(v.vertex, v.vertInfo, elementId);
#endif

#ifdef LEAP_GUI_WARPING
  ApplyGuiWarping(v.vertex, elementId);
#endif

  v2f_gui_dynamic o;
  o.vertex = UnityObjectToClipPos(v.vertex);

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
