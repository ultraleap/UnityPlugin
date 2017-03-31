using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Leap.Unity.Attributes;

namespace Leap.Unity.GraphicalRenderer {

  [AddComponentMenu("")]
  [LeapGraphicTag("Blend Shape")]
  public class LeapBlendShapeFeature : LeapGraphicFeature<LeapBlendShapeData> {
    public const string FEATURE_NAME = LeapGraphicRenderer.FEATURE_PREFIX + "BLEND_SHAPES";

    [EditTimeOnly]
    [SerializeField]
    private BlendShapeSpace _space;

    public BlendShapeSpace space {
      get {
        return _space;
      }
      set {
        isDirty = true;
        _space = value;
      }
    }

    public enum BlendShapeSpace {
      Local,
      World
    }

    public override SupportInfo GetSupportInfo(LeapGraphicGroup group) {
      if (!group.renderingMethod.IsValidGraphic<LeapMeshGraphicBase>()) {
        return SupportInfo.Error("Blend shapes require a renderer that supports mesh graphics.");
      } else {
        return SupportInfo.FullSupport();
      }
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
}
