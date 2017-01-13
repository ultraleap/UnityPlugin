using System;
using System.Collections.Generic;

namespace Leap.Unity.Query {

  public partial struct QueryWrapper<QueryType, QueryOp> where QueryOp : IEnumerator<QueryType> {

    public bool Any() {
      return op.MoveNext();
    }

    public bool Any(Func<QueryType, bool> predicate) {
      while (op.MoveNext()) {
        if (predicate(op.Current)) {
          return true;
        }
      }
      return false;
    }

    public bool All(Func<QueryType, bool> predicate) {
      while (op.MoveNext()) {
        if (!predicate(op.Current)) {
          return false;
        }
      }
      return true;
    }

    public bool Contains(QueryType instance) {
      while (op.MoveNext()) {
        if (op.Current.Equals(instance)) {
          return true;
        }
      }
      return false;
    }

    public int Count() {
      int count = 0;
      while (op.MoveNext()) {
        count++;
      }
      return count;
    }

    public int Count(Func<QueryType, bool> predicate) {
      int count = 0;
      while (op.MoveNext()) {
        if (predicate(op.Current)) {
          count++;
        }
      }
      return count;
    }

    public QueryType First() {
      if (!op.MoveNext()) {
        throw new InvalidOperationException("The source query is empty.");
      }

      return op.Current;
    }

    public QueryType First(Func<QueryType, bool> predicate) {
      while (true) {
        if (!op.MoveNext()) {
          throw new InvalidOperationException("The source query did not have any elements that satisfied the predicate.");
        }

        if (predicate(op.Current)) {
          return op.Current;
        }
      }
    }

    public QueryType FirstOrDefault() {
      if (!op.MoveNext()) {
        return default(QueryType);
      }

      return op.Current;
    }

    public QueryType FirstOrDefault(Func<QueryType, bool> predicate) {
      while (true) {
        if (!op.MoveNext()) {
          return default(QueryType);
        }

        if (predicate(op.Current)) {
          return op.Current;
        }
      }
    }
  }
}
