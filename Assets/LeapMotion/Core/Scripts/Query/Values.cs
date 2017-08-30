using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Query {

  public static class Values {

    public static QueryWrapper<T, SingleOp<T>> Single<T>(T value) {
      return new QueryWrapper<T, SingleOp<T>>(new SingleOp<T>(value));
    }

    public struct SingleOp<T> : IQueryOp<T> {
      private T _t;
      private bool _hasReturned;

      public SingleOp(T t) {
        _t = t;
        _hasReturned = false;
      }

      public bool TryGetNext(out T t) {
        if (_hasReturned) {
          t = default(T);
          return false;
        } else {
          t = _t;
          _hasReturned = true;
          return true;
        }
      }

      public void Reset() {
        _hasReturned = false;
      }
    }

  }
}