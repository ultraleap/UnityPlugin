using System.Collections;
using System.Collections.Generic;

namespace Leap.Unity.Query {

  public struct ConcatOp<SourceType, SourceOpA, SourceOpB> : IEnumerator<SourceType>
  where SourceOpA : IEnumerator<SourceType>
  where SourceOpB : IEnumerator<SourceType> {

    private SourceOpA sourceA;
    private SourceOpB sourceB;
    private bool isOnA;

    public ConcatOp(SourceOpA enumeratorA, SourceOpB enumeratorB) {
      this.sourceA = enumeratorA;
      this.sourceB = enumeratorB;
      this.isOnA = true;
    }

    public bool MoveNext() {
      if (isOnA) {
        if (sourceA.MoveNext()) {
          return true;
        } else {
          isOnA = false;
        }
      }

      return sourceB.MoveNext();
    }

    public SourceType Current {
      get {
        if (isOnA) {
          return sourceA.Current;
        } else {
          return sourceB.Current;
        }
      }
    }

    object IEnumerator.Current {
      get {
        return null;
      }
    }

    public void Reset() {
      sourceA.Reset();
      sourceB.Reset();
      isOnA = true;
    }

    public void Dispose() {
      sourceA.Dispose();
      sourceB.Dispose();
    }
  }

  public partial struct QueryWrapper<QueryType, QueryOp> where QueryOp : IEnumerator<QueryType> {
    public QueryWrapper<QueryType, ConcatOp<QueryType, QueryOp, SourceBOp>> Concat<SourceBOp>(QueryWrapper<QueryType, SourceBOp> sourceB)
      where SourceBOp : IEnumerator<QueryType> {
      return new QueryWrapper<QueryType, ConcatOp<QueryType, QueryOp, SourceBOp>>(new ConcatOp<QueryType, QueryOp, SourceBOp>(op, sourceB.op));
    }
  }
}
