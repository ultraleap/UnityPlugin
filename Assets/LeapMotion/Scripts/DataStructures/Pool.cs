using System;
using System.Collections.Generic;

namespace Leap.Unity {

  public interface IPoolable {
    void OnSpawn();
  }

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
