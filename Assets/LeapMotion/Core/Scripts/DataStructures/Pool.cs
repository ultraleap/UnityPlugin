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

  /// <summary>
  /// Implement this interface to recieve a callback whenever your object is
  /// spawned from a pool.
  /// </summary>
  public interface IPoolable {
    void OnSpawn();
    void OnRecycle();
  }

  /// <summary>
  /// A very lightweight pool implementation.  When you call Spawn, an object
  /// of type T will be returned.  If the pool was not empty, the T will be
  /// taken from the pool.  If the pool was empty, a new T will be constructed
  /// and returned instead.  Calling recycle will return a T to the pool.
  ///
  /// It is not required to implement the IPoolable interface to use the Pool
  /// class, which allows you to pool types such as List or Dictionary, types
  /// which you have no control over.  But make sure that you clean up these
  /// objects before you recycle them!
  /// </summary>
  /// <example>
  ///   Example workflow for types you DO NOT have control over:
  ///   <code>
  ///     // <![CDATA[" // (XML fix for Visual Studio)
  ///
  ///     var obj = Pool<T>.Spawn();
  ///     obj.Init(stuff);
  ///
  ///     //Do something with obj
  ///
  ///     obj.Clear();
  ///     Pool<T>.Recycle(obj);
  ///
  ///     // "]]> // (Close XML fix for Visual Studio)
  ///   </code>
  /// </example>
  /// <example>
  ///   Example workflow for types you DO have control over:
  ///   <code>
  ///     // <![CDATA[" // (XML fix for Visual Studio)
  ///
  ///     var obj = Pool<T>.Spawn();
  ///     obj.Init(stuff);
  ///
  ///     // Do something with obj
  ///
  ///     obj.Dispose(); // e.g. call Recycle(this) in the Dispose() implementation
  ///
  ///     // "]]> // (Close XML fix for Visual Studio)
  ///   </code>
  /// </example>
  public static class Pool<T> where T : new() {
    [ThreadStatic]
    private static Stack<T> _pool = new Stack<T>();

    public static T Spawn() {
      if (_pool == null) _pool = new Stack<T>();

      T value;
      if (_pool.Count > 0) {
        value = _pool.Pop();
      } else {
        value = new T();
      }

      if (value is IPoolable) {
        (value as IPoolable).OnSpawn();
      }

      return value;
    }

    public static void Recycle(T t) {
      if (t == null) {
        Debug.LogError("Cannot recycle a null object.");
        return;
      }

      if (t is IPoolable) {
        (t as IPoolable).OnRecycle();
      }

      _pool.Push(t);
    }
  }
}
