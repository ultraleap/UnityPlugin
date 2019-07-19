#include "Assets/LeapMotion/Modules/GraphicRenderer/Resources/GraphicRenderer.cginc"

#ifdef GRAPHIC_RENDERER_ENABLE_CUSTOM_CHANNELS
#define GRAPHICS_HAVE_ID
#endif

/*************************************************************************
 * Movement name:
 *  _ (none)
 *    no movement, position is baked into mesh
 *  GRAPHIC_RENDERER_MOVEMENT_TRANSLATION
 *    graphics can move around in rect space, but no rotation or scaling
 *  GRAPHIC_RENDERER_MOVEMENT_FULL
 *    each graphic has a float4x4 to describe it's motion
 *************************************************************************/

#ifdef GRAPHIC_RENDERER_MOVEMENT_TRANSLATION
#ifndef GRAPHIC_MOVEMENT
#define GRAPHIC_MOVEMENT
#endif
#endif

#ifdef GRAPHIC_RENDERMOVEMENT_FULL
#ifndef GRAPHIC_MOVEMENT
#define GRAPHIC_MOVEMENT
#endif
#endif

#ifdef GRAPHIC_MOVEMENT
#ifndef GRAPHICS_HAVE_ID
#define GRAPHICS_HAVE_ID
#endif
#endif

 /***********************************
  * Space name:
  *  _ (none)
  *    rect space, with no distortion
  *  GRAPHIC_RENDERER_CYLINDRICAL
  *    cylindrical space
  *  GRAPHIC_RENDERER_SPHERICAL
  *    spherical space
  ***********************************/
#ifdef GRAPHIC_MOVEMENT
#ifdef GRAPHIC_RENDERER_CYLINDRICAL
#define GRAPHIC_RENDERER_WARPING
#include "Assets/LeapMotion/Modules/GraphicRenderer/Resources/CylindricalSpace.cginc"

float4 _GraphicRendererCurved_GraphicParameters[GRAPHIC_MAX];

#ifdef GRAPHIC_RENDERER_VERTEX_NORMALS
void ApplyGraphicWarping(inout float4 vert, inout float3 normal, int graphicId) {
  float4 graphicParams = _GraphicRendererCurved_GraphicParameters[graphicId];
  Cylindrical_LocalToWorld(vert.xyz, normal, graphicParams);
}
#else
void ApplyGraphicWarping(inout float4 vert, int graphicId) {
  float4 graphicParams = _GraphicRendererCurved_GraphicParameters[graphicId];
  Cylindrical_LocalToWorld(vert.xyz, graphicParams);
}
#endif
#endif
#endif

#ifdef GRAPHIC_MOVEMENT
#ifdef GRAPHIC_RENDERER_SPHERICAL
#define GRAPHIC_RENDERER_WARPING
#include "Assets/LeapMotion/Modules/GraphicRenderer/Resources/SphericalSpace.cginc"

float4 _GraphicRendererCurved_GraphicParameters[GRAPHIC_MAX];

#ifdef GRAPHIC_RENDERER_VERTEX_NORMALS
void ApplyGraphicWarping(inout float4 vert, inout float3 normal, int graphicId) {
  float4 graphicParams = _GraphicRendererCurved_GraphicParameters[graphicId];
  Spherical_LocalToWorld(vert.xyz, normal, graphicParams);
}
#else
void ApplyGraphicWarping(inout float4 vert, int graphicId) {
  float4 graphicParams = _GraphicRendererCurved_GraphicParameters[graphicId];
  Spherical_LocalToWorld(vert.xyz, graphicParams);
}
#endif
#endif
#endif

//Base-case fallback, rect transformations
#ifdef GRAPHIC_MOVEMENT
#ifndef GRAPHIC_RENDERER_WARPING
#define GRAPHIC_RENDERER_WARPING
float3 _GraphicRendererRect_GraphicPositions[GRAPHIC_MAX];

