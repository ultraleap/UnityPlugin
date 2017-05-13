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

    /// <summary>
    /// Returns a new query operation representing only the elements of the sequence for which
    /// the predicate returns true.
    /// 
    /// For example:
    ///   (1, 2, 3, 4, 5, 6, 7).Query().Where(isEven)
    /// Would result in:
    ///   (2, 4, 6)
    /// </summary>
    public QueryWrapper<QueryType, WhereOp<QueryType, QueryOp>> Where(Func<QueryType, bool> predicate) {
      return new QueryWrapper<QueryType, WhereOp<QueryType, QueryOp>>(new WhereOp<QueryType, QueryOp>(_op, predicate));
    }

    /// <summary>
    /// Returns a new query operation representing only the elements of the sequence that are not null.
    /// 
    /// IMPORTANT!  This might have strange results when using objects that derive from UnityEngine.Object, since
    /// unity objects can sometimes not be null even though they pretend to be.  For unity objects, it is best
    /// to use ValidUnityObjs instead.
    /// </summary>
    public QueryWrapper<QueryType, WhereOp<QueryType, QueryOp>> NonNull() {
      return Where(obj => obj != null);
    }

    /// <summary>
    /// Returns a new query operation representing only the elements of the sequence that are valid
    /// unity objects.
    /// </summary>
    public QueryWrapper<QueryType, WhereOp<QueryType, QueryOp>> ValidUnityObjs() {
      return Where(obj => (obj as UnityEngine.Object) != null);
    }
  }
}
