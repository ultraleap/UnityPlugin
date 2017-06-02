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

    /// <summary>
    /// Returns true if all elements in the sequence satisfy the predicate.
    /// </summary>
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

    /// <summary>
    /// Returns true if all elements in the sequence are equal to the same value.
    /// Will always return true for sequences with one or zero elements.
    /// </summary>
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

    /// <summary>
    /// Returns true if the sequence has any elements in it.
    /// </summary>
    public bool Any() {
      var op = _op;

      QueryType obj;
      return op.TryGetNext(out obj);
    }

    /// <summary>
    /// Returns true if any elements in the sequence satisfy the predicate.
    /// </summary>
    public bool Any(Func<QueryType, bool> predicate) {
      return Where(predicate).Any();
    }

    /// <summary>
    /// Returns true if any element in the sequence is equal to a specific value.
    /// </summary>
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

    /// <summary>
    /// Returns the number of elements in the sequence.
    /// </summary>
    public int Count() {
      var op = _op;

      QueryType obj;
      int count = 0;
      while (op.TryGetNext(out obj)) {
        count++;
      }
      return count;
    }

    /// <summary>
    /// Returns the number of elements in the sequence that satisfy a predicate.
    /// </summary>
    public int Count(Func<QueryType, bool> predicate) {
      return Where(predicate).Count();
    }

    /// <summary>
    /// Returns the element at a specific index in the sequence.  Will throw an error
    /// if the sequence has no element at that index.
    /// </summary>
    public QueryType ElementAt(int index) {
      return Skip(index).First();
    }

    /// <summary>
    /// Returns the element at a specific index in the sequence.  Will return
    /// the default value if the sequence has no element at that index.
    /// </summary>
    public QueryType ElementAtOrDefault(int index) {
      return Skip(index).FirstOrDefault();
    }

    /// <summary>
    /// Returns the first element in the sequence.  Will throw an error if there are
    /// no elements in the sequence.
    /// </summary>
    public QueryType First() {
      var op = _op;

      QueryType obj;
      if (!op.TryGetNext(out obj)) {
        throw new InvalidOperationException("The source query is empty.");
      }

      return obj;
    }

    /// <summary>
    /// Returns the first element in the sequence that satisfies a predicate.  Will
    /// throw an error if there is no such element.
    /// </summary>
    public QueryType First(Func<QueryType, bool> predicate) {
      return Where(predicate).First();
    }

    /// <summary>
    /// Returns the first element in the sequence.  Will return the default value
    /// if the sequence is empty.
    /// </summary>
    public QueryType FirstOrDefault() {
      var op = _op;

      QueryType obj;
      op.TryGetNext(out obj);
      return obj;
    }

    /// <summary>
    /// Returns the first element in the sequence that satisfies a predicate.  Will return
    /// the default value if there is no such element.
    /// </summary>
    public QueryType FirstOrDefault(Func<QueryType, bool> predicate) {
      return Where(predicate).FirstOrDefault();
    }

    /// <summary>
    /// Folds all of the elements in the sequence into a single element, using a fold function.
    /// Will throw an error if there are no elements in the sequence.
    /// 
    /// The fold function takes in the current folded value, and the next item to fold in.
    /// It returns the result of folding the item into the current folded value.  For example,
    /// you can use the Fold operation to implement a sum:
    /// 
    /// var sum = numbers.Query().Fold((a,b) => a + b);
    /// </summary>
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

    /// <summary>
    /// Returns the index of the first element that is equal to a specific value.  Will return
    /// a negative index if there is no such element.
    /// </summary>
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

    /// <summary>
    /// Returns the index of the first element to satisfy a predicate.  Will return a negative
    /// index if there is no such element.
    /// </summary>
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

    /// <summary>
    /// Returns the last element in the sequence.  Will throw an error if the sequence is empty.
    /// </summary>
    public QueryType Last() {
      var op = _op;

      QueryType obj, temp;
      if (!op.TryGetNext(out obj)) {
        throw new InvalidOperationException("The source query is empty!");
      }

      while (op.TryGetNext(out temp)) {
        obj = temp;
      }

      return obj;
    }

    /// <summary>
    /// Returns the last element in the sequence that satisfies a predicate.  Will throw an error
    /// if there is no such element.
    /// </summary>
    public QueryType Last(Func<QueryType, bool> predicate) {
      return Where(predicate).Last();
    }

    /// <summary>
    /// Returns the last element in the sequence.  Will return the default value if the sequence is empty.
    /// </summary>
    public QueryType LastOrDefault() {
      var op = _op;

      QueryType obj = default(QueryType);
      while (op.TryGetNext(out obj)) { }
      return obj;
    }

    /// <summary>
    /// Returns the last element in the sequence that satisfies a predicate.  Will return the default
    /// value if there is no such element.
    /// </summary>
    public QueryType LastOrDefault(Func<QueryType, bool> predicate) {
      return Where(predicate).LastOrDefault();
    }

    /// <summary>
    /// Returns the first and only element in the sequence.  Will throw an error if the length of the 
    /// sequence is anything other than 1.
    /// </summary>
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

    /// <summary>
    /// Returns the first and only element in the sequence that satisfies the predicate.  Will throw
    /// an error if the number of such elements is anything other than 1.
    /// </summary>
    public QueryType Single(Func<QueryType, bool> predicate) {
      return Where(predicate).Single();
    }

    private static List<QueryType> _utilityList = new List<QueryType>();

    /// <summary>
    /// Converts the sequence into an array.
    /// </summary>
    public QueryType[] ToArray() {
      try {
        AppendList(_utilityList);
        return _utilityList.ToArray();
      } finally {
        _utilityList.Clear();
      }
    }

    /// <summary>
    /// Copies the elements of the sequence into an array.  Can optionally specify the offset into the array
    /// where to copy.
    /// </summary>
    public void FillArray(QueryType[] array, int offset = 0) {
      var op = _op;

      QueryType obj;
      while (op.TryGetNext(out obj)) {
        array[offset++] = obj;
      }
    }

    /// <summary>
    /// Converts the sequence into a list.
    /// </summary>
    public List<QueryType> ToList() {
      List<QueryType> list = new List<QueryType>();
      AppendList(list);
      return list;
    }

    /// <summary>
    /// Fills a given list with the elements in this sequence.  The list is cleared before the fill happens.
    /// </summary>
    public void FillList(List<QueryType> list) {
      list.Clear();
      AppendList(list);
    }

    /// <summary>
    /// Appends the elements in this sequence to the end of a given list.
    /// </summary>
    public void AppendList(List<QueryType> list) {
      var op = _op;

      QueryType obj;
      while (op.TryGetNext(out obj)) {
        list.Add(obj);
      }
    }

    public void FillHashSet(HashSet<QueryType> hashSet) {
      hashSet.Clear();
      AppendHashSet(hashSet);
    }

    public void AppendHashSet(HashSet<QueryType> hashSet) {
      var op = _op;

      QueryType obj;
      while (op.TryGetNext(out obj)) {
        hashSet.Add(obj);
      }
    }
  }
}
