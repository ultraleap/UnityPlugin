/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
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
