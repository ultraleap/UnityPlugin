using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[LeapGuiFeatureName("Blend Shape")]
public class LeapGuiBlendShapeFeature : LeapGuiFeature<LeapGuiBlendShapeData> {
  public const string FEATURE_NAME = LeapGui.FEATURE_PREFIX + "BLEND_SHAPES";

  [SerializeField]
  private BlendShapeSpace _space;

  public BlendShapeSpace space {
    get {
      return _space;
    }
    set {
      //TODO: dirty flags!
      _space = value;
    }
  }

  public enum BlendShapeSpace {
    Local,
    World
  }

#if UNITY_EDITOR
  public override void DrawFeatureEditor(Rect rect, bool isActive, bool isFocused) {
    _space = (BlendShapeSpace)EditorGUI.EnumPopup(rect, "Blend Space", _space);
  }

  public override float GetEditorHeight() {
    return EditorGUIUtility.singleLineHeight;
  }
#endif
}
