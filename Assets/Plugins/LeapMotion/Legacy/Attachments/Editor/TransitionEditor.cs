/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 * Ultraleap proprietary and confidential.                                    *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
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
