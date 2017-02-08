using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RendererInfo))]
public class RendererInfoEditor : Editor {

  private Dictionary<Material, bool> _showKeywords = new Dictionary<Material, bool>();

  public override void OnInspectorGUI() {
    base.OnInspectorGUI();

    var renderer = (target as RendererInfo).GetComponent<Renderer>();

    if (renderer == null) {
      EditorGUILayout.HelpBox("Put RendererInfo on a Game Object with a Renderer to see information about that Renderer.", MessageType.Info);
      return;
    }

    foreach (var material in renderer.sharedMaterials) {
      if (material == null) continue;

      if (!_showKeywords.ContainsKey(material)) {
        _showKeywords[material] = false;
      }

      EditorGUILayout.LabelField(material.name, EditorStyles.boldLabel);

      _showKeywords[material] = EditorGUILayout.Foldout(_showKeywords[material], "Shader Keywords");
      if (_showKeywords[material]) {
        EditorGUI.indentLevel++;

        foreach (var keyword in material.shaderKeywords) {
          EditorGUILayout.SelectableLabel(keyword, GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight));
        }

        EditorGUI.indentLevel--;
      }
    }
  }

}
