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

  public struct SelectOp<SourceType, ResultType, SourceOp> : IQueryOp<ResultType>
    where SourceOp : IQueryOp<SourceType> {
    private SourceOp _source;
    private Func<SourceType, ResultType> _mapping;

    public SelectOp(SourceOp enumerator, Func<SourceType, ResultType> mapping) {
      _source = enumerator;
      _mapping = mapping;
    }

    public bool TryGetNext(out ResultType t) {
      SourceType sourceObj;
      if (_source.TryGetNext(out sourceObj)) {
        t = _mapping(sourceObj);
        return true;
      } else {
        t = default(ResultType);
        return false;
      }
    }

    public void Reset() {
      _source.Reset();
    }
  }

  public partial struct QueryWrapper<QueryType, QueryOp> where QueryOp : IQueryOp<QueryType> {

    /// <summary>
    /// Returns a new query operation representing the current query sequence mapped element-by-element
    /// into a new query sequence by a mapping operation.
    /// 
    /// For example:
    ///   (1, 2, 3, 4).Query().Select(num => (num * 2).ToString())
    /// Would result in:
    ///   ("2", "4", "6", "8")
    /// </summary>
    public QueryWrapper<NewType, SelectOp<QueryType, NewType, QueryOp>> Select<NewType>(Func<QueryType, NewType> mapping) {
      return new QueryWrapper<NewType, SelectOp<QueryType, NewType, QueryOp>>(new SelectOp<QueryType, NewType, QueryOp>(_op, mapping));
    }

    /// <summary>
    /// Returns a new query operation representing the current query sequence where each element is cast
    /// to a new type.
    /// </summary>
    public QueryWrapper<NewType, SelectOp<QueryType, NewType, QueryOp>> Cast<NewType>() where NewType : class {
      return Select(obj => obj as NewType);
    }
  }
}
