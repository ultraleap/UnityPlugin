using UnityEngine;
using UnityEditor;

namespace Leap.Unity.GraphicalRenderer {

  public static class EditorPickingMeshRebuilder {

    [InitializeOnLoadMethod]
    private static void initManager() {
      SceneView.onSceneGUIDelegate += onSceneGui;
    }

    private static void onSceneGui(SceneView view) {
      if (Event.current.type != EventType.MouseDown) {
        return;
      }

      foreach (var graphicRenderer in Object.FindObjectsOfType<LeapGraphicRenderer>()) {
        graphicRenderer.editor.RebuildEditorPickingMeshes();
      }
    }
  }
}
