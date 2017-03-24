using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Query {

  public class SkipCountOp<SourceType, SourceOp> : IEnumerator<SourceType>
    where SourceOp : IEnumerator<SourceType> {

    private SourceOp _source;
    private int _toSkip;

    public SkipCountOp(SourceOp source, int toSkip) {
      _source = source;
      _toSkip = toSkip;
    }

    public bool MoveNext() {
      while (_toSkip != 0 && _source.MoveNext()) {
        _toSkip--;
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
    public QueryWrapper<QueryType, SkipCountOp<QueryType, QueryOp>> Skip(int toSkip) {
      return new QueryWrapper<QueryType, SkipCountOp<QueryType, QueryOp>>(new SkipCountOp<QueryType, QueryOp>(_op, toSkip));
    }
  }
}
