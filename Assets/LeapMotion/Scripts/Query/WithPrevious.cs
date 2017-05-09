
namespace Leap.Unity.Query {

  public struct WithPreviousOp<SourceType, SourceOp> : IQueryOp<PrevPair<SourceType>>
    where SourceOp : IQueryOp<SourceType> {
    private SourceOp _op;
    private SourceType _prev;
    private bool _isFirst;

    public WithPreviousOp(SourceOp op) {
      _op = op;
      _prev = default(SourceType);
      _isFirst = true;
    }

    public bool TryGetNext(out PrevPair<SourceType> t) {
      SourceType value;
      if (_op.TryGetNext(out value)) {
        t = new PrevPair<SourceType>() {
          value = value,
          prev = _prev,
          isFirst = _isFirst
        };
        _isFirst = false;
        _prev = value;
        return true;
      } else {
        t = default(PrevPair<SourceType>);
        return false;
      }
    }

    public void Reset() {
      _op.Reset();
      _prev = default(SourceType);
      _isFirst = true;
    }
  }

  public partial struct QueryWrapper<QueryType, QueryOp> where QueryOp : IQueryOp<QueryType> {
    public QueryWrapper<PrevPair<QueryType>, WithPreviousOp<QueryType, QueryOp>> WithPrevious() {
      return new QueryWrapper<PrevPair<QueryType>, WithPreviousOp<QueryType, QueryOp>>(new WithPreviousOp<QueryType, QueryOp>(_op));
    }
  }

  public struct PrevPair<T> {
    public T value;
    public T prev;
    public bool isFirst;
  }
}
