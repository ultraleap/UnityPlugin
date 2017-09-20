using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Leap.Unity {
  using Query;

  public static class ResaveScenesUtility {

    [MenuItem("Assets/Re-Save All Scenes")]
    public static void ReSaveAllScenes() {
      EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

      var paths = AssetDatabase.FindAssets("t:Scene").
                                Query().
                                Select(guid => AssetDatabase.GUIDToAssetPath(guid)).
                                ToList();

      try {
        for (int i = 0; i < paths.Count; i++) {
          var scenePath = paths[i];
          if (EditorUtility.DisplayCancelableProgressBar("Saving Scenes", "Saving " + Path.GetFileNameWithoutExtension(scenePath), i / (float)paths.Count)) {
            return;
          }

          var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
          EditorSceneManager.SaveScene(scene);
        }
      } finally {
        EditorUtility.ClearProgressBar();
      }
    }
  }
}
