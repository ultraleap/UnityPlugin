using System;
using System.Collections;
using System.Collections.Generic;

namespace Leap.Unity.Query {

  public struct TakeCountOp<SourceType, SourceOp> : IEnumerator<SourceType>
  where SourceOp : IEnumerator<SourceType> {
    private SourceOp _source;
    private int _toTake;

    public TakeCountOp(SourceOp source, int toTake) {
      _source = source;
      _toTake = toTake;
    }

    public bool MoveNext() {
      if (_toTake == 0) {
        return false;
      }

      _toTake--;
      return _source.MoveNext();
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
    public QueryWrapper<QueryType, TakeCountOp<QueryType, QueryOp>> Take(int count) {
      return new QueryWrapper<QueryType, TakeCountOp<QueryType, QueryOp>>(new TakeCountOp<QueryType, QueryOp>(_op, count));
    }
  }
}
