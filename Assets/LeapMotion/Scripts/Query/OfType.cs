using System;
using System.Collections;
using System.Collections.Generic;

namespace Leap.Unity.Query {

  public struct OfTypeOp<SourceType, ResultType, SourceOp> : IEnumerator<ResultType>
    where SourceOp : IEnumerator<SourceType>
    where ResultType : class {
    private SourceOp _source;

    public OfTypeOp(SourceOp source) {
      _source = source;
    }

    public bool MoveNext() {
      while (true) {
        if (!_source.MoveNext()) {
          return false;
        }

        if (_source.Current is ResultType) {
          return true;
        }
      }
    }

    public ResultType Current {
      get {
        return _source.Current as ResultType;
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
    public QueryWrapper<CastType, OfTypeOp<QueryType, CastType, QueryOp>> OfType<CastType>() where CastType : class {
      return new QueryWrapper<CastType, OfTypeOp<QueryType, CastType, QueryOp>>(new OfTypeOp<QueryType, CastType, QueryOp>(_op));
    }

    public QueryWrapper<QueryType, WhereOp<QueryType, QueryOp>> OfType(Type type) {
      return new QueryWrapper<QueryType, WhereOp<QueryType, QueryOp>>(new WhereOp<QueryType, QueryOp>(_op, element => element != null && type.IsAssignableFrom(element.GetType())));
    }
  }
}
