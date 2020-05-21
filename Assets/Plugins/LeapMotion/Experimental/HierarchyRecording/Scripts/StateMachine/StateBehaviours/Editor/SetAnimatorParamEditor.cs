/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 * Ultraleap proprietary and confidential.                                    *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Ultraleap and you, your company or other organization.             *
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
