using UnityEditor;
using UnityEngine;

namespace Leap.Unity.Interaction {

  [CanEditMultipleObjects]
  [CustomEditor(typeof(InteractionSlider), editorForChildClasses: true)]
  public class InteractionSliderEditor : InteractionButtonEditor {
    public override void OnInspectorGUI() {
      bool noRectTransformParent = !(target.transform.parent != null && target.transform.parent.GetComponent<RectTransform>() != null);
      specifyConditionalDrawing(() => noRectTransformParent, "horizontalSlideLimits", "verticalSlideLimits");
      if (!noRectTransformParent) {
        EditorGUILayout.HelpBox("This slider's limits are being controlled by the rect transform in its parent.", MessageType.Info);
      }
      base.OnInspectorGUI();
    }
  }
}
