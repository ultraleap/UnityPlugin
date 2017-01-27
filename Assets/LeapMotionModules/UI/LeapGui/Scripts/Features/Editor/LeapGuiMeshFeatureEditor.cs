using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Leap.Unity;

[CustomEditor(typeof(LeapGuiMeshFeature))]
public class LeapGuiMeshFeatureEditor : CustomEditorBase {

  protected override void OnEnable() {
    base.OnEnable();

    dontShowScriptField();

    createHorizonalSection("uv0", "uv1");
    createHorizonalSection("uv2", "uv3");

    specifyConditionalDrawing("color", "tint");
  }

}
