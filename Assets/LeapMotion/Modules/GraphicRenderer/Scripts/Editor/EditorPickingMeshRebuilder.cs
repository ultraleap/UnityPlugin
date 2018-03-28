/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

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
