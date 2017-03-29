using System.IO;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Leap.Unity.Query;

public class SceneTiedAsset : ScriptableObject {

  [SerializeField]
  private int _referenceId;

#if UNITY_EDITOR
  public bool isSavedAsset {
    get {
      return _referenceId != 0;
    }
  }
#endif

  protected SceneTiedAsset() { }

  protected virtual void OnAssetSaved() { }

#if UNITY_EDITOR
  public static bool CreateOrSave<T>(ref T t,
                                     Scene scene,
                                     string folderSuffix,
                                     string assetName,
                                     int referenceId) where T : SceneTiedAsset {
    bool didCreate = false;
    string assetFolder = null;

    Assert.AreNotEqual(referenceId, 0);

    if (scene.IsValid() && !string.IsNullOrEmpty(scene.path)) {
      string sceneDirectory = Path.GetDirectoryName(scene.path);
      string folderName = Path.GetFileNameWithoutExtension(scene.path) + folderSuffix;
      assetFolder = Path.Combine(sceneDirectory, folderName);
    }

    if (t == null) {
      if (assetFolder != null && AssetDatabase.IsValidFolder(assetFolder)) {
        string filter = assetName + " t:" + typeof(T).Name;
        string[] folder = new string[] { assetFolder };
        string[] guids = AssetDatabase.FindAssets(filter, folder);

        //Use the first asset that has a matching asset id
        t = guids.Query().Select(g => AssetDatabase.GUIDToAssetPath(g)).
                          Where(p => !string.IsNullOrEmpty(p)).
                          Select(p => AssetDatabase.LoadAssetAtPath<T>(p)).
                          NonNull().
                          FirstOrDefault(a => a._referenceId == referenceId);
      }

      //If t is still null, just create it!
      if (t == null) {
        t = CreateInstance<T>();
        t.name = assetName;
        t.hideFlags = HideFlags.HideAndDontSave;
        didCreate = true;
      }
    }

    if (assetFolder != null && !t.isSavedAsset) {
      if (!AssetDatabase.IsValidFolder(assetFolder)) {
        AssetDatabase.CreateFolder(Path.GetDirectoryName(assetFolder), Path.GetFileName(assetFolder));
      }

      string assetPath = Path.Combine(assetFolder, assetName + ".asset");
      assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

      t.hideFlags = HideFlags.None;
      t._referenceId = referenceId;
      AssetDatabase.CreateAsset(t, assetPath);
      t.OnAssetSaved();
      AssetDatabase.SaveAssets();
    }

    return didCreate;
  }

  public static void Delete<T>(ref T t) where T : SceneTiedAsset {
    string path = AssetDatabase.GetAssetPath(t);

    DestroyImmediate(t, allowDestroyingAssets: true);

    if (!string.IsNullOrEmpty(path)) {
      AssetDatabase.DeleteAsset(path);
    }

    t = null;
  }
#endif
}
