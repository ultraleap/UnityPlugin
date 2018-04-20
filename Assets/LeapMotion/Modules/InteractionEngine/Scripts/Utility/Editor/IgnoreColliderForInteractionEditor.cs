/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEditor;
using UnityEngine;

namespace Leap.Unity.Interaction {

  [CustomEditor(typeof(IgnoreColliderForInteraction))]
  public class IgnoreColliderForInteractionEditor
    : CustomEditorBase<IgnoreColliderForInteraction> {


    public override void OnInspectorGUI() {
      base.OnInspectorGUI();

      EditorGUILayout.HelpBox(
        "Causes any Colliders located on the same GameObject to be ignored by the "
      + "Interaction Engine. Does not affect parents or children. It is recommended that "
      + "you use this component only with trigger colliders.",
        MessageType.None);
    }

  }

}
