/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEditor;
using Leap.Unity;
using Leap.Unity.Attachments;

[CustomEditor (typeof(Transition))]
public class TransitionEditor : CustomEditorBase {
  protected override void OnEnable() {
    base.OnEnable();

    specifyConditionalDrawing("AnimatePosition",
                              "OnPosition",
                              "OffPosition",
                              "PositionCurve");
    specifyConditionalDrawing("AnimateRotation",
                          "OnRotation",
                          "OffRotation",
                          "RotationCurve");
    specifyConditionalDrawing("AnimateScale",
                              "OnScale",
                              "OffScale",
                              "ScaleCurve");
    specifyConditionalDrawing("AnimateColor",
                              "TransitionColor",
                              "ColorShaderPropertyName",
                              "ColorCurve");
  }

}
