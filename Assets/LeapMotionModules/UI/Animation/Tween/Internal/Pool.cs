using System.Collections.Generic;

namespace Leap.Unity.Animation.Internal {

  public static class Pool<T> where T : IPoolable, new() {
    private static Queue<T> _pool = new Queue<T>();

    public static T Spawn() {
      T t;
      if (_pool.Count > 0) {
        t = _pool.Dequeue();
      } else {
        t = new T();
      }
      t.OnSpawn();
      return t;
    }

    public static void Recycle(T t) {
      _pool.Enqueue(t);
    }
  }
}
