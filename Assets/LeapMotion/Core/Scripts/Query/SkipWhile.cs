/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;

namespace Leap.Unity.Query {

  public struct SkipWhileOp<SourceType, SourceOp> : IQueryOp<SourceType>
    where SourceOp : IQueryOp<SourceType> {

    private SourceOp _source;
    private Func<SourceType, bool> _predicate;
    private bool _finishedSkipping;

    public SkipWhileOp(SourceOp source, Func<SourceType, bool> predicate) {
      _source = source;
      _predicate = predicate;
      _finishedSkipping = false;
    }

    public bool TryGetNext(out SourceType t) {
      while (!_finishedSkipping) {
        if (!_source.TryGetNext(out t)) {
          _finishedSkipping = true;
          return false;
        }

        if (!_predicate(t)) {
          _finishedSkipping = true;
          return true;
        }
      }

      return _source.TryGetNext(out t);
    }

    public void Reset() {
      _source.Reset();
      _finishedSkipping = false;
    }
  }

  public partial struct QueryWrapper<QueryType, QueryOp> where QueryOp : IQueryOp<QueryType> {

    /// <summary>
    /// Returns a new query operation that skips values while the predicate returns true.  As soon
    /// as the predicate returns false, the operation returns the remainder of the sequence.  Even
    /// if the predicate becomes true again, the elements are still returned.
    /// 
    /// For example
    ///   (-1, -2, -5, 5, 9, -1, 5, -3).Query().SkipWhile(isNegative)
    /// Would result in 
    ///   (5, 9, -1, 5, -3)
    /// </summary>
    public QueryWrapper<QueryType, SkipWhileOp<QueryType, QueryOp>> SkipWhile(Func<QueryType, bool> predicate) {
      return new QueryWrapper<QueryType, SkipWhileOp<QueryType, QueryOp>>(new SkipWhileOp<QueryType, QueryOp>(_op, predicate));
    }
  }
}
