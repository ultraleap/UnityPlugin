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

    public static QueryWrapper<int, RangeOp> From(int from) {
      return new QueryWrapper<int, RangeOp>(new RangeOp(from, int.MaxValue, step: 1));
    }

    public static QueryWrapper<int, RangeOp> To(int to) {
      return new QueryWrapper<int, RangeOp>(new RangeOp(0, to, step: 1));
    }

    public static QueryWrapper<int, RangeOp> To(this QueryWrapper<int, RangeOp> wrapper, int to) {
      return new QueryWrapper<int, RangeOp>(new RangeOp(wrapper.op.from, to, wrapper.op.step));
    }

    public static QueryWrapper<int, RangeOp> By(this QueryWrapper<int, RangeOp> wrapper, int step) {
      return new QueryWrapper<int, RangeOp>(new RangeOp(wrapper.op.from, wrapper.op.to, step));
    }

    public struct RangeOp : IQueryOp<int> {
      public readonly int from, to, step;
      private int _curr;

      public RangeOp(int from, int to, int step) {
        this.from = from;
        this.to = step == 0 ? from : to;
        this.step = to > from ? Mathf.Abs(step) : -Mathf.Abs(step);

        _curr = this.from;
      }

      public bool TryGetNext(out int t) {
        t = _curr;

        if (_curr == to) {
          return false;
        }

        if ((_curr > to) == (to > from)) {
          return false;
        }

        _curr += step;
        return true;
      }

      public void Reset() {
        _curr = from;
      }
    }
  }
}