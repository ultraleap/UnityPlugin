using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Query {

  public class SelectManyOp<SourceType, ResultType, SourceOp, ResultOp> : IEnumerator<ResultType>
    where SourceOp : IEnumerator<SourceType>
    where ResultOp : IEnumerator<ResultType> {

    private SourceOp _source;
    private Func<SourceType, QueryWrapper<ResultType, ResultOp>> _selector;

    private ResultOp _innerSource;

    public SelectManyOp(SourceOp source, Func<SourceType, QueryWrapper<ResultType, ResultOp>> selector) {
      _source = source;
      _selector = selector;
    }

    public bool MoveNext() {
      if (!_innerSource.MoveNext()) {
        if (!_source.MoveNext()) {
          return false;
        }
        _innerSource = _selector(_source.Current).GetEnumerator();
      }
      return true;
    }

    public ResultType Current {
      get {
        return _innerSource.Current;
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
      _source.Dispose();
      _innerSource.Dispose();
      _selector = null;
    }
  }

  public partial struct QueryWrapper<QueryType, QueryOp> where QueryOp : IEnumerator<QueryType> {
    public QueryWrapper<NewType, SelectManyOp<QueryType, NewType, QueryOp, NewOp>> SelectMany<NewType, NewOp>(Func<QueryType, QueryWrapper<NewType, NewOp>> selector)
      where NewOp : IEnumerator<NewType> {
      return new QueryWrapper<NewType, SelectManyOp<QueryType, NewType, QueryOp, NewOp>>(new SelectManyOp<QueryType, NewType, QueryOp, NewOp>(_op, selector));
    }
  }
}
