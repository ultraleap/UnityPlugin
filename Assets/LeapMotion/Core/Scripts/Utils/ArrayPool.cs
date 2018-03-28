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

namespace Leap.Unity {

  public static class ArrayPool<T> {
    private static Dictionary<int, Stack<T[]>> _bins;

    static ArrayPool() {
      _bins = new Dictionary<int, Stack<T[]>>();

      _bins[0] = new Stack<T[]>();
      for (int i = 0; i < 32; i++) {
        _bins[1 << i] = new Stack<T[]>();
      }
    }

    /// <summary>
    /// Spawns an array from the ArrayPool given the minimum length.
    /// The actual length of the array may be larger than what is
    /// requested.
    /// 
    /// Arrays can be returned to the array pool with the Recycle
    /// method.
    /// </summary>
    public static T[] Spawn(int minLength) {
      int count = Mathf.NextPowerOfTwo(minLength);
      var bin = _bins[count];

      if (bin.Count > 0) {
        return bin.Pop();
      } else {
        return new T[count];
      }
    }

    /// <summary>
    /// Recycles an array into the ArrayPool.  The array does not
    /// need to be an array that was created with Spawn.
    /// </summary>
    public static void Recycle(T[] array) {
      Array.Clear(array, 0, array.Length);
      int binKey = Mathf.NextPowerOfTwo(array.Length + 1) / 2;
      _bins[binKey].Push(array);
    }
  }
}
