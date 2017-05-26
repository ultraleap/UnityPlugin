/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

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
