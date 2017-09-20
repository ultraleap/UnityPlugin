using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Leap.Unity {
  using Query;

  public static class ResaveUtility {

    [MenuItem("Assets/Save All Scenes\\Assets")]
    public static void SaveAllScenesAndAssets() {
      SaveAllScenes();
      SaveAllAssets();
    }

    public static void SaveAllScenes() {
      EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

      var paths = AssetDatabase.FindAssets("t:Scene").
                                Query().
                                Select(guid => AssetDatabase.GUIDToAssetPath(guid)).
                                ToList();

      try {
        for (int i = 0; i < paths.Count; i++) {
          var scenePath = paths[i];

          if (EditorUtility.DisplayCancelableProgressBar("Saving Scenes", "Saving " + Path.GetFileNameWithoutExtension(scenePath), i / (float)paths.Count)) {
            break;
          }

          var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
          EditorSceneManager.SaveScene(scene);
        }
      } finally {
        EditorUtility.ClearProgressBar();
      }
    }

    [MenuItem("Assets/Save All Assets For Reals")]
    public static void SaveAllAssets() {
      var paths = AssetDatabase.FindAssets("t:ScriptableObject").
                                Query().
                                Concat(AssetDatabase.FindAssets("t:GameObject").
                                                     Query()).
                                Select(guid => AssetDatabase.GUIDToAssetPath(guid)).
                                ToList();

      try {
        for (int i = 0; i < paths.Count; i++) {
          var assetPath = paths[i];

          if (EditorUtility.DisplayCancelableProgressBar("Saving Assets", "Saving " + Path.GetFileNameWithoutExtension(assetPath), i / (float)paths.Count)) {
            break;
          }

          var obj = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
          EditorUtility.SetDirty(obj);
        }
      } finally {
        AssetDatabase.SaveAssets();
        EditorUtility.ClearProgressBar();
      }
    }
  }
}