#ifdef GRAPHIC_RENDERER_VERTEX_NORMALS
void ApplyGraphicWarping(inout float4 vert, inout float3 normal, int graphicId) {
  vert.xyz += _GraphicRendererRect_GraphicPositions[graphicId];
}
#else
void ApplyGraphicWarping(inout float4 vert, int graphicId) {
  vert.xyz += _GraphicRendererRect_GraphicPositions[graphicId];
}
#endif
#endif
#endif

#ifdef GRAPHIC_RENDERER_WARPING
#ifndef GRAPHICS_HAVE_ID
#define GRAPHICS_HAVE_ID
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

float _GraphicRendererBlendShapeAmounts[GRAPHIC_MAX];

void ApplyBlendShapes(inout float4 vert, float3 delta, int graphicId) {
  vert.xyz += delta * _GraphicRendererBlendShapeAmounts[graphicId];
}
#endif

#ifdef GRAPHIC_RENDERER_VERTEX_COLORS
#ifndef GRAPHICS_HAVE_COLOR
#define GRAPHICS_HAVE_COLOR
#endif
#endif

struct appdata_graphic_baked {
  float4 vertex : POSITION;

#ifdef GRAPHIC_RENDERER_VERTEX_NORMALS
  float3 normal : NORMAL;
#endif

#ifdef GRAPHICS_HAVE_ID
  float4 vertInfo : TANGENT;
#endif

#ifdef GRAPHIC_RENDERER_VERTEX_UV_0
  float2 texcoord : TEXCOORD0;
#endif

#ifdef GRAPHIC_RENDERER_VERTEX_UV_1
  float2 texcoord1 : TEXCOORD1;
#endif

#ifdef GRAPHIC_RENDERER_VERTEX_UV_2
  float2 texcoord2 : TEXCOORD2;
#endif

#ifdef GRAPHIC_RENDERER_VERTEX_UV_3
  float2 texcoord3 : TEXCOORD3;
#endif

#ifdef GRAPHIC_RENDERER_VERTEX_COLORS
  float4 color : COLOR;
#endif
};

#ifdef GRAPHIC_RENDERER_VERTEX_NORMALS
#define __V2F_NORMALS float3 normal : NORMAL;
#else
#define __V2F_NORMALS
#endif

#ifdef GRAPHIC_RENDERER_VERTEX_UV_0
#define __V2F_UV0 float2 uv_0 : TEXCOORD0;
#else
#define __V2F_UV0
#endif

#ifdef GRAPHIC_RENDERER_VERTEX_UV_1
#define __V2F_UV1 float2 uv_1 : TEXCOORD1;
#else
#define __V2F_UV1
#endif

#ifdef GRAPHIC_RENDERER_VERTEX_UV_2
#define __V2F_UV2 float2 uv_2 : TEXCOORD2;
#else
#define __V2F_UV2
#endif

#ifdef GRAPHIC_RENDERER_VERTEX_UV_3
#define __V2F_UV3 float2 uv_3 : TEXCOORD3;
#else
#define __V2F_UV3
#endif

#ifdef GRAPHICS_HAVE_COLOR
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
  __V2F_UV3                     \
  __V2F_COLOR

#ifdef GRAPHIC_RENDERER_VERTEX_COLORS
#define SURF_INPUT_GRAPHICAL float4 color : COLOR;
#else
#ifdef GRAPHICS_HAVE_COLOR
#define SURF_INPUT_GRAPHICAL float4 color;
#else
#define SURF_INPUT_GRAPHICAL
#endif
#endif

struct v2f_graphic_baked {
  V2F_GRAPHICAL
};

#ifdef GRAPHICS_HAVE_ID
#define BEGIN_V2F(v) int graphicId = v.vertInfo.w;
#else
#define BEGIN_V2F(v)
#endif

