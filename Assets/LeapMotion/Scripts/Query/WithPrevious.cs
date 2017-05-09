
namespace Leap.Unity.Query {

  public struct WithPreviousOp<SourceType, SourceOp> : IQueryOp<PrevPair<SourceType>>
    where SourceOp : IQueryOp<SourceType> {
    private SourceOp _mainOp;
    private SourceOp _delayedOp;

    private bool _includeStart;
    private int _offsetLeft;
    private int _offset;

    public WithPreviousOp(SourceOp op, int offset, bool includeStart) {
      _mainOp = op;
      _delayedOp = op;

      _includeStart = includeStart;
      _offsetLeft = offset;
      _offset = offset;
    }

    public bool TryGetNext(out PrevPair<SourceType> t) {
      top:

      SourceType value;
      if (_mainOp.TryGetNext(out value)) {
        if (_offsetLeft > 0) {
          _offsetLeft--;
          if (!_includeStart) {
            goto top;
          }

          t = new PrevPair<SourceType>() {
            value = value,
            prev = default(SourceType),
            hasPrev = false
          };
        } else {
          SourceType prev;
          _delayedOp.TryGetNext(out prev);
          t = new PrevPair<SourceType>() {
            value = value,
            prev = prev,
            hasPrev = true
          };
        }
        return true;
      } else {
        t = default(PrevPair<SourceType>);
        return false;
      }
    }

    public void Reset() {
      _mainOp.Reset();
      _delayedOp.Reset();
      _offsetLeft = _offset;
    }
  }

  public partial struct QueryWrapper<QueryType, QueryOp> where QueryOp : IQueryOp<QueryType> {
    public QueryWrapper<PrevPair<QueryType>, WithPreviousOp<QueryType, QueryOp>> WithPrevious(int offset = 1) {
      return new QueryWrapper<PrevPair<QueryType>, WithPreviousOp<QueryType, QueryOp>>(new WithPreviousOp<QueryType, QueryOp>(_op, offset, includeStart: false));
    }

    public QueryWrapper<PrevPair<QueryType>, WithPreviousOp<QueryType, QueryOp>> WithPrevious(bool includeStart) {
      return new QueryWrapper<PrevPair<QueryType>, WithPreviousOp<QueryType, QueryOp>>(new WithPreviousOp<QueryType, QueryOp>(_op, 1, includeStart));
    }

    public QueryWrapper<PrevPair<QueryType>, WithPreviousOp<QueryType, QueryOp>> WithPrevious(int offset, bool includeStart) {
      return new QueryWrapper<PrevPair<QueryType>, WithPreviousOp<QueryType, QueryOp>>(new WithPreviousOp<QueryType, QueryOp>(_op, offset, includeStart));
    }
  }

  public struct PrevPair<T> {
    public T value;
    public T prev;
    public bool hasPrev;
  }
}
