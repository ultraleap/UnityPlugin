/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;
using UnityEditor;

namespace Leap.Unity.GraphicalRenderer {

  public static class EditorPickingMeshRebuilder {

    [InitializeOnLoadMethod]
    private static void initManager() {
      #if UNITY_2019_1_OR_NEWER
      SceneView.duringSceneGui += onSceneGui;
      #else
      SceneView.onSceneGUIDelegate += onSceneGui;
      #endif
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
