using UnityEditor;

namespace Leap.Unity.GraphicalRenderer {

  [CustomPropertyDrawer(typeof(LeapBlendShapeFeature))]
  public class LeapBlendShapeFeatureDrawer : CustomPropertyDrawerBase {

    protected override void init(SerializedProperty property) {
      base.init(property);

      drawCustom(rect => {
        EditorGUI.LabelField(rect, "Blend Shape");
      }, EditorGUIUtility.singleLineHeight);
    }
  }
}
