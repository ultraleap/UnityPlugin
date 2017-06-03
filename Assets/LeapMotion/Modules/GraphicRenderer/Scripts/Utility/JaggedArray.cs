/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using UnityEngine;

namespace Leap.Unity.GraphicalRenderer {

  public class JaggedArray<T> : ISerializationCallbackReceiver {

    [NonSerialized]
    private T[][] _array;

    [SerializeField]
    private T[] _data;
    [SerializeField]
    private int[] _lengths;

    public JaggedArray() { }

    public JaggedArray(int length) {
      _array = new T[length][];
    }

    public JaggedArray(T[][] array) {
      _array = array;
    }

    public void OnAfterDeserialize() {
      _array = new T[_lengths.Length][];
      int offset = 0;
      for (int i = 0; i < _lengths.Length; i++) {
        int length = _lengths[i];
        if (length == -1) {
          _array[i] = null;
        } else {
          _array[i] = new T[length];
          Array.Copy(_data, offset, _array[i], 0, length);
          offset += length;
        }
      }
    }

    public void OnBeforeSerialize() {
      if (_array == null) {
        _data = new T[0];
        _lengths = new int[0];
        return;
      }

      int count = 0;
      foreach (var child in _array) {
        if (child == null) continue;
        count += child.Length;
      }

      _data = new T[count];
      _lengths = new int[_array.Length];
      int offset = 0;
      for (int i = 0; i < _array.Length; i++) {
        var child = _array[i];

        if (child == null) {
          _lengths[i] = -1;
        } else {
          Array.Copy(child, 0, _data, offset, child.Length);
          _lengths[i] = child.Length;
          offset += child.Length;
        }
      }
    }

    public T[] this[int index] {
      get {
        return _array[index];
      }
      set {
        _array[index] = value;
      }
    }

    public static implicit operator T[][] (JaggedArray<T> jaggedArray) {
      return jaggedArray._array;
    }

    public static implicit operator JaggedArray<T>(T[][] array) {
      return new JaggedArray<T>(array);
    }
  }
}
