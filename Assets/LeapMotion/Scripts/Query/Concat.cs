using System;
using System.Collections;
using System.Collections.Generic;

namespace Leap.Unity.Query {

  public struct ConcatOp<SourceType, SourceOpA, SourceOpB> : IEnumerator<SourceType>
  where SourceOpA : IEnumerator<SourceType>
  where SourceOpB : IEnumerator<SourceType> {

    private SourceOpA _sourceA;
    private SourceOpB _sourceB;
    private bool _isOnA;

    public ConcatOp(SourceOpA enumeratorA, SourceOpB enumeratorB) {
      _sourceA = enumeratorA;
      _sourceB = enumeratorB;
      _isOnA = true;
    }

    public bool MoveNext() {
      if (_isOnA) {
        if (_sourceA.MoveNext()) {
          return true;
        } else {
          _isOnA = false;
        }
      }

      return _sourceB.MoveNext();
    }

    public SourceType Current {
      get {
        if (_isOnA) {
          return _sourceA.Current;
        } else {
          return _sourceB.Current;
        }
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
      _sourceA.Dispose();
      _sourceB.Dispose();
    }
  }

  public partial struct QueryWrapper<QueryType, QueryOp> where QueryOp : IEnumerator<QueryType> {
    public QueryWrapper<QueryType, ConcatOp<QueryType, QueryOp, SourceBOp>> Concat<SourceBOp>(QueryWrapper<QueryType, SourceBOp> sourceB)
      where SourceBOp : IEnumerator<QueryType> {
      return new QueryWrapper<QueryType, ConcatOp<QueryType, QueryOp, SourceBOp>>(new ConcatOp<QueryType, QueryOp, SourceBOp>(_op, sourceB._op));
    }
  }
}
