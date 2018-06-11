/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using UnityEditor;

namespace Leap.Unity.Recording {

  [CanEditMultipleObjects]
  [CustomEditor(typeof(SetAnimatorParam))]
  public class SetAnimatorParamEditor : CustomEditorBase<SetAnimatorParam> {

    protected override void OnEnable() {
      base.OnEnable();

      specifyConditionalDrawing("_type", (int)AnimatorControllerParameterType.Bool, "_boolValue");
      specifyConditionalDrawing("_type", (int)AnimatorControllerParameterType.Int, "_intValue");
      specifyConditionalDrawing("_type", (int)AnimatorControllerParameterType.Float, "_floatValue");
    }
  }
}
