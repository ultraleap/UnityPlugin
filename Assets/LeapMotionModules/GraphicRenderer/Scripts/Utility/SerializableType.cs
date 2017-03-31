using System;
using System.Reflection;
using UnityEngine;

[Serializable]
public struct SerializableType : ISerializationCallbackReceiver {

  [SerializeField, HideInInspector]
  private Type _type;

  [SerializeField, HideInInspector]
  private string _fullName;

  private static Assembly _cachedAssembly = null;
  private static Assembly _assembly {
    get {
      if (_cachedAssembly == null) {
        _cachedAssembly = Assembly.GetAssembly(typeof(LeapGraphicRenderer));
      }
      return _cachedAssembly;
    }
  }

  public void OnAfterDeserialize() {
    if (!string.IsNullOrEmpty(_fullName)) {
      _type = _assembly.GetType(_fullName);
    } else {
      _type = null;
    }
  }

  public void OnBeforeSerialize() {
    if (_type != null) {
      _fullName = _type.FullName;
    }
  }

  public static implicit operator Type(SerializableType serializableType) {
    return serializableType._type;
  }

  public static implicit operator SerializableType(Type type) {
    return new SerializableType() {
      _type = type
    };
  }
}
