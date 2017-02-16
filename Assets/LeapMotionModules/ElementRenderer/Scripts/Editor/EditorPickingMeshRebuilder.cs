using UnityEngine;
using UnityEditor;

public static class EditorPickingMeshRebuilder {

  [InitializeOnLoadMethod]
  private static void initManager() {
    SceneView.onSceneGUIDelegate += onSceneGui;
  }

  private static void onSceneGui(SceneView view) {
    if (Event.current.type != EventType.MouseDown) {
      return;
    }

    foreach (var gui in UnityEngine.Object.FindObjectsOfType<LeapGui>()) {
      gui.RebuildEditorPickingMeshes();
    }
  }
}
