using UnityEngine;
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
