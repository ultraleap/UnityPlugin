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

  public struct ZipOp<ResultType, SourceAType, SourceBType, SourceAOp, SourceBOp> : IQueryOp<ResultType>
    where SourceAOp : IQueryOp<SourceAType>
    where SourceBOp : IQueryOp<SourceBType> {

    private SourceAOp _sourceA;
    private SourceBOp _sourceB;
    private Func<SourceAType, SourceBType, ResultType> _resultSelector;

    public ZipOp(SourceAOp sourceA, SourceBOp sourceB, Func<SourceAType, SourceBType, ResultType> resultSelector) {
      _sourceA = sourceA;
      _sourceB = sourceB;
      _resultSelector = resultSelector;
    }

    public bool TryGetNext(out ResultType t) {
      SourceAType a;
      SourceBType b;
      if (_sourceA.TryGetNext(out a) && _sourceB.TryGetNext(out b)) {
        t = _resultSelector(a, b);
        return true;
      } else {
        t = default(ResultType);
        return false;
      }
    }

    public void Reset() {
      _sourceA.Reset();
      _sourceB.Reset();
    }
  }

  public partial struct QueryWrapper<QueryType, QueryOp> where QueryOp : IQueryOp<QueryType> {

    /// <summary>
    /// Returns a new query operation that represents the combination of this query sequence with another
    /// query sequence.  The two sequences are combined element-by-element using a selector function.
    /// The resulting sequence has a length equal to the smaller of the two sequences.
    /// 
    /// For example:
    ///   sequenceA = (A, B, C, D)
    ///   sequenceB = (E, F, G, H)
    ///   sequenceA.Query().Zip(sequenceB.Query(), (a, b) => a + b)
    /// Would result in:
    ///   (AE, BF, CG, DH)
    /// </summary>
    public QueryWrapper<NewType, ZipOp<NewType, QueryType, OtherType, QueryOp, OtherOp>> Zip<NewType, OtherType, OtherOp>(QueryWrapper<OtherType, OtherOp> sourceB, Func<QueryType, OtherType, NewType> resultSelector)
      where OtherOp : IQueryOp<OtherType> {
      return new QueryWrapper<NewType, ZipOp<NewType, QueryType, OtherType, QueryOp, OtherOp>>(new ZipOp<NewType, QueryType, OtherType, QueryOp, OtherOp>(_op, sourceB._op, resultSelector));
    }
  }
}
