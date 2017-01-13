using System;
using System.Collections;
using System.Collections.Generic;

namespace Leap.Unity.Query {

  public struct SelectOp<SourceType, ResultType, SourceOp> : IEnumerator<ResultType> where SourceOp : IEnumerator<SourceType> {
    private SourceOp source;
    private Func<SourceType, ResultType> mapping;

    public SelectOp(SourceOp enumerator, Func<SourceType, ResultType> mapping) {
      this.source = enumerator;
      this.mapping = mapping;
    }

    public bool MoveNext() {
      return source.MoveNext();
    }

    public ResultType Current {
      get {
        return mapping(source.Current);
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
      mapping = null;
    }
  }

  public partial struct QueryWrapper<QueryType, QueryOp> where QueryOp : IEnumerator<QueryType> {
    public QueryWrapper<NewType, SelectOp<QueryType, NewType, QueryOp>> Select<NewType>(Func<QueryType, NewType> mapping) {
      return new QueryWrapper<NewType, SelectOp<QueryType, NewType, QueryOp>>(new SelectOp<QueryType, NewType, QueryOp>(op, mapping));
    }
  }
}
