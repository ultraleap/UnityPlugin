using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Query {

  public class SkipWhileOp<SourceType, SourceOp> : IEnumerator<SourceType>
    where SourceOp : IEnumerator<SourceType> {

    private SourceOp _source;
    private Func<SourceType, bool> _predicate;
    private bool _finishedSkipping;

    public SkipWhileOp(SourceOp source, Func<SourceType, bool> predicate) {
      _source = source;
      _predicate = predicate;
      _finishedSkipping = false;
    }

    public bool MoveNext() {
      while (!_finishedSkipping) {
        if (!_source.MoveNext()) {
          _finishedSkipping = true;
          return false;
        }

        if (!_predicate(_source.Current)) {
          _finishedSkipping = true;
          return true;
        }
      }

      return _source.MoveNext();
    }

    public SourceType Current {
      get {
        return _source.Current;
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
    }
  }

  public partial struct QueryWrapper<QueryType, QueryOp> where QueryOp : IEnumerator<QueryType> {
    public QueryWrapper<QueryType, SkipWhileOp<QueryType, QueryOp>> SkipWhile(Func<QueryType, bool> predicate) {
      return new QueryWrapper<QueryType, SkipWhileOp<QueryType, QueryOp>>(new SkipWhileOp<QueryType, QueryOp>(_op, predicate));
    }
  }
}
