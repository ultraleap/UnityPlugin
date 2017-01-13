using System.Collections;
using System.Collections.Generic;

namespace Leap.Unity.Query {

  public struct OfTypeOp<SourceType, ResultType, SourceOp> : IEnumerator<ResultType>
  where SourceOp : IEnumerator<SourceType>
  where ResultType : class {
    private SourceOp source;

    public OfTypeOp(SourceOp source) {
      this.source = source;
    }

    public bool MoveNext() {
      while (true) {
        if (!source.MoveNext()) {
          return false;
        }

        if (source.Current is ResultType) {
          return true;
        }
      }
    }

    public ResultType Current {
      get {
        return source.Current as ResultType;
      }
    }

    object IEnumerator.Current {
      get {
        return null;
      }
    }

    public void Reset() {
      source.Reset();
    }

    public void Dispose() {
      source.Dispose();
    }
  }

  public partial struct QueryWrapper<QueryType, QueryOp> where QueryOp : IEnumerator<QueryType> {
    public QueryWrapper<CastType, OfTypeOp<QueryType, CastType, QueryOp>> OfType<CastType>() where CastType : class {
      return new QueryWrapper<CastType, OfTypeOp<QueryType, CastType, QueryOp>>(new OfTypeOp<QueryType, CastType, QueryOp>(op));
    }
  }
}
