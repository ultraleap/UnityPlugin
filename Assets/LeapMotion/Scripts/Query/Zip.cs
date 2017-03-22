using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Query {

  public class ZipOp<ResultType, SourceAType, SourceBType, SourceAOp, SourceBOp> : IEnumerator<ResultType>
    where SourceAOp : IEnumerator<SourceAType>
    where SourceBOp : IEnumerator<SourceBType> {

    private SourceAOp _sourceA;
    private SourceBOp _sourceB;
    private Func<SourceAType, SourceBType, ResultType> _resultSelector;

    public ZipOp(SourceAOp sourceA, SourceBOp sourceB, Func<SourceAType, SourceBType, ResultType> resultSelector) {
      _sourceA = sourceA;
      _sourceB = sourceB;
      _resultSelector = resultSelector;
    }

    public bool MoveNext() {
      return (_sourceA.MoveNext() && _sourceB.MoveNext());
    }

    public ResultType Current {
      get {
        return _resultSelector(_sourceA.Current, _sourceB.Current);
      }
    }

    object IEnumerator.Current {
      get {
        throw new InvalidOperationException();
      }
    }

    public void Reset() {
      throw new InvalidOperationException();
    }

    public void Dispose() {
      _sourceA.Dispose();
      _sourceB.Dispose();
    }
  }

  public partial struct QueryWrapper<QueryType, QueryOp> where QueryOp : IEnumerator<QueryType> {
    public QueryWrapper<NewType, ZipOp<NewType, QueryType, OtherType, QueryOp, OtherOp>> Zip<NewType, OtherType, OtherOp>(QueryWrapper<OtherType, OtherOp> sourceB, Func<QueryType, OtherType, NewType> resultSelector)
      where OtherOp : IEnumerator<OtherType> {
      return new QueryWrapper<NewType, ZipOp<NewType, QueryType, OtherType, QueryOp, OtherOp>>(new ZipOp<NewType, QueryType, OtherType, QueryOp, OtherOp>(_op, sourceB._op, resultSelector));
    }
  }
}
