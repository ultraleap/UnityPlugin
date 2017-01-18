using System;
using System.Collections;
using System.Collections.Generic;

namespace Leap.Unity.Query {

  public struct SelectOp<SourceType, ResultType, SourceOp> : IEnumerator<ResultType>
    where SourceOp : IEnumerator<SourceType> {
    private SourceOp _source;
    private Func<SourceType, ResultType> _mapping;

    public SelectOp(SourceOp enumerator, Func<SourceType, ResultType> mapping) {
      _source = enumerator;
      _mapping = mapping;
    }

    public bool MoveNext() {
      return _source.MoveNext();
    }

    public ResultType Current {
      get {
        return _mapping(_source.Current);
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
      _mapping = null;
    }
  }

  public partial struct QueryWrapper<QueryType, QueryOp> where QueryOp : IEnumerator<QueryType> {
    public QueryWrapper<NewType, SelectOp<QueryType, NewType, QueryOp>> Select<NewType>(Func<QueryType, NewType> mapping) {
      return new QueryWrapper<NewType, SelectOp<QueryType, NewType, QueryOp>>(new SelectOp<QueryType, NewType, QueryOp>(_op, mapping));
    }
  }
}
