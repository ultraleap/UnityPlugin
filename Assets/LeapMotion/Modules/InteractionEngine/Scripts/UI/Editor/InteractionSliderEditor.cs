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
      bool noRectTransformParent = !(target.transform.parent != null && target.transform.parent.GetComponent<RectTransform>() != null && !(target as InteractionSlider).overrideRectLimits);
      if (!noRectTransformParent) {
        EditorGUILayout.HelpBox("This slider's limits are being controlled by the rect transform in its parent.", MessageType.Info);
      }

      specifyCustomDecorator("horizontalSlideLimits", decorateHorizontalSlideLimits);
      specifyCustomDecorator("verticalSlideLimits",   decorateVerticalSlideLimits);
      specifyCustomDecorator("horizontalSteps", decorateHorizontalSteps);

      // specifyConditionalDrawing(() => noRectTransformParent, "horizontalSlideLimits", "verticalSlideLimits");

      if (!Application.isPlaying) {
        (target as InteractionSlider).RecalculateSliderLimits();
      }

      base.OnInspectorGUI();
    }

    public override bool RequiresConstantRepaint() {
      return true;
    }

    private void decorateHorizontalSlideLimits(SerializedProperty property) {
      EditorGUI.BeginDisabledGroup(target.transform.parent != null && target.transform.parent.GetComponent<RectTransform>() != null && !(target as InteractionSlider).overrideRectLimits);
    }

    private void decorateVerticalSlideLimits(SerializedProperty property) {
      EditorGUI.EndDisabledGroup();
      EditorGUI.BeginDisabledGroup(target.transform.parent != null && target.transform.parent.GetComponent<RectTransform>() != null && !(target as InteractionSlider).overrideRectLimits);
    }

    private void decorateHorizontalSteps(SerializedProperty property) {
      EditorGUI.EndDisabledGroup();
    }

  }
}
