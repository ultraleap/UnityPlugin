using System;

namespace Leap.Unity.Query {

  public struct WhereOp<SourceType, SourceOp> : IQueryOp<SourceType>
    where SourceOp : IQueryOp<SourceType> {
    private SourceOp _source;
    private Func<SourceType, bool> _predicate;

    public WhereOp(SourceOp enumerator, Func<SourceType, bool> predicate) {
      _source = enumerator;
      _predicate = predicate;
    }

    public bool TryGetNext(out SourceType t) {
      while (true) {
        if (!_source.TryGetNext(out t)) {
          return false;
        }
        if (_predicate(t)) {
          return true;
        }
      }
    }

    public void Reset() {
      _source.Reset();
    }
  }

  public partial struct QueryWrapper<QueryType, QueryOp> where QueryOp : IQueryOp<QueryType> {
    public QueryWrapper<QueryType, WhereOp<QueryType, QueryOp>> Where(Func<QueryType, bool> predicate) {
      return new QueryWrapper<QueryType, WhereOp<QueryType, QueryOp>>(new WhereOp<QueryType, QueryOp>(_op, predicate));
    }

    public QueryWrapper<QueryType, WhereOp<QueryType, QueryOp>> NonNull() {
      return Where(obj => obj != null);
    }
  }
}
