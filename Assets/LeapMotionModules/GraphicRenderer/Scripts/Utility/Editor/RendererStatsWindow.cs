using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Leap.Unity.Query;

namespace Leap.Unity.GraphicalRenderer {

  public class RendererStatsWindow : EditorWindow {

    [MenuItem("Window/Renderer Stats")]
    static void Init() {
      EditorWindow.GetWindow<RendererStatsWindow>().Show();
    }

    private void OnEnable() {
      Selection.selectionChanged += Repaint;
    }

    private void OnDisable() {
      Selection.selectionChanged -= Repaint;
    }

    private void OnGUI() {
      var renderers = Selection.gameObjects.Query().
                                            Select(g => g.GetComponent<Renderer>()).
                                            Where(r => r != null).
                                            ToList();

      if (renderers.Count == 0) {
        EditorGUILayout.HelpBox("Select a renderer to see statistics!", MessageType.Info);
        return;
      }

      foreach (var renderer in renderers) {
        using (new EditorGUI.DisabledGroupScope(true)) {
          EditorGUILayout.ObjectField(renderer, typeof(Renderer), allowSceneObjects: true);
        }

        EditorGUILayout.LabelField("Variant keywords:", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        foreach (var material in renderer.sharedMaterials) {
          if (material == null) continue;

          using (new EditorGUI.DisabledGroupScope(true)) {
            EditorGUILayout.ObjectField(material, typeof(Material), allowSceneObjects: true);
          }

          foreach (var keyword in material.shaderKeywords) {
            EditorGUILayout.LabelField(keyword);
          }
        }

        EditorGUI.indentLevel--;
        EditorGUILayout.Space();
      }
    }
  }
}
