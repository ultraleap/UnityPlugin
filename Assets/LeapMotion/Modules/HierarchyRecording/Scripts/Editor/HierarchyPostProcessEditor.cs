using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Leap.Unity;

[CustomEditor(typeof(HierarchyPostProcess))]
public class HierarchyPostProcessEditor : CustomEditorBase<HierarchyPostProcess> {

  public override void OnInspectorGUI() {
    base.OnInspectorGUI();

    bool isPrefab = PrefabUtility.GetPrefabType(target) == PrefabType.Prefab;
    EditorGUI.BeginDisabledGroup(isPrefab);

    if (GUILayout.Button(new GUIContent("Build Playback Prefab",
                                        isPrefab ? "Draw this object into the scene "
                                                 + "before converting its raw recording "
                                                 + "data into AnimationClip data."
                                                 : ""))) {
      target.BuildPlaybackPrefab();
    }

    EditorGUI.EndDisabledGroup();
  }
}
