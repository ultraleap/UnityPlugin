using System;
using UnityEngine;
using UnityObject = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity {

  /// <summary>
  /// A convenient serializable representation of an asset folder.
  /// Only useful for editor scripts since asset folder structure
  /// is not preserved for builds.  The asset folder struct itself is
  /// still available at runtime for serialization ease, but the Path
  /// property will not be available.
  /// </summary>
  [Serializable]
  public struct AssetFolder {

    [SerializeField]
    private UnityObject _assetFolder;

#if UNITY_EDITOR
    /// <summary>
    /// Gets or sets the folder path.  This path will always be a path
    /// relative to the asset folder, and matches the format expected and
    /// returned by AssetDatabase.
    /// </summary>
    public string Path {
      get {
        if (_assetFolder != null) {
          return AssetDatabase.GetAssetPath(_assetFolder);
        } else {
          return null;
        }
      }
      set {
        _assetFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(value);
      }
    }
#endif
  }
}
