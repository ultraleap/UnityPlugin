using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Leap.Unity;

[CanEditMultipleObjects]
[CustomEditor(typeof(LeapGuiBlendShapeData))]
public class LeapGuiBlendShapeDataEditor : CustomEditorBase {
  protected override void OnEnable() {
    base.OnEnable();
    dontShowScriptField();

    specifyConditionalDrawing("_type", (int)LeapGuiBlendShapeData.BlendShapeType.Translation, "_translation");
    specifyConditionalDrawing("_type", (int)LeapGuiBlendShapeData.BlendShapeType.Rotation, "_rotation");
    specifyConditionalDrawing("_type", (int)LeapGuiBlendShapeData.BlendShapeType.Scale, "_scale");
    specifyConditionalDrawing("_type", (int)LeapGuiBlendShapeData.BlendShapeType.Mesh, "_mesh");
  }
}
