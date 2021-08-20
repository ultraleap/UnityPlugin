/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity {

  [Serializable]
  public struct Hash : IEnumerable, IEquatable<Hash> {
    private int _hash;

    public Hash(int hash) {
      _hash = hash;
    }

    public void Add<T>(T t) {
      int code = t == null ? 0x2692d0f9 : t.GetHashCode();
      _hash ^= code + 0x3e3779b9 + (_hash << 6) + (_hash >> 2);
    }

    public void AddRange<T>(List<T> sequence) {
      for (int i = 0; i < sequence.Count; i++) {
        Add(sequence[i]);
      }
    }

    /// <summary>
    /// Returns a hash code of the entire transform hierarchy, changing 
    /// any of the following should result in a different hash code:
    ///  - position/rotation/scale of any transform
    ///  - creation/deletion/reordering of any transform
    ///  - turning a transform into a rect transform or back
    ///  - enabling or disabling ANY behavior
    /// </summary>
    private static List<Behaviour> _behaviourCache = new List<Behaviour>();
    public static Hash GetHierarchyHash(Transform root) {
      var hash = Hash.GetDataHash(root);

      int childCount = root.childCount;
      for (int i = 0; i < childCount; i++) {
        hash.Add(GetHierarchyHash(root.GetChild(i)));
      }

      root.GetComponents(_behaviourCache);
      for (int i = 0; i < _behaviourCache.Count; i++) {
        var behaviour = _behaviourCache[i];

        //A behaviour returned from GetComponents can be null if it is an invalid
        //script object or due to a compile error >.>
        if (behaviour != null) {
          hash.Add(behaviour.enabled);
        }
      }

      return hash;
    }

    /// <summary>
    /// Returns a hash of the Transform in addition to it's data.
    /// Changing either the position, rotation, or scale of a 
    /// transform will result in a different hash.  Two transforms
    /// with the same position rotation and scale will not have
    /// the same hash!
    /// </summary>
    public static Hash GetDataHash(Transform transform) {
      var hash = new Hash() {
        transform,
        transform.gameObject.activeSelf,
        transform.localPosition,
        transform.localRotation,
        transform.localScale
      };

      if (transform is RectTransform) {
        RectTransform rectTransform = transform as RectTransform;
        hash.Add(rectTransform.rect);
      }

      return hash;
    }

    public IEnumerator GetEnumerator() {
      throw new NotImplementedException();
    }

    public override int GetHashCode() {
      return _hash;
    }

    public override bool Equals(object obj) {
      if (!(obj is Hash)) {
        return false;
      }
      Hash hash = (Hash)obj;
      return hash._hash == _hash;
    }

    public bool Equals(Hash other) {
      return _hash == other._hash;
    }

    public static implicit operator Hash(int hash) {
      return new Hash(hash);
    }

    public static implicit operator int(Hash hash) {
      return hash._hash;
    }
  }
}
