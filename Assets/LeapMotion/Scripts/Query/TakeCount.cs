/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

namespace Leap.Unity.Query {

  public struct TakeCountOp<SourceType, SourceOp> : IQueryOp<SourceType>
  where SourceOp : IQueryOp<SourceType> {
    private SourceOp _source;
    private int _takeLeft;
    private int _toTake;

    public TakeCountOp(SourceOp source, int toTake) {
      _source = source;
      _takeLeft = toTake;
      _toTake = toTake;
    }

    public bool TryGetNext(out SourceType t) {
      if (_takeLeft == 0) {
        t = default(SourceType);
        return false;
      }

      _takeLeft--;
      return _source.TryGetNext(out t);
    }

    public void Reset() {
      _takeLeft = _toTake;
      _source.Reset();
    }
  }

  public partial struct QueryWrapper<QueryType, QueryOp> where QueryOp : IQueryOp<QueryType> {
    public QueryWrapper<QueryType, TakeCountOp<QueryType, QueryOp>> Take(int count) {
      return new QueryWrapper<QueryType, TakeCountOp<QueryType, QueryOp>>(new TakeCountOp<QueryType, QueryOp>(_op, count));
    }
  }
}
