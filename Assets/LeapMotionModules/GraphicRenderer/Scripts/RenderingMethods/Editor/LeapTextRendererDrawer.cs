using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Leap.Unity.GraphicalRenderer {

  [CustomPropertyDrawer(typeof(LeapTextRenderer))]
  public class LeapTextRendererDrawer : CustomPropertyDrawerBase {

    protected override void init(SerializedProperty property) {
      base.init(property);

      drawProperty("_font");
      drawProperty("_dynamicPixelsPerUnit");
      drawProperty("_useColor");
      drawPropertyConditionally("_globalTint", "_useColor");
      drawProperty("_shader");
      drawProperty("_scale");
    }

  }
}

