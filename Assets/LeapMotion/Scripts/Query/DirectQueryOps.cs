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

    public bool Any() {
      QueryType obj;
      return _op.TryGetNext(out obj);
    }

    public bool Any(Func<QueryType, bool> predicate) {
      return Where(predicate).Any();
    }

    public bool All(Func<QueryType, bool> predicate) {
      QueryType obj;
      while (_op.TryGetNext(out obj)) {
        if (!predicate(obj)) {
          return false;
        }
      }
      return true;
    }

    public bool Contains(QueryType instance) {
      QueryType obj;
      while (_op.TryGetNext(out obj)) {
        if (obj.Equals(instance)) {
          return true;
        }
      }
      return false;
    }

    public int Count() {
      QueryType obj;
      int count = 0;
      while (_op.TryGetNext(out obj)) {
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
      QueryType obj;
      if (!_op.TryGetNext(out obj)) {
        throw new InvalidOperationException("The source query is empty.");
      }

      return obj;
    }

    public QueryType First(Func<QueryType, bool> predicate) {
      return Where(predicate).First();
    }

    public QueryType FirstOrDefault() {
      QueryType obj;
      _op.TryGetNext(out obj);
      return obj;
    }

    public QueryType FirstOrDefault(Func<QueryType, bool> predicate) {
      return Where(predicate).FirstOrDefault();
    }

    public QueryType Fold(Func<QueryType, QueryType, QueryType> foldFunc) {
      QueryType value;
      if (!_op.TryGetNext(out value)) {
        throw new InvalidOperationException();
      }

      QueryType next;
      while (_op.TryGetNext(out next)) {
        value = foldFunc(value, next);
      }

      return value;
    }

    public int IndexOf(QueryType value) {
      QueryType obj;
      int index = 0;
      while (_op.TryGetNext(out obj)) {
        if (obj.Equals(value)) {
          return index;
        }
        index++;
      }
      return -1;
    }

    public int IndexOf(Func<QueryType, bool> predicate) {
      QueryType obj;
      int index = 0;
      while (_op.TryGetNext(out obj)) {
        if (predicate(obj)) {
          return index;
        }
        index++;
      }
      return -1;
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
      QueryType obj;
      while (_op.TryGetNext(out obj)) {
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
      QueryType obj;
      while (_op.TryGetNext(out obj)) {
        list.Add(obj);
      }
    }
  }
}
