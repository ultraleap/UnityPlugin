using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class ResaveScenesUtility {


  [MenuItem("Assets/Re-Save All Scenes")]
  public static void ReSaveAllScenes() {
    foreach (var scenePath in AssetDatabase.FindAssets("t:Scene")) {
      var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
      EditorSceneManager.SaveScene(scene);
    }
  }
}
