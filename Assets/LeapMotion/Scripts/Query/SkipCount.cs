/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

namespace Leap.Unity.Query {

  public struct SkipCountOp<SourceType, SourceOp> : IQueryOp<SourceType>
    where SourceOp : IQueryOp<SourceType> {

    private SourceOp _source;
    private int _toSkip;
    private int _skipLeft;

    public SkipCountOp(SourceOp source, int toSkip) {
      _source = source;
      _toSkip = toSkip;
      _skipLeft = _toSkip;
    }

    public bool TryGetNext(out SourceType t) {
      while (true) {
        if (!_source.TryGetNext(out t)) {
          return false;
        }

        if (_skipLeft == 0) {
          return true;
        }
        _skipLeft--;
      }
    }

    public void Reset() {
      _skipLeft = _toSkip;
      _source.Reset();
    }
  }

  public partial struct QueryWrapper<QueryType, QueryOp> where QueryOp : IQueryOp<QueryType> {
    public QueryWrapper<QueryType, SkipCountOp<QueryType, QueryOp>> Skip(int toSkip) {
      return new QueryWrapper<QueryType, SkipCountOp<QueryType, QueryOp>>(new SkipCountOp<QueryType, QueryOp>(_op, toSkip));
    }
  }
}
