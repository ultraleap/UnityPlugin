/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using System.Collections.Generic;

namespace Leap.Unity.Query {

  public partial struct QueryWrapper<QueryType, QueryOp> where QueryOp : IQueryOp<QueryType> {

    public bool All(Func<QueryType, bool> predicate) {
      var op = _op;

      QueryType obj;
      while (op.TryGetNext(out obj)) {
        if (!predicate(obj)) {
          return false;
        }
      }
      return true;
    }

    public bool AllEqual() {
      var op = _op;

      QueryType a;
      if (!op.TryGetNext(out a)) {
        return true;
      }

      QueryType b;
      while (op.TryGetNext(out b)) {
        if ((a == null) != (b == null)) {
          return false;
        }

        if (a == null && b == null) {
          continue;
        }

        if (!a.Equals(b)) {
          return false;
        }
      }

      return true;
    }

    public bool Any() {
      var op = _op;

      QueryType obj;
      return op.TryGetNext(out obj);
    }

    public bool Any(Func<QueryType, bool> predicate) {
      return Where(predicate).Any();
    }

    public bool Contains(QueryType instance) {
      var op = _op;

      QueryType obj;
      while (op.TryGetNext(out obj)) {
        if (obj.Equals(instance)) {
          return true;
        }
      }
      return false;
    }

    public int Count() {
      var op = _op;

      QueryType obj;
      int count = 0;
      while (op.TryGetNext(out obj)) {
        count++;
      }
      return count;
    }

    public int Count(Func<QueryType, bool> predicate) {
      return Where(predicate).Count();
    }

    public QueryType ElementAt(int index) {
      return Skip(index).First();
    }

    public QueryType ElementAtOrDefault(int index) {
      return Skip(index).FirstOrDefault();
    }

    public QueryType First() {
      var op = _op;

      QueryType obj;
      if (!op.TryGetNext(out obj)) {
        throw new InvalidOperationException("The source query is empty.");
      }

      return obj;
    }

    public QueryType First(Func<QueryType, bool> predicate) {
      return Where(predicate).First();
    }

    public QueryType FirstOrDefault() {
      var op = _op;

      QueryType obj;
      op.TryGetNext(out obj);
      return obj;
    }

    public QueryType FirstOrDefault(Func<QueryType, bool> predicate) {
      return Where(predicate).FirstOrDefault();
    }

    public QueryType Fold(Func<QueryType, QueryType, QueryType> foldFunc) {
      var op = _op;

      QueryType value;
      if (!op.TryGetNext(out value)) {
        throw new InvalidOperationException();
      }

      QueryType next;
      while (op.TryGetNext(out next)) {
        value = foldFunc(value, next);
      }

      return value;
    }

    public int IndexOf(QueryType value) {
      var op = _op;

      QueryType obj;
      int index = 0;
      while (op.TryGetNext(out obj)) {
        if (obj.Equals(value)) {
          return index;
        }
        index++;
      }
      return -1;
    }

    public int IndexOf(Func<QueryType, bool> predicate) {
      var op = _op;

      QueryType obj;
      int index = 0;
      while (op.TryGetNext(out obj)) {
        if (predicate(obj)) {
          return index;
        }
        index++;
      }
      return -1;
    }

    public QueryType Last() {
      var op = _op;

      QueryType obj;
      if (!op.TryGetNext(out obj)) {
        throw new InvalidOperationException("The source query is empty!");
      }

      while (op.TryGetNext(out obj)) { }

      return obj;
    }

    public QueryType Last(Func<QueryType, bool> predicate) {
      return Where(predicate).Last();
    }

    public QueryType LastOrDefault() {
      var op = _op;

      QueryType obj = default(QueryType);
      while (op.TryGetNext(out obj)) { }
      return obj;
    }

    public QueryType LastOrDefault(Func<QueryType, bool> predicate) {
      return Where(predicate).LastOrDefault();
    }

    public QueryType Single() {
      var op = _op;

      QueryType obj;
      if (!op.TryGetNext(out obj)) {
        throw new InvalidOperationException("The source query is empty!");
      }

      QueryType dummy;
      if (op.TryGetNext(out dummy)) {
        throw new InvalidOperationException("The source query had more than a single elemeny!");
      }

      return obj;
    }

    public QueryType Single(Func<QueryType, bool> predicate) {
      return Where(predicate).Single();
    }

    private static List<QueryType> _utilityList = new List<QueryType>();
    public QueryType[] ToArray() {
      try {
        AppendList(_utilityList);
        return _utilityList.ToArray();
      } finally {
        _utilityList.Clear();
      }
    }

    public void FillArray(QueryType[] array, int offset = 0) {
      var op = _op;

      QueryType obj;
      while (op.TryGetNext(out obj)) {
        array[offset++] = obj;
      }
    }

    public List<QueryType> ToList() {
      List<QueryType> list = new List<QueryType>();
      AppendList(list);
      return list;
    }

    public void FillList(List<QueryType> list) {
      list.Clear();
      AppendList(list);
    }

    public void AppendList(List<QueryType> list) {
      var op = _op;

      QueryType obj;
      while (op.TryGetNext(out obj)) {
        list.Add(obj);
      }
    }
  }
}
