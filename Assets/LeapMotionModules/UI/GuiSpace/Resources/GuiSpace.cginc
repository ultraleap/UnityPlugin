
/* Feature name:
 *  _ (none)
 *    rect space, with no distortion
 *  GUI_SPACE_ALL
 *    all spaces, controlled with a property value.  Very slow but flexible.
 *  GUI_SPACE_CYLINDRICAL_CONSTANT_WIDTH
 *    cylindrical space with a constant width mapping
 *  GUI_SPACE_CYLINDRICAL_ANGULAR
 *    cylindrical space with an angular mapping
 */

uniform float4x4 _GuiSpaceTransform;

#ifdef GUI_SPACE_ALL
uniform int _GuiSpaceSelection;
#define GUI_SPACE_CYLINDRICAL_CONSTANT_WIDTH   1
#define GUI_SPACE_CYLINDRICAL_ANGULAR          2
#endif

uniform float4 _GuiSpaceParams; 

void applyCylindricalSpaceConstantWidth(inout float4 vert) {
  float theta = vert.x / vert.z;
  vert.x = sin(theta) * vert.z;
  vert.z = cos(theta) * vert.z;
}

void applyCylindricalSpaceAngular(inout float4 vert) {

}

// Takes an object space vertex and converts it to a clip space vertex
float4 GuiVertToClipSpace(float4 vert) {
	vert = mul(unity_ObjectToWorld, vert);

  /*
#ifdef GUI_SPACE_ALL
  if (_GuiSpaceSelection == GUI_SPACE_CYLINDRICAL_CONSTANT_WIDTH) {
    applyCylindricalSpaceConstantWidth(vert);
  } else if (_GuiSpaceSelection == GUI_SPACE_CYLINDRICAL_ANGULAR) {
    applyCylindricalSpaceAngular(vert);
  }
#elseifdef GUI_SPACE_CYLINDRICAL_CONSTANT_WIDTH
  applyCylindricalSpaceConstantWidth(vert);
#elseifdef GUI_SPACE_CYLINDRICAL_ANGULAR
  applyCylindricalSpaceAngular(vert);
#endif
*/

  applyCylindricalSpaceConstantWidth(vert);

	return mul(UNITY_MATRIX_VP, vert);
}
