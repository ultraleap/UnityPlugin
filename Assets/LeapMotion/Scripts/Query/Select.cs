using System;

namespace Leap.Unity.Query {

  public struct SelectOp<SourceType, ResultType, SourceOp> : IQueryOp<ResultType>
    where SourceOp : IQueryOp<SourceType> {
    private SourceOp _source;
    private Func<SourceType, ResultType> _mapping;

    public SelectOp(SourceOp enumerator, Func<SourceType, ResultType> mapping) {
      _source = enumerator;
      _mapping = mapping;
    }

    public bool TryGetNext(out ResultType t) {
      SourceType sourceObj;
      if (_source.TryGetNext(out sourceObj)) {
        t = _mapping(sourceObj);
        return true;
      } else {
        t = default(ResultType);
        return false;
      }
    }

    public void Reset() {
      _source.Reset();
    }
  }

  public partial struct QueryWrapper<QueryType, QueryOp> where QueryOp : IQueryOp<QueryType> {
    public QueryWrapper<NewType, SelectOp<QueryType, NewType, QueryOp>> Select<NewType>(Func<QueryType, NewType> mapping) {
      return new QueryWrapper<NewType, SelectOp<QueryType, NewType, QueryOp>>(new SelectOp<QueryType, NewType, QueryOp>(_op, mapping));
    }

    public QueryWrapper<NewType, SelectOp<QueryType, NewType, QueryOp>> Cast<NewType>() where NewType : class {
      return Select(obj => obj as NewType);
    }
  }
}
