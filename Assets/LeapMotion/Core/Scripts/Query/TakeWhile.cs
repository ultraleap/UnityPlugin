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

    /// <summary>
    /// Returns a new query operation that takes values while the predicate returns true.  As soon
    /// as the predicate returns false, the sequence will return no more elements.  Even if the
    /// predicate becomes true again, the sequence will still halt.
    /// 
    /// For example:
    ///   (1, 3, 9, -1, 5, -4, 9).Query().TakeWhile(isPositive)
    /// Would result in:
    ///   (1, 3, 9)
    /// </summary>
    public QueryWrapper<QueryType, TakeWhileOp<QueryType, QueryOp>> TakeWhile(Func<QueryType, bool> predicate) {
      return new QueryWrapper<QueryType, TakeWhileOp<QueryType, QueryOp>>(new TakeWhileOp<QueryType, QueryOp>(_op, predicate));
    }
  }
}
