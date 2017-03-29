using System;
using System.Collections;
using System.Collections.Generic;

namespace Leap.Unity.Query {

  public struct WhereOp<SourceType, SourceOp> : IEnumerator<SourceType>
    where SourceOp : IEnumerator<SourceType> {
    private SourceOp _source;
    private Func<SourceType, bool> _predicate;

    public WhereOp(SourceOp enumerator, Func<SourceType, bool> predicate) {
      _source = enumerator;
      _predicate = predicate;
    }

    public bool MoveNext() {
      while (true) {
        if (!_source.MoveNext()) {
          return false;
        }

        if (_predicate(_source.Current)) {
          return true;
        }
      }
    }

    public SourceType Current {
      get {
        return _source.Current;
      }
    }

    object IEnumerator.Current {
      get {
        throw new InvalidOperationException();
      }
    }

    public void Reset() {
      throw new InvalidOperationException();
    }

    public void Dispose() {
      _source.Dispose();
      _predicate = null;
    }
  }

  public partial struct QueryWrapper<QueryType, QueryOp> where QueryOp : IEnumerator<QueryType> {
    public QueryWrapper<QueryType, WhereOp<QueryType, QueryOp>> Where(Func<QueryType, bool> predicate) {
      return new QueryWrapper<QueryType, WhereOp<QueryType, QueryOp>>(new WhereOp<QueryType, QueryOp>(_op, predicate));
    }

    public QueryWrapper<QueryType, WhereOp<QueryType, QueryOp>> NonNull() {
      return Where(o => o != null);
    }
  }
}
