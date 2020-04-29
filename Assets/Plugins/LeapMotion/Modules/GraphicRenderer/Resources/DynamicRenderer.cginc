#include "Assets/Plugins/LeapMotion/Modules/GraphicRenderer/Resources/GraphicRenderer.cginc"

#ifdef GRAPHIC_RENDERER_ENABLE_CUSTOM_CHANNELS
#define GRAPHICS_HAVE_ID
#endif

/***********************************
 * Space name:
 *  _ (none)
 *    rect space, with no distortion
 *  GRAPHIC_RENDERER_CYLINDRICAL
 *    cylindrical space
 ***********************************/

#ifdef GRAPHIC_RENDERER_CYLINDRICAL
#define GRAPHIC_RENDERER_WARPING
#include "Assets/Plugins/LeapMotion/Modules/GraphicRenderer/Resources/CylindricalSpace.cginc"

float4 _GraphicRendererCurved_GraphicParameters[GRAPHIC_MAX];
float4x4 _GraphicRenderer_LocalToWorld;

#ifdef GRAPHIC_RENDERER_VERTEX_NORMALS
void ApplyGraphicWarping(inout float4 anchorSpaceVert, inout float3 anchorSpaceNormal, int graphicId) {
  float4 parameters = _GraphicRendererCurved_GraphicParameters[graphicId];

  Cylindrical_LocalToWorld(anchorSpaceVert.xyz, anchorSpaceNormal, parameters);

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
#include "Assets/Plugins/LeapMotion/Modules/GraphicRenderer/Resources/SphericalSpace.cginc"

float4 _GraphicRendererCurved_GraphicParameters[GRAPHIC_MAX];
float4x4 _GraphicRenderer_LocalToWorld;

#ifdef GRAPHIC_RENDERER_VERTEX_NORMALS
void ApplyGraphicWarping(inout float4 anchorSpaceVert, inout float3 anchorSpaceNormal, int graphicId) {
  float4 parameters = _GraphicRendererCurved_GraphicParameters[graphicId];

  Spherical_LocalToWorld(anchorSpaceVert.xyz, anchorSpaceNormal, parameters);

  anchorSpaceVert = mul(_GraphicRenderer_LocalToWorld, anchorSpaceVert);
  anchorSpaceNormal = mul(_GraphicRenderer_LocalToWorld, float4(anchorSpaceNormal, 0));
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

#ifdef GRAPHICS_NEED_ANCHOR_SPACE
float4x4 _GraphicRendererCurved_WorldToAnchor[GRAPHIC_MAX];
#endif

struct appdata_graphic_dynamic {
  float4 vertex : POSITION;

#ifdef GRAPHIC_RENDERER_VERTEX_NORMALS
  float3 normal : NORMAL;
#endif

#ifdef GRAPHICS_HAVE_ID
  float4 vertInfo : TANGENT;
#endif

#ifdef GRAPHIC_RENDERER_VERTEX_UV_0
  float4 texcoord : TEXCOORD0;
#endif

#ifdef GRAPHIC_RENDERER_VERTEX_UV_1
  float4 texcoord1 : TEXCOORD1;
#endif

#ifdef GRAPHIC_RENDERER_VERTEX_UV_2
  float4 texcoord2 : TEXCOORD2;
#endif

#ifdef GRAPHIC_RENDERER_VERTEX_UV_3
  float4 texcoord3 : TEXCOORD3;
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

struct v2f_graphic_dynamic {
  V2F_GRAPHICAL
};

#ifdef GRAPHICS_HAVE_ID
#ifdef GRAPHIC_ID_FROM_UV0
#define BEGIN_V2F(v) int graphicId = v.texcoord.w;
#else
#define BEGIN_V2F(v) int graphicId = v.vertInfo.w;
#endif
#else
#define BEGIN_V2F(v)
#endif

#ifdef GRAPHIC_RENDERER_BLEND_SHAPES
#define __APPLY_BLEND_SHAPES(v) ApplyBlendShapes(v.vertex, v.vertInfo.xyz, graphicId);
#else
#define __APPLY_BLEND_SHAPES(v)
#endif

#ifdef GRAPHICS_NEED_ANCHOR_SPACE
#define __POS_TO_ANCHOR_SPACE(v) v.vertex = mul(unity_ObjectToWorld, v.vertex); \
                                 v.vertex = mul(_GraphicRendererCurved_WorldToAnchor[graphicId], v.vertex);  

#ifdef GRAPHIC_RENDERER_VERTEX_NORMALS
#define __NORMAL_TO_ANCHOR_SPACE(v) v.normal = mul(unity_ObjectToWorld, float4(v.normal.xyz, 0)); \
                                    v.normal = mul(_GraphicRendererCurved_WorldToAnchor[graphicId], float4(v.normal.xyz, 0));
#else
#define __NORMAL_TO_ANCHOR_SPACE(v)       
#endif

#else
#define __POS_TO_ANCHOR_SPACE(v)
#define __NORMAL_TO_ANCHOR_SPACE(v)
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

#ifdef GRAPHICS_NEED_ANCHOR_SPACE
#define __COPY_POSITION(v,o) o.vertex = mul(UNITY_MATRIX_VP, v.vertex);
#define __CORRECT_POS_FOR_SURF(v) v.vertex = mul(unity_WorldToObject, v.vertex);
#ifdef GRAPHIC_RENDERER_VERTEX_NORMALS
#define __CORRECT_NORMALS_FOR_SURF(V) v.normal = mul(unity_WorldToObject, v.normal);
#else
#define __CORRECT_NORMALS_FOR_SURF(V)
#endif
#else
#define __COPY_POSITION(v,o) o.vertex = UnityObjectToClipPos(v.vertex);
#define __CORRECT_POS_FOR_SURF(v)
#define __CORRECT_NORMALS_FOR_SURF(V)
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

#define APPLY_DYNAMIC_GRAPHICS(v,o) \
{                                   \
  __APPLY_BLEND_SHAPES(v)           \
  __POS_TO_ANCHOR_SPACE(v)          \
  __NORMAL_TO_ANCHOR_SPACE(v)       \
  __APPLY_WARPING(v)                \
  __COPY_POSITION(v,o)              \
  __COPY_NORMALS(v,o)               \
  __COPY_UV0(v,o)                   \
  __COPY_UV1(v,o)                   \
  __COPY_UV2(v,o)                   \
  __COPY_UV3(v,o)                   \
  __COPY_COLORS(v,o)                \
  __APPLY_TINT(v,o)                 \
}

#define APPLY_DYNAMIC_GRAPHICS_STANDARD(v,o) \
{                                            \
  __APPLY_BLEND_SHAPES(v);                   \
  __POS_TO_ANCHOR_SPACE(v)                   \
  __NORMAL_TO_ANCHOR_SPACE(v)                \
  __APPLY_WARPING(v);                        \
  __CORRECT_POS_FOR_SURF(v)                  \
  __CORRECT_NORMALS_FOR_SURF(v)              \
  __APPLY_SURF_TINT(v,o)                     \
}

#define DEFINE_FLOAT_CHANNEL(name) float name[GRAPHIC_MAX]
#define DEFINE_FLOAT4_CHANNEL(name) float4 name[GRAPHIC_MAX]
#define DEFINE_FLOAT4x4_CHANNEL(name) float4x4 name[GRAPHIC_MAX]

#ifdef GRAPHICS_HAVE_ID
#define getChannel(name) (name[graphicId])
#else
#define getChannel(name) 0
#endif
