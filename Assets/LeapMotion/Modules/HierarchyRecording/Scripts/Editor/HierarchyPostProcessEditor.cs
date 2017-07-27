using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Leap.Unity;

[CustomEditor(typeof(HierarchyPostProcess))]
public class HierarchyPostProcessEditor : CustomEditorBase<HierarchyPostProcess> {

  public override void OnInspectorGUI() {
    base.OnInspectorGUI();

    if (GUILayout.Button("Build Playback Prefab")) {
      target.BuildPlaybackPrefab();
    }
  }



}
