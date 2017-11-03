/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

namespace Leap.Unity.Query {

  public struct RepeatOp<SourceType, SourceOp> : IQueryOp<SourceType>
    where SourceOp : IQueryOp<SourceType> {
    private SourceOp _source;
    private int _repeatTimes;
    private int _currTimes;

    public RepeatOp(SourceOp source, int times) {
      _source = source;
      _repeatTimes = times;
      _currTimes = 0;
    }

    public bool TryGetNext(out SourceType t) {
      if (_currTimes == _repeatTimes) {
        t = default(SourceType);
        return false;
      }

      if (_source.TryGetNext(out t)) {
        return true;
      }

      _currTimes++;
      if (_currTimes == _repeatTimes) {
        return false;
      }

      _source.Reset();
      if (_source.TryGetNext(out t)) {
        return true;
      }

      return false;
    }

    public void Reset() {
      _source.Reset();
      _currTimes = 0;
    }
  }

  public partial struct QueryWrapper<QueryType, QueryOp> where QueryOp : IQueryOp<QueryType> {

    /// <summary>
    /// Returns a new query operation representing the current sequence repeated a number of
    /// times.  If the number of repetitions is less than zero, the sequence will be repeated
    /// forever.
    /// 
    /// For example:
    ///   (1, 2, 3).Query().Repeat(3)
    /// Would result in:
    ///   (1, 2, 3, 1, 2, 3, 1, 2, 3)
    /// </summary>
    public QueryWrapper<QueryType, RepeatOp<QueryType, QueryOp>> Repeat(int times = -1) {
      return new QueryWrapper<QueryType, RepeatOp<QueryType, QueryOp>>(new RepeatOp<QueryType, QueryOp>(_op, times));
    }
  }
}
