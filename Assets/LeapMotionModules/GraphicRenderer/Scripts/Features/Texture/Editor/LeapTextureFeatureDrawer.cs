using UnityEditor;
using UnityEngine;

namespace Leap.Unity.GraphicalRenderer {

  [CustomPropertyDrawer(typeof(LeapTextureFeature))]
  public class LeapTextureFeatureDrawer : CustomPropertyDrawerBase {

    protected override void init(SerializedProperty property) {
      base.init(property);

      drawProperty("propertyName");
      drawProperty("channel");
    }
  }
}
