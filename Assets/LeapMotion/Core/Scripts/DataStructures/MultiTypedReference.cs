/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity {

  /// <summary>
  /// Represents a single reference to a value of type BaseType.
  /// 
  /// Unlike a normal reference, when MultiTypedReference is serialized
  /// it is able to support a certain amount of polymorphism.  To use 
  /// MultiTypedReference you must specify exactly which types could possibly
  /// be referenced.  To must also pre-declare a non-generic version of the
  /// chosen class, mucgh in the same style as UnityEvent.
  /// </summary>
  public abstract class MultiTypedReference<BaseType> where BaseType : class {
    public abstract void Clear();
    public abstract BaseType Value { get; set; }
  }

  public static class MultiTypedReferenceUtil {

#if UNITY_EDITOR
    public const string ID_NAME_TABLE = "abcdefghijklmnopqrstuvwxyz";

    public static SerializedProperty GetReferenceProperty(SerializedProperty property) {
      var indexProp = property.FindPropertyRelative("_index");
      int indexValue = indexProp.intValue;
      if (indexValue == -1) {
        return null;
      }

      string listPropName = "_" + ID_NAME_TABLE[indexValue];
      var listProp = property.FindPropertyRelative(listPropName);
      return listProp.GetArrayElementAtIndex(0);
    }
#endif
  }

  public class MultiTypedReference<BaseType, A, B> : MultiTypedReference<BaseType>
    where BaseType : class
    where A : BaseType
    where B : BaseType {

    [SerializeField]
    protected int _index = -1;

    [SerializeField]
    private List<A> _a = new List<A>();

    [SerializeField]
    private List<B> _b = new List<B>();

    [NonSerialized]
    protected BaseType _cachedValue;

    public override void Clear() {
      _cachedValue = null;

      if (_index == 0) {
        _a.Clear();
      } else if (_index == 1) {
        _b.Clear();
      }

      _index = -1;
    }

    public sealed override BaseType Value {
      get {
        if (_cachedValue != null) {
          return _cachedValue;
        }

        _cachedValue = internalGet();

        return _cachedValue;
      }
      set {
        Clear();
        internalSetAfterClear(value);
        _cachedValue = value;
      }
    }

    protected virtual BaseType internalGet() {
      if (_index == -1) {
        return null;
      } else if (_index == 0) {
        return _cachedValue = _a[0];
      } else if (_index == 1) {
        return _cachedValue = _b[0];
      } else {
        throw new Exception("Invalid index " + _index);
      }
    }

    protected virtual void internalSetAfterClear(BaseType obj) {
      if (obj == null) {
        _index = -1;
      } else if (obj is A) {
        _a.Add((A)obj);
        _index = 0;
      } else if (obj is B) {
        _b.Add((B)obj);
        _index = 1;
      } else {
        throw new ArgumentException("The type " + obj.GetType().Name + " is not supported by this reference.");
      }
    }
  }

  public class MultiTypedReference<BaseType, A, B, C> : MultiTypedReference<BaseType, A, B>
    where BaseType : class
    where A : BaseType
    where B : BaseType
    where C : BaseType {

    [SerializeField]
    private List<C> _c = new List<C>();

    public override void Clear() {
      if (_index == 2) {
        _c.Clear();
      }

      base.Clear();
    }

    protected override BaseType internalGet() {
      if (_index == 2) {
        return _c[0];
      } else {
        return base.internalGet();
      }
    }

    protected override void internalSetAfterClear(BaseType obj) {
      if (obj is C) {
        _c.Add((C)obj);
        _index = 2;
      } else {
        base.internalSetAfterClear(obj);
      }
    }
  }

  public class MultiTypedReference<BaseType, A, B, C, D> : MultiTypedReference<BaseType, A, B, C>
    where BaseType : class
    where A : BaseType
    where B : BaseType
    where C : BaseType
    where D : BaseType {

    [SerializeField]
    private List<D> _d = new List<D>();

    public override void Clear() {
      if (_index == 3) {
        _d.Clear();
      }

      base.Clear();
    }

    protected override BaseType internalGet() {
      if (_index == 3) {
        return _d[0];
      } else {
        return base.internalGet();
      }
    }

    protected override void internalSetAfterClear(BaseType obj) {
      if (obj is D) {
        _d.Add((D)obj);
        _index = 3;
      } else {
        base.internalSetAfterClear(obj);
      }
    }
  }
}
