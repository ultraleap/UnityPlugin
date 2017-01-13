using System;
using System.Collections;
using System.Collections.Generic;

namespace Leap.Unity.Query {

  public struct TakeWhileOp<SourceType, SourceOp> : IEnumerator<SourceType>
  where SourceOp : IEnumerator<SourceType> {
    private SourceOp _source;
    private Func<SourceType, bool> _predicate;
    private bool _hasPredicateFailed;

    public TakeWhileOp(SourceOp source, Func<SourceType, bool> predicate) {
      _source = source;
      _predicate = predicate;
      _hasPredicateFailed = false;
    }

    public bool MoveNext() {
      if (_hasPredicateFailed) {
        return false;
      }

      while (true) {
        if (!_source.MoveNext()) {
          return false;
        }

        if (!_predicate(_source.Current)) {
          _hasPredicateFailed = true;
          return false;
        } else {
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
    }
  }

  public partial struct QueryWrapper<QueryType, QueryOp> where QueryOp : IEnumerator<QueryType> {
    public QueryWrapper<QueryType, TakeWhileOp<QueryType, QueryOp>> TakeWhile(Func<QueryType, bool> predicate) {
      return new QueryWrapper<QueryType, TakeWhileOp<QueryType, QueryOp>>(new TakeWhileOp<QueryType, QueryOp>(_op, predicate));
    }
  }
}
