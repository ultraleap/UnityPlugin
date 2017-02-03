using System;
using System.Collections;
using System.Collections.Generic;

namespace Leap.Unity.Query {

  public struct SelectOp<SourceType, ResultType, SourceOp> : IEnumerator<ResultType>
    where SourceOp : IEnumerator<SourceType> {
    private SourceOp _source;
    private Func<SourceType, ResultType> _mapping;
    private ResultType _current;

    public SelectOp(SourceOp enumerator, Func<SourceType, ResultType> mapping) {
      _source = enumerator;
      _mapping = mapping;
      _current = default(ResultType);
    }

    public bool MoveNext() {
      if (_source.MoveNext()) {
        _current = _mapping(_source.Current);
        return true;
      } else {
        return false;
      }
    }

    public ResultType Current {
      get {
        return _current;
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

    public QueryWrapper<NewType, SelectOp<QueryType, NewType, QueryOp>> Cast<NewType>() where NewType : class {
      return Select(obj => obj as NewType);
    }
  }
}
