using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public class NoReferenceTexture2D : NoReferenceField<Texture2D> { }

[Serializable]
public class NoReferenceMesh : NoReferenceField<Mesh> { }

[Serializable]
public class NoReferenceField<T> where T : UnityEngine.Object {

  [SerializeField]
  private string _guid;

  [NonSerialized]
  private T _cachedValue;
  [NonSerialized]
  private string _cachedGuid;

  public T GetValue() {
#if UNITY_EDITOR
    if (string.IsNullOrEmpty(_guid)) {
      _cachedGuid = null;
      _cachedValue = null;
      return null;
    }

    if (_cachedGuid == _guid) {
      return _cachedValue;
    }

    string path = AssetDatabase.GUIDToAssetPath(_guid);
    if (string.IsNullOrEmpty(path)) {
      return null;
    }

    var value = AssetDatabase.LoadAssetAtPath<T>(path);
    if (value != null) {
      _cachedValue = value;
      _cachedGuid = _guid;
    }

    return _cachedValue;
#else
    return null;
#endif
  }
}
