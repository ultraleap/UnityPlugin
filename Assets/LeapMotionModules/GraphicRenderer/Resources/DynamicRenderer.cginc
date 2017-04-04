#include "Assets/LeapMotionModules/GraphicRenderer/Resources/GraphicRenderer.cginc"

/***********************************
 * Space name:
 *  _ (none)
 *    rect space, with no distortion
 *  GRAPHIC_RENDERER_CYLINDRICAL
 *    cylindrical space
 ***********************************/

#ifdef GRAPHIC_RENDERER_CYLINDRICAL
#define GRAPHIC_RENDERER_WARPING
#include "Assets/LeapMotionModules/GraphicRenderer/Resources/CylindricalSpace.cginc"

float4 _GraphicRendererCurved_GraphicParameters[GRAPHIC_MAX];
float4x4 _GraphicRenderer_LocalToWorld;

#ifdef GRAPHIC_RENDERER_VERTEX_NORMALS
void ApplyGraphicWarping(inout float4 anchorSpaceVert, inout float4 anchorSpaceNormal, int graphicId) {
  float4 parameters = _GraphicRendererCurved_GraphicParameters[graphicId];

  Cylindrical_LocalToWorld(anchorSpaceVert.xyz, anchorSpaceNormal.xyz, parameters);

  anchorSpaceVert = mul(_GraphicRenderer_LocalToWorld, anchorSpaceVert);
  anchorSpaceNormal = mul(_GraphicRenderer_LocalToWorld, float4(anchorSpaceNormal.xyz, 0));
}
#else
void ApplyGraphicWarping(inout float4 anchorSpaceVert, int graphicId) {
  float4 parameters = _GraphicRendererCurved_GraphicParameters[graphicId];

  Cylindrical_LocalToWorld(anchorSpaceVert.xyz, parameters);

  anchorSpaceVert = mul(_GraphicRenderer_LocalToWorld, anchorSpaceVert);
}
#endif
#endif

#ifdef GRAPHIC_RENDERER_SPHERICAL
#define GRAPHIC_RENDERER_WARPING
#include "Assets/LeapMotionModules/GraphicRenderer/Resources/SphericalSpace.cginc"

float4 _GraphicRendererCurved_GraphicParameters[GRAPHIC_MAX];
float4x4 _GraphicRenderer_LocalToWorld;

#ifdef GRAPHIC_RENDERER_VERTEX_NORMALS
void ApplyGraphicWarping(inout float4 anchorSpaceVert, inout float4 anchorSpaceNormal, int graphicId) {
  float4 parameters = _GraphicRendererCurved_GraphicParameters[graphicId];

  Spherical_LocalToWorld(anchorSpaceVert.xyz, anchorSpaceNormal.xyz, parameters);

  anchorSpaceVert = mul(_GraphicRenderer_LocalToWorld, anchorSpaceVert);
  anchorSpaceNormal = mul(_GraphicRenderer_LocalToWorld, float4(anchorSpaceNormal.xyz, 0));
}
#else
void ApplyGraphicWarping(inout float4 anchorSpaceVert, int graphicId) {
  float4 parameters = _GraphicRendererCurved_GraphicParameters[graphicId];

  Spherical_LocalToWorld(anchorSpaceVert.xyz, parameters);

  anchorSpaceVert = mul(_GraphicRenderer_LocalToWorld, anchorSpaceVert);
}
#endif
#endif

#ifdef GRAPHIC_RENDERER_WARPING
#ifndef GRAPHICS_HAVE_ID
#define GRAPHICS_HAVE_ID
#endif
#ifndef GRAPHICS_NEED_ANCHOR_SPACE
#define GRAPHICS_NEED_ANCHOR_SPACE
#endif
#endif

/***********************************
 * Feature name:
 *  _ (none)
 *    no runtime tinting, base color only
 *  GRAPHIC_RENDERER_TINTING
 *    runtime tinting on a per-graphic basis
 ***********************************/

#ifdef GRAPHIC_RENDERER_TINTING
#ifndef GRAPHICS_HAVE_ID
#define GRAPHICS_HAVE_ID
#endif
#ifndef GRAPHICS_HAVE_COLOR
#define GRAPHICS_HAVE_COLOR
#endif

float4 _GraphicRendererTints[GRAPHIC_MAX];

float4 GetGraphicTint(int graphicId) {
  return _GraphicRendererTints[graphicId];
}
#endif

/***********************************
 * Feature name:
 *  _ (none)
 *    no runtime blend shapes
 *  GRAPHIC_RENDERER_BLEND_SHAPES
 *    runtime application of blend shapes on a per-graphic basis
 ***********************************/

