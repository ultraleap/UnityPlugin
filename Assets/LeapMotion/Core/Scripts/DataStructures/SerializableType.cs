/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using System.Reflection;
using UnityEngine;

namespace Leap.Unity {

  [Serializable]
  public struct SerializableType : ISerializationCallbackReceiver {

    [SerializeField, HideInInspector]
    private Type _type;

    [SerializeField, HideInInspector]
    private string _fullName;

    private static Assembly[] _cachedAssemblies = null;
    private static Assembly[] _assemblies {
      get {
        if (_cachedAssemblies == null) {
          _cachedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
        }
        return _cachedAssemblies;
      }
    }

    public void OnAfterDeserialize() {
      if (!string.IsNullOrEmpty(_fullName)) {
        foreach (var assembly in _assemblies) {
          _type = assembly.GetType(_fullName, throwOnError: false);
          if (_type != null) {
            break;
          }
        }
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
}
