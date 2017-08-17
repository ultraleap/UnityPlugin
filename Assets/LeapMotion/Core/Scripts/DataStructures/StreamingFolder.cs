using System;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity {

  [Serializable]
  public class StreamingFolder : AssetFolder, ISerializationCallbackReceiver {

    [SerializeField]
    private string _relativePath;

    public override string Path {
      get {
        return System.IO.Path.Combine(Application.streamingAssetsPath, _relativePath);
      }
      set {
        throw new NotImplementedException();
      }
    }

    public void OnAfterDeserialize() { }

    public void OnBeforeSerialize() {
#if UNITY_EDITOR
      string fullFolder = System.IO.Path.GetFullPath(AssetDatabase.GetAssetPath(_assetFolder));
      if (string.IsNullOrEmpty(fullFolder)) {
        _relativePath = null;
      } else {
        _relativePath = Utils.MakeRelativePath(Application.streamingAssetsPath, fullFolder);
        _relativePath = string.Join(System.IO.Path.DirectorySeparatorChar.ToString(),
                                    _relativePath.Split(System.IO.Path.DirectorySeparatorChar).Skip(1).ToArray());
      }
#endif
    }
  }
}
