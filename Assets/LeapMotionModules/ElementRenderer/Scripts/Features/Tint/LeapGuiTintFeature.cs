using UnityEngine;

[AddComponentMenu("")]
[LeapGuiTag("Tint")]
public class LeapGuiTintFeature : LeapGuiFeature<LeapGuiTintData> {
  public const string FEATURE_NAME = LeapGui.FEATURE_PREFIX + "TINTING";

#if UNITY_EDITOR
  public override void DrawFeatureEditor(Rect rect, bool isActive, bool isFocused) { }

  public override float GetEditorHeight() {
    return 0;
  }
#endif
}

