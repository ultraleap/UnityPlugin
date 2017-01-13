using System;
using System.Collections;
using System.Collections.Generic;

namespace Leap.Unity.Query {

  public struct WhereOp<T, EnumType> : IEnumerator<T> where EnumType : IEnumerator<T> {
    private EnumType enumerator;
    private Func<T, bool> predicate;

    public WhereOp(EnumType enumerator, Func<T, bool> predicate) {
      this.enumerator = enumerator;
      this.predicate = predicate;
    }

    public bool MoveNext() {
      while (true) {
        if (!enumerator.MoveNext()) {
          return false;
        }

        if (predicate(enumerator.Current)) {
          return true;
        }
      }
    }

    public T Current {
      get {
        return enumerator.Current;
      }
    }

    object IEnumerator.Current {
      get {
        return null;
      }
    }

    public void Reset() {
      enumerator.Reset();
    }

    public void Dispose() {
      enumerator.Dispose();
      predicate = null;
    }
  }

  public partial struct QueryWrapper<QueryType, QueryOp> where QueryOp : IEnumerator<QueryType> {
    public QueryWrapper<QueryType, WhereOp<QueryType, QueryOp>> Where(Func<QueryType, bool> predicate) {
      return new QueryWrapper<QueryType, WhereOp<QueryType, QueryOp>>(new WhereOp<QueryType, QueryOp>(op, predicate));
    }
  }
}