#ifdef GRAPHIC_RENDERER_BLEND_SHAPES
#ifndef GRAPHICS_HAVE_ID
#define GRAPHICS_HAVE_ID
#endif
#ifndef GRAPHICS_NEED_ANCHOR_SPACE
#define GRAPHICS_NEED_ANCHOR_SPACE
#endif

float _GraphicRendererBlendShapeAmounts[GRAPHIC_MAX];

void ApplyBlendShapes(inout float4 vert, float4 uv3, int graphicId) {
  vert.xyz += uv3.xyz * _GraphicRendererBlendShapeAmounts[graphicId];
}
#endif

#ifdef GRAPHIC_RENDERER_VERTEX_COLORS
#ifndef GRAPHICS_HAVE_COLOR
#define GRAPHICS_HAVE_COLOR
#endif
#endif

#ifdef GRAPHICS_NEED_ANCHOR_SPACE
float4x4 _GraphicRendererCurved_WorldToAnchor[GRAPHIC_MAX];
#endif

struct appdata_graphic_dynamic {
  float4 vertex : POSITION;

#ifdef GRAPHIC_RENDERER_VERTEX_NORMALS
  float4 normal : NORMAL;
#endif

#ifdef GRAPHIC_RENDERER_VERTEX_UV_0
  float4 uv0 : TEXCOORD0;
#endif

#ifdef GRAPHIC_RENDERER_VERTEX_UV_1
  float4 uv1 : TEXCOORD1;
#endif

#ifdef GRAPHIC_RENDERER_VERTEX_UV_2
  float4 uv2 : TEXCOORD2;
#endif

#ifdef GRAPHICS_HAVE_ID
  float4 vertInfo : TEXCOORD3;
#endif

#ifdef GRAPHIC_RENDERER_VERTEX_COLORS
  float4 color : COLOR;
#endif
};

struct v2f_graphic_dynamic {
  float4 vertex : SV_POSITION;

#ifdef GRAPHIC_RENDERER_VERTEX_NORMALS
  float4 normal : NORMAL;
#endif

#ifdef GRAPHIC_RENDERER_VERTEX_UV_0
  float4 uv0 : TEXCOORD0;
#endif

#ifdef GRAPHIC_RENDERER_VERTEX_UV_1
  float4 uv1 : TEXCOORD2;
#endif

#ifdef GRAPHIC_RENDERER_VERTEX_UV_2
  float4 uv2 : TEXCOORD3;
#endif

#ifdef GRAPHICS_HAVE_COLOR
  float4 color : COLOR;
#endif
};

v2f_graphic_dynamic ApplyDynamicGraphics(appdata_graphic_dynamic v) {
#ifdef GRAPHICS_HAVE_ID
#ifdef GRAPHIC_ID_FROM_UV0
  int graphicId = v.uv0.w;
#else
  int graphicId = v.vertInfo.w;
#endif
#endif

#ifdef GRAPHICS_NEED_ANCHOR_SPACE
  v.vertex = mul(unity_ObjectToWorld, v.vertex);
  v.vertex = mul(_GraphicRendererCurved_WorldToAnchor[graphicId], v.vertex);
#ifdef GRAPHIC_RENDERER_VERTEX_NORMALS
  v.normal = mul(unity_ObjectToWorld, float4(v.normal.xyz, 0));
  v.normal = mul(_GraphicRendererCurved_WorldToAnchor[graphicId], float4(v.normal.xyz, 0));
#endif
#endif

#ifdef GRAPHIC_RENDERER_BLEND_SHAPES
  ApplyBlendShapes(v.vertex, v.vertInfo, graphicId);
#endif

#ifdef GRAPHIC_RENDERER_WARPING
#ifdef GRAPHIC_RENDERER_VERTEX_NORMALS
  ApplyGraphicWarping(v.vertex, v.normal, graphicId);
#else
  ApplyGraphicWarping(v.vertex, graphicId);
#endif
#endif

  v2f_graphic_dynamic o;
#ifdef GRAPHICS_NEED_ANCHOR_SPACE
  o.vertex = mul(UNITY_MATRIX_VP, v.vertex);
#if GRAPHIC_RENDERER_VERTEX_NORMALS
  o.normal = v.normal;
#endif
#else
  o.vertex = UnityObjectToClipPos(v.vertex);
#if GRAPHIC_RENDERER_VERTEX_NORMALS
  o.normal = UnityObjectToWorldNormal(v.normal);
#endif
#endif

#ifdef GRAPHIC_RENDERER_VERTEX_UV_0
  o.uv0 = v.uv0;
#endif

#ifdef GRAPHIC_RENDERER_TINTING
  o.color = GetGraphicTint(graphicId);
#ifdef GRAPHIC_RENDERER_VERTEX_COLORS
  o.color *= v.color;
#endif
#else
#ifdef GRAPHIC_RENDERER_VERTEX_COLORS
  o.color = v.color;
#endif
#endif

  return o;
}
