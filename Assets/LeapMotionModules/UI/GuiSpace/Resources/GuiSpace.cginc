#include "UnityCG.cginc"

/************************************************************************* 
 * Movement name:
 *  _ (none)
 *    no movement, position is baked into mesh
 *  GUI_ELEMENT_MOVEMENT_TRANSLATION
 *    elements can move around in rect space, but no rotation or scaling
 *  GUI_ELEMENT_MOVEMENT_FULL
 *    each element has a float4x4 to describe it's motion
 *************************************************************************/

#ifdef GUI_ELEMENT_MOVEMENT_TRANSLATION
#define GUI_ELEMENTS_HAVE_MOTION
void ApplyElementMotion(inout float4 vert, int elementId) { }
#endif

#ifdef GUI_ELEMENT_MOVEMENT_FULL
#define GUI_ELEMENTS_HAVE_MOTION
float4x4 _GuiElementMovement_Transform[ELEMENT_MAX];

void ApplyElementMotion(inout float4 vert, int elementId) {
  vert = mul(_GuiElementMovement_Transform[elementId], vert);
}
#endif

#ifdef GUI_ELEMENTS_HAVE_MOTION
#define GUI_ELEMENTS_HAVE_ID
#endif

/***********************************
 * Space name:
 *  _ (none)
 *    rect space, with no distortion
 *  GUI_SPACE_CYLINDRICAL
 *    cylindrical space
 ***********************************/

#ifdef GUI_SPACE_CYLINDRICAL
#define GUI_ELEMENTS_HAVE_ID
float _GuiSpaceCylindrical_ReferenceRadius;
float3 _GuiSpaceCylindrical_ParentPosition[ELEMENT_MAX];

void ApplyGuiWarping(inout float4 vert, int elementId) {
  float3 parentPos = _GuiSpaceCylindrical_ParentPosition[elementId];

  parentPos.x += vert.x / parentPos.z;
  parentPos.yz += vert.yz;

  vert.x = sin(parentPos.x) * parentPos.z;
  vert.y = parentPos.y;
  vert.z = cos(parentPos.x) * parentPos.z - _GuiSpaceCylindrical_ReferenceRadius;
}
#endif

/***********************************
 * Feature name:
 *  _ (none)
 *    no runtime tinting, base color only
 *  GUI_SPACE_TINTING
 *    runtime tinting on a per-element basis
 ***********************************/

#ifdef GUI_SPACE_TINTING
#define GUI_ELEMENTS_HAVE_ID
float _GuiSpace_Tints[ELEMENT_MAX];

void ApplyGuiTinting(inout float4 color, int elementId) {
  color *= _GuiSpace_Tints[elementId];
}
#endif

/***********************************
 * Feature name:
 *  _ (none)
 *    no runtime blend shapes
 *  GUI_SPACE_BLEND_SHAPES
 *    runtime application of blend shapes on a per-element basis
 ***********************************/

#ifdef GUI_SPACE_BLEND_SHAPES
#define GUI_ELEMENTS_HAVE_ID
float3 _GuiSpace_BlendShapeAmmounts[ELEMENT_MAX];

void ApplyBlendShapes(inout float4 vert, int elementId) {
  color.xyz += _GuiSpace_BlendShapeAmmounts[elementId];
}
#endif

#define ELEMENT_MAX 32

struct appdata_gui {
  float4 vertex : POSITION;

#ifdef GUI_SPACE_NORMALS
  float4 normal : NORMAL;
#endif

#ifdef GUI_SPACE_UV_0
  float2 uv0 : TEXCOORD0;
#endif

#ifdef GUI_SPACE_UV_1
  float2 uv1 : TEXCOORD2;
#endif

#ifdef GUI_SPACE_UV_2
  float2 uv2 : TEXCOORD3;
#endif

#ifdef GUI_ELEMENTS_HAVE_ID
  float4 vertInfo : TEXCOORD3;
#endif

#ifdef GUI_SPACE_VERTEX_COLORS
  float4 color : COLOR;
#endif
};

struct v2f_gui {
  float4 vertex : SV_POSITION;

#ifdef GUI_SPACE_NORMALS
  float4 normal : NORMAL;
#endif

#ifdef GUI_SPACE_UV_0
  float2 uv0 : TEXCOORD0;
#endif

#ifdef GUI_SPACE_UV_1
  float2 uv1 : TEXCOORD2;
#endif

#ifdef GUI_SPACE_UV_2
  float2 uv2 : TEXCOORD3;
#endif

#ifdef GUI_SPACE_VERTEX_COLORS
  float4 color : COLOR;
#endif
};


// Takes an object space vertex and converts it to a clip space vertex
v2f_gui ApplyGuiSpace(appdata_gui v) {
  int elementId = v.vertInfo.w;

#ifdef GUI_ELEMENTS_HAVE_MOTION
  ApplyElementMotion(v.vertex, elementId);
  ApplyGuiWarping(v.vertex, elementId);
#endif

  v2f_gui o;
  o.vertex = UnityObjectToClipPos(v.vertex);

#ifdef GUI_SPACE_NORMALS
  o.normal = v.normal; //TODO?????
#endif

#ifdef GUI_SPACE_UV_0
  o.uv0 = v.uv0;
#endif

#ifdef GUI_SPACE_UV_1
  o.uv1 = v.uv1;
#endif

#ifdef GUI_SPACE_UV_2
  o.uv2 = v.uv2;
#endif

#ifdef GUI_SPACE_VERTEX_COLORS
  o.color = v.color;
#endif
}
