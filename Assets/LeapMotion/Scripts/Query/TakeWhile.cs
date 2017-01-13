using System;
using System.Collections;
using System.Collections.Generic;

namespace Leap.Unity.Query {

  public struct TakeWhileOp<SourceType, SourceOp> : IEnumerator<SourceType>
  where SourceOp : IEnumerator<SourceType> {
    private SourceOp source;
    private Func<SourceType, bool> predicate;
    private bool hasPredicateFailed;

    public TakeWhileOp(SourceOp source, Func<SourceType, bool> predicate) {
      this.source = source;
      this.predicate = predicate;
      this.hasPredicateFailed = false;
    }

    public bool MoveNext() {
      if (hasPredicateFailed) {
        return false;
      }

      while (true) {
        if (!source.MoveNext()) {
          return false;
        }

        if (!predicate(source.Current)) {
          hasPredicateFailed = true;
          return false;
        }
      }
    }

    public SourceType Current {
      get {
        return source.Current;
      }
    }

    object IEnumerator.Current {
      get {
        return null;
      }
    }

    public void Reset() {
      source.Reset();
      hasPredicateFailed = false;
    }

    public void Dispose() {
      source.Dispose();
    }
  }

  public partial struct QueryWrapper<QueryType, QueryOp> where QueryOp : IEnumerator<QueryType> {
    public QueryWrapper<QueryType, TakeWhileOp<QueryType, QueryOp>> TakeWhile(Func<QueryType, bool> predicate) {
      return new QueryWrapper<QueryType, TakeWhileOp<QueryType, QueryOp>>(new TakeWhileOp<QueryType, QueryOp>(op, predicate));
    }
  }
}
