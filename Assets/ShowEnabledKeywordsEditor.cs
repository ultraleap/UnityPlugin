using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ShowEnabledKeywords))]
public class ShowEnabledKeywordsEditor : Editor {

  public override void OnInspectorGUI() {
    base.OnInspectorGUI();

    var gameObject = (target as ShowEnabledKeywords).gameObject;
    var mat = gameObject.GetComponent<Renderer>().sharedMaterial;

    foreach (var keyword in mat.shaderKeywords) {
      EditorGUILayout.LabelField(keyword);
    }
  }

}
