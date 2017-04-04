// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

#include "Assets/LeapMotionModules/ElementRenderer/Resources/LeapGui.cginc"

/***********************************
 * Space name:
 *  _ (none)
 *    rect space, with no distortion
 *  LEAP_GUI_CYLINDRICAL
 *    cylindrical space
 ***********************************/

float4x4 _LeapGui_WorldToGui;
float4x4 _LeapGui_GuiToWorld;

#ifdef LEAP_GUI_CYLINDRICAL
#define LEAP_GUI_WARPING
#include "Assets/LeapMotionModules/ElementRenderer/Resources/CylindricalSpace.cginc"

float4 _LeapGuiCurved_ElementParameters[ELEMENT_MAX];
float4x4 _LeapGui_LocalToWorld;

#ifdef LEAP_GUI_VERTEX_NORMALS
void ApplyGuiWarping(inout float4 anchorSpaceVert, inout float4 anchorSpaceNormal, int elementId) {
  float4 parameters = _LeapGuiCurved_ElementParameters[elementId];

  Cylindrical_LocalToWorld(anchorSpaceVert.xyz, anchorSpaceNormal.xyz, parameters);

  anchorSpaceVert = mul(_LeapGui_LocalToWorld, anchorSpaceVert);
  anchorSpaceNormal = mul(_LeapGui_LocalToWorld, float4(anchorSpaceNormal.xyz, 0));
}
#else
void ApplyGuiWarping(inout float4 anchorSpaceVert, int elementId) {
  float4 parameters = _LeapGuiCurved_ElementParameters[elementId];

  Cylindrical_LocalToWorld(anchorSpaceVert.xyz, parameters);

  anchorSpaceVert = mul(_LeapGui_LocalToWorld, anchorSpaceVert);
}
#endif
#endif

#ifdef LEAP_GUI_SPHERICAL
#define LEAP_GUI_WARPING
#include "Assets/LeapMotionModules/ElementRenderer/Resources/SphericalSpace.cginc"

float4 _LeapGuiCurved_ElementParameters[ELEMENT_MAX];
float4x4 _LeapGui_LocalToWorld;

#ifdef LEAP_GUI_VERTEX_NORMALS
void ApplyGuiWarping(inout float4 anchorSpaceVert, inout float4 anchorSpaceNormal, int elementId) {
  float4 parameters = _LeapGuiCurved_ElementParameters[elementId];

  Spherical_LocalToWorld(anchorSpaceVert.xyz, anchorSpaceNormal.xyz, parameters);

  anchorSpaceVert = mul(_LeapGui_LocalToWorld, anchorSpaceVert);
  anchorSpaceNormal = mul(_LeapGui_LocalToWorld, float4(anchorSpaceNormal.xyz, 0));
}
#else
void ApplyGuiWarping(inout float4 anchorSpaceVert, int elementId) {
  float4 parameters = _LeapGuiCurved_ElementParameters[elementId];

  Spherical_LocalToWorld(anchorSpaceVert.xyz, parameters);

  anchorSpaceVert = mul(_LeapGui_LocalToWorld, anchorSpaceVert);
}
#endif
#endif

#ifdef LEAP_GUI_WARPING
#ifndef GUI_ELEMENTS_HAVE_ID
#define GUI_ELEMENTS_HAVE_ID
#endif
#ifndef GUI_ELEMENTS_NEED_ANCHOR_SPACE
#define GUI_ELEMENTS_NEED_ANCHOR_SPACE
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
#ifndef GUI_ELEMENTS_NEED_ANCHOR_SPACE
#define GUI_ELEMENTS_NEED_ANCHOR_SPACE
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

#ifdef GUI_ELEMENTS_NEED_ANCHOR_SPACE
float4x4 _LeapGuiCurved_WorldToAnchor[ELEMENT_MAX];
#endif

struct appdata_gui_dynamic {
  float4 vertex : POSITION;

#ifdef LEAP_GUI_VERTEX_NORMALS
  float4 normal : NORMAL;
#endif

#ifdef LEAP_GUI_VERTEX_UV_0
  float4 uv0 : TEXCOORD0;
#endif

#ifdef LEAP_GUI_VERTEX_UV_1
  float4 uv1 : TEXCOORD1;
#endif

#ifdef LEAP_GUI_VERTEX_UV_2
  float4 uv2 : TEXCOORD2;
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

#ifdef LEAP_GUI_VERTEX_NORMALS
  float4 normal : NORMAL;
#endif

#ifdef LEAP_GUI_VERTEX_UV_0
  float4 uv0 : TEXCOORD0;
#endif

#ifdef LEAP_GUI_VERTEX_UV_1
  float4 uv1 : TEXCOORD2;
#endif

#ifdef LEAP_GUI_VERTEX_UV_2
  float4 uv2 : TEXCOORD3;
#endif

#ifdef GUI_ELEMENTS_HAVE_COLOR
  float4 color : COLOR;
#endif
};

v2f_gui_dynamic ApplyDynamicGui(appdata_gui_dynamic v) {
#ifdef GUI_ELEMENTS_HAVE_ID
#ifdef GUI_ELEMENT_ID_FROM_UV0
  int elementId = v.uv0.w;
#else
  int elementId = v.vertInfo.w;
#endif
#endif

#ifdef GUI_ELEMENTS_NEED_ANCHOR_SPACE
  v.vertex = mul(unity_ObjectToWorld, v.vertex);
  v.vertex = mul(_LeapGuiCurved_WorldToAnchor[elementId], v.vertex);
#ifdef LEAP_GUI_VERTEX_NORMALS
  v.normal = mul(unity_ObjectToWorld, float4(v.normal.xyz, 0));
  v.normal = mul(_LeapGuiCurved_WorldToAnchor[elementId], float4(v.normal.xyz, 0));
#endif
#endif

#ifdef LEAP_GUI_BLEND_SHAPES
  ApplyBlendShapes(v.vertex, v.vertInfo, elementId);
#endif

#ifdef LEAP_GUI_WARPING
#ifdef LEAP_GUI_VERTEX_NORMALS
  ApplyGuiWarping(v.vertex, v.normal, elementId);
#else
  ApplyGuiWarping(v.vertex, elementId);
#endif
#endif

  v2f_gui_dynamic o;
#ifdef GUI_ELEMENTS_NEED_ANCHOR_SPACE
  o.vertex = mul(UNITY_MATRIX_VP, v.vertex);
#if LEAP_GUI_VERTEX_NORMALS
  o.normal = v.normal;
#endif
#else
  o.vertex = UnityObjectToClipPos(v.vertex);
#if LEAP_GUI_VERTEX_NORMALS
  o.normal = UnityObjectToWorldNormal(v.normal);
#endif
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