#ifdef GRAPHIC_RENDERER_BLEND_SHAPES
#define __APPLY_BLEND_SHAPES(v) ApplyBlendShapes(v.vertex, v.vertInfo.xyz, graphicId);
#else
#define __APPLY_BLEND_SHAPES(v)
#endif

#ifdef GRAPHIC_RENDERER_WARPING
#ifdef GRAPHIC_RENDERER_VERTEX_NORMALS
#define __APPLY_WARPING(v) ApplyGraphicWarping(v.vertex, v.normal, graphicId);          
#else
#define __APPLY_WARPING(v) ApplyGraphicWarping(v.vertex, graphicId);
#endif
#else
#define __APPLY_WARPING(v)
#endif

#ifdef GRAPHIC_RENDERER_VERTEX_NORMALS
#define __COPY_NORMALS(v,o) o.normal = UnityObjectToWorldNormal(v.normal);
#else
#define __COPY_NORMALS(v,o)
#endif

#ifdef GRAPHIC_RENDERER_VERTEX_UV_0
#define __COPY_UV0(v,o) o.uv_0 = v.texcoord;
#else
#define __COPY_UV0(v,o)
#endif

#ifdef GRAPHIC_RENDERER_VERTEX_UV_1
#define __COPY_UV1(v,o) o.uv_1 = v.texcoord1;
#else
#define __COPY_UV1(v,o)
#endif

#ifdef GRAPHIC_RENDERER_VERTEX_UV_2
#define __COPY_UV2(v,o) o.uv_2 = v.texcoord2;
#else
#define __COPY_UV2(v,o)
#endif

#ifdef GRAPHIC_RENDERER_VERTEX_UV_3
#define __COPY_UV3(v,o) o.uv_3 = v.texcoord3;
#else
#define __COPY_UV3(v,o)
#endif

#ifdef GRAPHIC_RENDERER_VERTEX_COLORS
#define __COPY_COLORS(v,o) o.color = v.color;
#else
#ifdef GRAPHICS_HAVE_COLOR
#define __COPY_COLORS(v,o) o.color = 1;
#else
#define __COPY_COLORS(v,o)
#endif
#endif

#ifdef GRAPHIC_RENDERER_TINTING
#define __APPLY_TINT(v,o) o.color *= GetGraphicTint(graphicId);

#ifdef GRAPHIC_RENDERER_VERTEX_COLORS
#define __APPLY_SURF_TINT(v,o) v.color *= GetGraphicTint(graphicId);
#else
#define __APPLY_SURF_TINT(v,o) o.color = GetGraphicTint(graphicId);
#endif

#else
#define __APPLY_TINT(v,o)
#define __APPLY_SURF_TINT(v,o)
#endif

#define APPLY_BAKED_GRAPHICS(v,o)                \
{                                                \
  __APPLY_BLEND_SHAPES(v)                        \
  __APPLY_WARPING(v)                             \
  o.vertex = UnityObjectToClipPos(v.vertex);     \
  __COPY_NORMALS(v,o)                            \
  __COPY_UV0(v,o)                                \
  __COPY_UV1(v,o)                                \
  __COPY_UV2(v,o)                                \
  __COPY_UV3(v,o)                                \
  __COPY_COLORS(v,o)                             \
  __APPLY_TINT(v,o)                              \
}

#define APPLY_BAKED_GRAPHICS_STANDARD(v,o) \
{                                          \
  __APPLY_BLEND_SHAPES(v);                 \
  __APPLY_WARPING(v);                      \
  __APPLY_SURF_TINT(v,o)                   \
}

#define DEFINE_FLOAT_CHANNEL(name) float name[GRAPHIC_MAX]
#define DEFINE_FLOAT4_CHANNEL(name) float4 name[GRAPHIC_MAX]
#define DEFINE_FLOAT4x4_CHANNEL(name) float4x4 name[GRAPHIC_MAX]

#ifdef GRAPHICS_HAVE_ID
#define getChannel(name) (name[graphicId])
#else
#define getChannel(name) 0
#endif
