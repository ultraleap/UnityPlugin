using UnityEngine;

namespace Leap.Unity.GraphicalRenderer {

  [AddComponentMenu("")]
  [LeapGraphicTag("Tint")]
  public class LeapRuntimeTintFeature : LeapGraphicFeature<LeapRuntimeTintData> {
    public const string FEATURE_NAME = LeapGraphicRenderer.FEATURE_PREFIX + "TINTING";

#if UNITY_EDITOR
    public override void DrawFeatureEditor(Rect rect, bool isActive, bool isFocused) { }

    public override float GetEditorHeight() {
      return 0;
    }
#endif
  }
}
