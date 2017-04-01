#include "Assets/LeapMotionModules/GraphicRenderer/Resources/GraphicRenderer.cginc"

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
#ifndef LEAP_GUI_MOVEMENT
#define LEAP_GUI_MOVEMENT
#endif
#endif

#ifdef LEAP_GUI_MOVEMENT_FULL
#ifndef LEAP_GUI_MOVEMENT
#define LEAP_GUI_MOVEMENT
#endif
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
#ifdef LEAP_GUI_MOVEMENT
#ifdef LEAP_GUI_CYLINDRICAL
#define LEAP_GUI_WARPING
#include "Assets/LeapMotionModules/GraphicRenderer/Resources/CylindricalSpace.cginc"

float4 _LeapGuiCurved_ElementParameters[GRAPHIC_MAX];

#ifdef LEAP_GUI_VERTEX_NORMALS
void ApplyGuiWarping(inout float4 vert, inout float3 normal, int elementId) {
  float4 elementParams = _LeapGuiCurved_ElementParameters[elementId];
  Cylindrical_LocalToWorld(vert.xyz, normal, elementParams);
}
#else
void ApplyGuiWarping(inout float4 vert, int elementId) {
  float4 elementParams = _LeapGuiCurved_ElementParameters[elementId];
  Cylindrical_LocalToWorld(vert.xyz, elementParams);
}
#endif
#endif
#endif

#ifdef LEAP_GUI_MOVEMENT
#ifdef LEAP_GUI_SPHERICAL
#define LEAP_GUI_WARPING
#include "Assets/LeapMotionModules/GraphicRenderer/Resources/SphericalSpace.cginc"

float4 _LeapGuiCurved_ElementParameters[GRAPHIC_MAX];

#ifdef LEAP_GUI_VERTEX_NORMALS
void ApplyGuiWarping(inout float4 vert, inout float3 normal, int elementId) {
  float4 elementParams = _LeapGuiCurved_ElementParameters[elementId];
  Spherical_LocalToWorld(vert.xyz, normal, elementParams);
}
#else
void ApplyGuiWarping(inout float4 vert, int elementId) {
  float4 elementParams = _LeapGuiCurved_ElementParameters[elementId];
  Spherical_LocalToWorld(vert.xyz, elementParams);
}
#endif
#endif
#endif

//Base-case fallback, rect transformations
#ifdef LEAP_GUI_MOVEMENT
#ifndef LEAP_GUI_WARPING
#define LEAP_GUI_WARPING
float3 _LeapGuiRect_ElementPositions[GRAPHIC_MAX];

#ifdef LEAP_GUI_VERTEX_NORMALS
void ApplyGuiWarping(inout float4 vert, inout float3 normal, int elementId) {
  vert.xyz += _LeapGuiRect_ElementPositions[elementId];
}
#else
void ApplyGuiWarping(inout float4 vert, int elementId) {
  vert.xyz += _LeapGuiRect_ElementPositions[elementId];
}
#endif
#endif
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

float4 _LeapGuiTints[GRAPHIC_MAX];

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

float _LeapGuiBlendShapeAmounts[GRAPHIC_MAX];

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
  float3 normal : NORMAL;
#endif

#ifdef LEAP_GUI_VERTEX_UV_0
  float2 texcoord : TEXCOORD0;
#endif

#ifdef LEAP_GUI_VERTEX_UV_1
  float2 texcoord1 : TEXCOORD1;
#endif

#ifdef LEAP_GUI_VERTEX_UV_2
  float2 texcoord2 : TEXCOORD2;
#endif

#ifdef GUI_ELEMENTS_HAVE_ID
  float4 vertInfo : TEXCOORD3;
#endif

#ifdef LEAP_GUI_VERTEX_COLORS
  float4 color : COLOR;
#endif
};

#ifdef LEAP_GUI_VERTEX_NORMALS
#define __V2F_NORMALS float3 normal : NORMAL;
#else
#define __V2F_NORMALS
#endif

