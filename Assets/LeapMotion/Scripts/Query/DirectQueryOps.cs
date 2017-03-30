using System;
using System.Collections.Generic;

namespace Leap.Unity.Query {

  public partial struct QueryWrapper<QueryType, QueryOp> where QueryOp : IEnumerator<QueryType> {

    public bool Any() {
      using (thisAndConsume) {
        return _op.MoveNext();
      }
    }

    public bool Any(Func<QueryType, bool> predicate) {
      using (thisAndConsume) {
        while (_op.MoveNext()) {
          if (predicate(_op.Current)) {
            return true;
          }
        }
        return false;
      }
    }

    public bool All(Func<QueryType, bool> predicate) {
      using (thisAndConsume) {
        while (_op.MoveNext()) {
          if (!predicate(_op.Current)) {
            return false;
          }
        }
        return true;
      }
    }

    public bool Contains(QueryType instance) {
      using (thisAndConsume) {
        while (_op.MoveNext()) {
          if (_op.Current.Equals(instance)) {
            return true;
          }
        }
        return false;
      }
    }

    public int Count() {
      using (thisAndConsume) {
        int count = 0;
        while (_op.MoveNext()) {
          count++;
        }
        return count;
      }
    }

    public int Count(Func<QueryType, bool> predicate) {
      using (thisAndConsume) {
        int count = 0;
        while (_op.MoveNext()) {
          if (predicate(_op.Current)) {
            count++;
          }
        }
        return count;
      }
    }

    public QueryType ElementAt(int index) {
      return Skip(index).First();
    }

    public QueryType ElementAtOrDefault(int index) {
      return Skip(index).FirstOrDefault();
    }

    public QueryType First() {
      using (thisAndConsume) {
        if (!_op.MoveNext()) {
          throw new InvalidOperationException("The source query is empty.");
        }

        return _op.Current;
      }
    }

    public QueryType First(Func<QueryType, bool> predicate) {
      using (thisAndConsume) {
        while (true) {
          if (!_op.MoveNext()) {
            throw new InvalidOperationException("The source query did not have any elements that satisfied the predicate.");
          }

          if (predicate(_op.Current)) {
            return _op.Current;
          }
        }
      }
    }

    public QueryType FirstOrDefault() {
      using (thisAndConsume) {
        if (!_op.MoveNext()) {
          return default(QueryType);
        }

        return _op.Current;
      }
    }

    public QueryType FirstOrDefault(Func<QueryType, bool> predicate) {
      using (thisAndConsume) {
        while (true) {
          if (!_op.MoveNext()) {
            return default(QueryType);
          }

          if (predicate(_op.Current)) {
            return _op.Current;
          }
        }
      }
    }

    public QueryType Fold(Func<QueryType, QueryType, QueryType> foldFunc) {
      using (thisAndConsume) {
        if (!_op.MoveNext()) {
          throw new InvalidOperationException();
        }
        QueryType value = _op.Current;

        while (_op.MoveNext()) {
          value = foldFunc(value, _op.Current);
        }

        return value;
      }
    }

    public int IndexOf(QueryType value) {
      using (thisAndConsume) {
        int index = 0;
        while (_op.MoveNext()) {
          if (_op.Current.Equals(value)) {
            return index;
          }
          index++;
        }
      }
      return -1;
    }

    public int IndexOf(Func<QueryType, bool> predicate) {
      using (thisAndConsume) {
        int index = 0;
        while (_op.MoveNext()) {
          if (predicate(_op.Current)) {
            return index;
          }
          index++;
        }
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
      using (thisAndConsume) {
        while (_op.MoveNext()) {
          array[offset++] = _op.Current;
        }
      }
    }

    public List<QueryType> ToList() {
      List<QueryType> list = new List<QueryType>();
      using (thisAndConsume) {
        while (_op.MoveNext()) {
          list.Add(_op.Current);
        }
      }
      return list;
    }

    public void FillList(List<QueryType> list) {
      using (thisAndConsume) {
        list.Clear();
        while (_op.MoveNext()) {
          list.Add(_op.Current);
        }
      }
    }

    public void AppendList(List<QueryType> list) {
      using (thisAndConsume) {
        while (_op.MoveNext()) {
          list.Add(_op.Current);
        }
      }
    }
  }
}
