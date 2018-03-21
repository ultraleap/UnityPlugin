using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Query {

  public partial struct Query<T> {

    public Query<K> OfType<K>() where K : T {
      var dstArray = ArrayPool<K>.Spawn(_count);

      int dstCount = 0;
      for (int i = 0; i < _count; i++) {
        if (_array[i] is K) {
          dstArray[dstCount++] = (K)_array[i];
        }
      }

      Dispose();
      return new Query<K>(dstArray, dstCount);
    }

    public Query<K> Cast<K>() where K : class {
      return this.Select(item => item as K);
    }

  }
}
