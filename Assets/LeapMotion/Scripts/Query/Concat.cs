/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

namespace Leap.Unity.Query {

  public struct ConcatOp<SourceType, SourceOpA, SourceOpB> : IQueryOp<SourceType>
  where SourceOpA : IQueryOp<SourceType>
  where SourceOpB : IQueryOp<SourceType> {

    private SourceOpA _sourceA;
    private SourceOpB _sourceB;
    private bool _isOnA;

    public ConcatOp(SourceOpA enumeratorA, SourceOpB enumeratorB) {
      _sourceA = enumeratorA;
      _sourceB = enumeratorB;
      _isOnA = true;
    }

    public bool TryGetNext(out SourceType t) {
      if (_isOnA) {
        if (_sourceA.TryGetNext(out t)) {
          return true;
        } else {
          _isOnA = false;
        }
      }

      return _sourceB.TryGetNext(out t);
    }

    public void Reset() {
      _isOnA = true;
      _sourceA.Reset();
      _sourceB.Reset();
    }
  }

  public partial struct QueryWrapper<QueryType, QueryOp> where QueryOp : IQueryOp<QueryType> {
    public QueryWrapper<QueryType, ConcatOp<QueryType, QueryOp, SourceBOp>> Concat<SourceBOp>(QueryWrapper<QueryType, SourceBOp> sourceB)
      where SourceBOp : IQueryOp<QueryType> {
      return new QueryWrapper<QueryType, ConcatOp<QueryType, QueryOp, SourceBOp>>(new ConcatOp<QueryType, QueryOp, SourceBOp>(_op, sourceB._op));
    }
  }
}
