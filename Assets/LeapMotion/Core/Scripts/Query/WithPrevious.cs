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

  public struct WithPreviousOp<SourceType, SourceOp> : IQueryOp<PrevPair<SourceType>>
    where SourceOp : IQueryOp<SourceType> {
    private SourceOp _mainOp;
    private SourceOp _delayedOp;

    private bool _includeStart;
    private int _offsetLeft;
    private int _offset;

    public WithPreviousOp(SourceOp op, int offset, bool includeStart) {
      if (offset <= 0) {
        throw new ArgumentException("Offset must be larger than zero.");
      }

      _mainOp = op;
      _delayedOp = op;

      _includeStart = includeStart;
      _offsetLeft = offset;
      _offset = offset;
    }

    public bool TryGetNext(out PrevPair<SourceType> t) {
      top:

      SourceType value;
      if (_mainOp.TryGetNext(out value)) {
        if (_offsetLeft > 0) {
          _offsetLeft--;
          if (!_includeStart) {
            goto top;
          }

          t = new PrevPair<SourceType>() {
            value = value,
            prev = default(SourceType),
            hasPrev = false
          };
        } else {
          SourceType prev;
          _delayedOp.TryGetNext(out prev);
          t = new PrevPair<SourceType>() {
            value = value,
            prev = prev,
            hasPrev = true
          };
        }
        return true;
      } else {
        t = default(PrevPair<SourceType>);
        return false;
      }
    }

    public void Reset() {
      _mainOp.Reset();
      _delayedOp.Reset();
      _offsetLeft = _offset;
    }
  }

  public partial struct QueryWrapper<QueryType, QueryOp> where QueryOp : IQueryOp<QueryType> {

    /// <summary>
    /// Returns a new query operation where each new element in the sequence is an instance of the PrevPair struct.
    /// The value field of the pair will point to an element in the current sequence, and the prev field will
    /// point to an element that comes 'offset' elements before the current element.
    /// 
    /// For example, with an offset of 2, the sequence:
    ///   A, B, C, D, E, F
    /// is transformed into:
    ///   (C,A) (D,B) (E,C) (F,D)
    /// </summary>
    public QueryWrapper<PrevPair<QueryType>, WithPreviousOp<QueryType, QueryOp>> WithPrevious(int offset = 1) {
      return new QueryWrapper<PrevPair<QueryType>, WithPreviousOp<QueryType, QueryOp>>(new WithPreviousOp<QueryType, QueryOp>(_op, offset, includeStart: false));
    }

    /// <summary>
    /// Returns a new query operation where each new element in the sequence is an instance of the PrevPair struct.
    /// The value field of the pair will point to an element in the current sequence, and the prev field will
    /// point to an element that comes before the current element.  If 'includeStart' is true, the sequence will
    /// also include elements that have no previous element.
    /// 
    /// For example, includeStart as true, the sequence:
    ///   A, B, C, D, E, F
    /// is transformed into:
    ///   (A,_) (B,A) (C,B) (D,C) (E,D) (F,E)
    /// </summary>
    public QueryWrapper<PrevPair<QueryType>, WithPreviousOp<QueryType, QueryOp>> WithPrevious(bool includeStart) {
      return new QueryWrapper<PrevPair<QueryType>, WithPreviousOp<QueryType, QueryOp>>(new WithPreviousOp<QueryType, QueryOp>(_op, 1, includeStart));
    }

    /// <summary>
    /// Returns a new query operation where each new element in the sequence is an instance of the PrevPair struct.
    /// The value field of the pair will point to an element in the current sequence, and the prev field will
    /// point to an element that comes 'offset' elements before the current element. If 'includeStart' is true, 
    /// the sequence will also include elements that have no previous element.
    /// 
    /// For example, with an offset of 2 and with includeStart as true, the sequence:
    ///   A, B, C, D, E, F
    /// is transformed into:
    ///   (A,_) (B,_) (C,A) (D,B) (E,C) (F,D)
    /// </summary>
    public QueryWrapper<PrevPair<QueryType>, WithPreviousOp<QueryType, QueryOp>> WithPrevious(int offset, bool includeStart) {
      return new QueryWrapper<PrevPair<QueryType>, WithPreviousOp<QueryType, QueryOp>>(new WithPreviousOp<QueryType, QueryOp>(_op, offset, includeStart));
    }
  }

  public struct PrevPair<T> {
    /// <summary>
    /// The current element of the sequence
    /// </summary>
    public T value;

    /// <summary>
    /// If hasPrev is true, the element that came before value
    /// </summary>
    public T prev;

    /// <summary>
    /// Does the prev field represent a previous value?  If false,
    /// prev will take the default value of T.
    /// </summary>
    public bool hasPrev;
  }
}
