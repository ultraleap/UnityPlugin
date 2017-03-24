using System;
using System.Collections.Generic;

namespace Leap.Unity {

  /// <summary>
  /// Implement this interface to recieve a callback whenever your object is
  /// spawned from a pool.  You do not recieve a callback during recycle because
  /// the recommended workflow is to have the object implement IDisposable and
  /// recycle itself within the Dispose method.
  /// </summary>
  public interface IPoolable {
    void OnSpawn();
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
  /// 
  /// Example workflow for types you DO NOT have control over:
  ///   var obj = Pool<T>.Spawn();
  ///   obj.Init(stuff);
  /// 
  ///   //Do something with obj
  /// 
  ///   obj.Clear();
  ///   Pool<T>.Recycle(obj);
  ///   
  /// Example workflow for types you DO have control over:
  ///   var obj = Pool<T>.Spawn();
  ///   obj.Init(stuff);
  ///   
  ///   //Do something with obj
  ///   
  ///   obj.Dispose(); // e.g. call Recycle(this) in the Dispose() implementation
  /// </summary>
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
      _pool.Push(t);
    }
  }
}
