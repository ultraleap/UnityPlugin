using System;

namespace Leap.Unity.Query {

  public struct TakeWhileOp<SourceType, SourceOp> : IQueryOp<SourceType>
  where SourceOp : IQueryOp<SourceType> {
    private SourceOp _source;
    private Func<SourceType, bool> _predicate;
    private bool _hasPredicateFailed;

    public TakeWhileOp(SourceOp source, Func<SourceType, bool> predicate) {
      _source = source;
      _predicate = predicate;
      _hasPredicateFailed = false;
    }

    public bool TryGetNext(out SourceType t) {
      if (_hasPredicateFailed) {
        t = default(SourceType);
        return false;
      }

      if (!_source.TryGetNext(out t)) {
        return false;
      }

      if (!_predicate(t)) {
        _hasPredicateFailed = true;
        return false;
      }

      return true;
    }

    public void Reset() {
      _hasPredicateFailed = false;
      _source.Reset();
    }
  }

  public partial struct QueryWrapper<QueryType, QueryOp> where QueryOp : IQueryOp<QueryType> {
    public QueryWrapper<QueryType, TakeWhileOp<QueryType, QueryOp>> TakeWhile(Func<QueryType, bool> predicate) {
      return new QueryWrapper<QueryType, TakeWhileOp<QueryType, QueryOp>>(new TakeWhileOp<QueryType, QueryOp>(_op, predicate));
    }
  }
}
