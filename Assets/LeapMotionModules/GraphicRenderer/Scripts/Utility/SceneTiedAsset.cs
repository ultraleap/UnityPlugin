using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Leap.Unity.Query;

namespace Leap.Unity.GraphicalRenderer {


  public class SceneTiedAsset : ScriptableObject {

    [SerializeField]
    private bool _isSavedAsset;

#if UNITY_EDITOR
    public bool isSavedAsset {
      get {
        return _isSavedAsset;
      }
    }
#endif

    protected SceneTiedAsset() { }

    protected virtual void OnAssetSaved() { }

    private static class AssetRef<T> {
      private static Dictionary<Object, T> _to = new Dictionary<Object, T>();
      private static Dictionary<T, Object> _from = new Dictionary<T, Object>();

      public static bool TryGet(Object key, out T value) {
        return _to.TryGetValue(key, out value);
      }

      public static bool TryGet(T key, out Object value) {
        return _from.TryGetValue(key, out value);
      }

      public static void Put(Object key, T value) {
        Assert.IsTrue(key != null);
        Assert.IsTrue(value != null);
        _to[key] = value;
        _from[value] = key;
      }
    }

#if UNITY_EDITOR
    public static bool CreateOrSave<T>(GameObject holder,
                                       ref T t,
                                       string folderSuffix,
                                       string assetName) where T : SceneTiedAsset {
      bool didChange = false;
      string assetFolder = null;
      var scene = holder.scene;

      if (scene.IsValid() && !string.IsNullOrEmpty(scene.path)) {
        string sceneDirectory = Path.GetDirectoryName(scene.path);
        string folderName = Path.GetFileNameWithoutExtension(scene.path) + folderSuffix;
        assetFolder = Path.Combine(sceneDirectory, folderName);
      }

      if (t == null) {
        //Try to get the asset from the asset ref map
        AssetRef<T>.TryGet(holder, out t);

        //If t is still null, just create it!
        if (t == null) {
          t = CreateInstance<T>();
          t.name = assetName;
          t.hideFlags = HideFlags.HideAndDontSave;

          AssetRef<T>.Put(holder, t);
        }

        didChange = true;
      } else {
        Object otherHolder;
        if (AssetRef<T>.TryGet(t, out otherHolder)) {
          if (otherHolder != holder) {
            t = Instantiate(t);
            t._isSavedAsset = false;
            didChange = true;

            AssetRef<T>.Put(holder, t);
          }
        }
      }

      if (assetFolder != null && !t.isSavedAsset) {
        if (!AssetDatabase.IsValidFolder(assetFolder)) {
          AssetDatabase.CreateFolder(Path.GetDirectoryName(assetFolder), Path.GetFileName(assetFolder));
        }

        string assetPath = Path.Combine(assetFolder, assetName + ".asset");
        assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

        t.hideFlags = HideFlags.None;
        t._isSavedAsset = true;
        AssetDatabase.CreateAsset(t, assetPath);
        t.OnAssetSaved();
        AssetDatabase.SaveAssets();
      }

      return didChange;
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
}