#ifdef LEAP_GUI_VERTEX_UV_0
#define __V2F_UV0 float2 uv0 : TEXCOORD0;
#else
#define __V2F_UV0
#endif

#ifdef LEAP_GUI_VERTEX_UV_1
#define __V2F_UV1 float2 uv1 : TEXCOORD1;
#else
#define __V2F_UV1
#endif

#ifdef LEAP_GUI_VERTEX_UV_2
#define __V2F_UV2 float2 uv2 : TEXCOORD2;
#else
#define __V2F_UV2
#endif

#ifdef GUI_ELEMENTS_HAVE_COLOR
#define __V2F_COLOR float4 color : COLOR;
#else
#define __V2F_COLOR
#endif

#define V2F_GRAPHICAL           \
  float4 vertex : SV_POSITION;  \
  __V2F_NORMALS                 \
  __V2F_UV0                     \
  __V2F_UV1                     \
  __V2F_UV2                     \
  __V2F_COLOR

struct v2f_gui_baked {
  V2F_GRAPHICAL
};

#ifdef GUI_ELEMENTS_HAVE_ID
#define BEGIN_V2F(v) int elementId = v.vertInfo.w;
#else
#define BEGIN_V2F(v)
#endif

#ifdef LEAP_GUI_BLEND_SHAPES
#define __APPLY_BLEND_SHAPES(v,o) ApplyBlendShapes(v.vertex, v.vertInfo, elementId);
#else
#define __APPLY_BLEND_SHAPES(v,o)
#endif

#ifdef LEAP_GUI_WARPING
#ifdef LEAP_GUI_VERTEX_NORMALS
#define __APPLY_WARPING(v,o) ApplyGuiWarping(v.vertex, v.normal, elementId); \
                             o.normal = UnityObjectToWorldNormal(v.normal);
#else
#define __APPLY_WARPING(v,o) ApplyGuiWarping(v.vertex, elementId);
#endif
#else
#define __APPLY_WARPING(v,o)
#endif

#ifdef LEAP_GUI_VERTEX_UV_0
#define __COPY_UV0(v,o) o.uv0 = v.texcoord;
#else
#define __COPY_UV0(v,o)
#endif

#ifdef LEAP_GUI_VERTEX_UV_1
#define __COPY_UV1(v,o) o.uv1 = v.texcoord1;
#else
#define __COPY_UV1(v,o)
#endif

#ifdef LEAP_GUI_VERTEX_UV_2
#define __COPY_UV2(v,o) o.uv2 = v.texcoord2;
#else
#define __COPY_UV2(v,o)
#endif

#ifdef LEAP_GUI_VERTEX_COLORS
#define __COPY_COLORS(v,o) o.color = v.color;
#else
#define __COPY_COLORS(v,o)
#endif

#ifdef LEAP_GUI_TINTING
#define __APPLY_TINT(v,o) o.color *= GetElementTint(elementId);
#else
#define __APPLY_TINT(v,o)
#endif

#define APPLY_BAKED_GUI(v,o)                  \
{                                             \
  __APPLY_BLEND_SHAPES(v,o)                   \
  __APPLY_WARPING(v,o)                        \
  o.vertex = UnityObjectToClipPos(v.vertex);  \
  __COPY_UV0(v,o)                             \
  __COPY_UV1(v,o)                             \
  __COPY_UV2(v,o)                             \
  __COPY_COLORS(v,o)                          \
  __APPLY_TINT(v,o)                           \
}

#define DEFINE_FLOAT_CHANNEL(name) float name[GRAPHIC_MAX]
#define DEFINE_FLOAT4_CHANNEL(name) float4 name[GRAPHIC_MAX]
#define DEFINE_FLOAT4x4_CHANNEL(name) float4x4 name[GRAPHIC_MAX]

#ifdef GUI_ELEMENTS_HAVE_ID
#define getChannel(name) (name[elementId])
#else
#define getChannel(name) 0
#endif
