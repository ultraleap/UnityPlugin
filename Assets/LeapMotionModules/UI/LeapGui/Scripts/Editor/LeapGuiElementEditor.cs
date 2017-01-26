using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LeapGuiElement))]
public class LeapGuiElementEditor : Editor {

  public override void OnInspectorGUI() {
    base.OnInspectorGUI();

    if (GUILayout.Button("Add Mesh Data")) {
      (target as LeapGuiElement).data.Add(ScriptableObject.CreateInstance<LeapGuiMeshData>());
    }

    foreach (var data in (target as LeapGuiElement).data) {
      Editor.CreateEditor(data).DrawDefaultInspector();
    }
  }
}
