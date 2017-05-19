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

  public struct OfTypeOp<SourceType, ResultType, SourceOp> : IQueryOp<ResultType>
    where SourceOp : IQueryOp<SourceType>
    where ResultType : class {
    private SourceOp _source;

    public OfTypeOp(SourceOp source) {
      _source = source;
    }

    public bool TryGetNext(out ResultType t) {
      SourceType sourceObj;
      while (true) {
        if (!_source.TryGetNext(out sourceObj)) {
          t = default(ResultType);
          return false;
        }

        if (sourceObj is ResultType) {
          t = sourceObj as ResultType;
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
    /// Returns a new query operation representing only the items of the current sequence that
    /// are of a specific type.
    /// 
    /// For example
    ///   ("A", 1, null, 5.0f, 900, "hello").Query().OfType<string>()
    /// would result in
    ///   ("A", "hello")
    /// </summary>
    public QueryWrapper<CastType, OfTypeOp<QueryType, CastType, QueryOp>> OfType<CastType>() where CastType : class {
      return new QueryWrapper<CastType, OfTypeOp<QueryType, CastType, QueryOp>>(new OfTypeOp<QueryType, CastType, QueryOp>(_op));
    }

    /// <summary>
    /// Returns a new query operation representing only the items of the current sequence that
    /// are of a specific type.
    /// 
    /// For example
    ///   ("A", 1, null, 5.0f, 900, "hello").Query().OfType(typeof(int))
    /// would result in
    ///   (1, 900)
    /// </summary>
    public QueryWrapper<QueryType, WhereOp<QueryType, QueryOp>> OfType(Type type) {
      return new QueryWrapper<QueryType, WhereOp<QueryType, QueryOp>>(new WhereOp<QueryType, QueryOp>(_op, element => element != null && type.IsAssignableFrom(element.GetType())));
    }
  }
}
