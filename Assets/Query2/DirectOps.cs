using System;
using System.Collections.Generic;

namespace Leap.Unity.Query2 {

  public partial struct Query<T> {

    public bool All(Func<T, bool> predicate) {
      using (this) {
        var array = _data.array;
        for (int i = 0; i < _count; i++) {
          if (!predicate(array[i])) {
            return false;
          }
        }
        return true;
      }
    }

    public bool AllEqual() {
      using (this) {
        if (_count <= 1) {
          return true;
        }

        var array = _data.array;
        var a = array[0];
        for (int i = 1; i < _count; i++) {
          var b = array[i];

          if ((a == null) != (b == null)) {
            return false;
          }

          if ((a == null) && (b == null)) {
            continue;
          }

          if (!a.Equals(b)) {
            return false;
          }
        }

        return true;
      }
    }

    public bool Any() {
      using (this) {
        return _count > 0;
      }
    }

    public bool Any(Func<T, bool> predicate) {
      using (this) {
        var array = _data.array;

        for (int i = 0; i < _count; i++) {
          if (predicate(array[i])) {
            return true;
          }
        }

        return false;
      }
    }

    public int Count() {
      using (this) {
        return _count;
      }
    }

    public int Count(Func<T, bool> predicate) {
      return Where(predicate).Count();
    }

    public int CountUnique() {
      using (this) {
        var array = _data.array;

        var set = Pool<HashSet<T>>.Spawn();
        for (int i = 0; i < _count; i++) {
          set.Add(array[i]);
        }

        int unique = set.Count;
        set.Clear();
        Pool<HashSet<T>>.Recycle(set);
        return unique;
      }
    }

    public int CountUnique<K>(Func<T, K> selector) {
      return Select(selector).CountUnique();
    }

    public T ElementAt(int index) {
      using (this) {
        if (index < 0 || index >= _count) {
          throw new IndexOutOfRangeException("The index " + index + " was out of range.  Query only has length of " + _count);
        }

        return _data.array[index];
      }
    }

    public T ElementAtOrDefault(int index) {
      using (this) {
        if (index < 0 || index >= _count) {
          return default(T);
        } else {
          return _data.array[index];
        }
      }
    }

    public T First() {
      using (this) {
        if (_count == 0) {
          throw new InvalidOperationException("The source Query was empty.");
        }
        return _data.array[0];
      }
    }

    public T First(Func<T, bool> predicate) {
      return Where(predicate).First();
    }

    public T FirstOrDefault() {
      using (this) {
        if (_count == 0) {
          return default(T);
        } else {
          return _data.array[0];
        }
      }
    }

    public T FirstOrDefault(Func<T, bool> predicate) {
      return Where(predicate).FirstOrDefault();
    }

    public Maybe<T> FirstOrNone() {
      using (this) {
        if (_count == 0) {
          return Maybe.None;
        } else {
          return Maybe.Some(_data.array[0]);
        }
      }
    }

    public Maybe<T> FirstOrNone(Func<T, bool> predicate) {
      return Where(predicate).FirstOrNone();
    }

    public T Fold(Func<T, T, T> foldFunc) {
      using (this) {
        if (_count == 0) {
          throw new InvalidOperationException("The source Query was empty.");
        }

        var array = _data.array;
        var result = array[0];
        for (int i = 1; i < _count; i++) {
          result = foldFunc(result, array[i]);
        }

        return result;
      }
    }

    public int IndexOf(T t) {
      using (this) {
        var array = _data.array;

        for (int i = 0; i < _count; i++) {
          if (t == null) {
            if (array[i] == null) {
              return i;
            }
          } else if (t.Equals(array[i])) {
            return i;
          }
        }

        return -1;
      }
    }

    public int IndexOf(Func<T, bool> predicate) {
      using (this) {
        var array = _data.array;

        for (int i = 0; i < _count; i++) {
          if (predicate(array[i])) {
            return i;
          }
        }

        return -1;
      }
    }

    public T Last() {
      using (this) {
        if (_count == 0) {
          throw new InvalidOperationException("The source Query was empty.");
        } else {
          return _data.array[_count - 1];
        }
      }
    }

    public T Last(Func<T, bool> predicate) {
      return Where(predicate).Last();
    }

    public T LastOrDefault() {
      using (this) {
        if (_count == 0) {
          return default(T);
        } else {
          return _data.array[_count - 1];
        }
      }
    }

    public Maybe<T> LastOrNone() {
      using (this) {
        if (_count == 0) {
          return Maybe.None;
        } else {
          return Maybe.Some(_data.array[_count - 1]);
        }
      }
    }

    public Maybe<T> LastOrNone(Func<T, bool> predicate) {
      return Where(predicate).LastOrNone();
    }

    public T Single() {
      using (this) {
        if (_count != 1) {
          throw new InvalidOperationException("The Query had a count of " + _count + " instead of a count of 1.");
        } else {
          return _data.array[0];
        }
      }
    }

    public T Single(Func<T, bool> predicate) {
      return Where(predicate).Single();
    }

    public T SingleOrDefault() {
      using (this) {
        if (_count != 1) {
          return default(T);
        } else {
          return _data.array[0];
        }
      }
    }

    public T SingleOrDefault(Func<T, bool> predicate) {
      return Where(predicate).SingleOrDefault();
    }

    public Maybe<T> SingleOrNone() {
      using (this) {
        if (_count != 1) {
          return Maybe.None;
        } else {
          return Maybe.Some(_data.array[0]);
        }
      }
    }

    public Maybe<T> SingleOrNone(Func<T, bool> predicate) {
      return Where(predicate).SingleOrNone();
    }

    public T UniformOrDefault() {
      return UniformOrNone().valueOrDefault;
    }

    public Maybe<T> UniformOrNone() {
      using (this) {
        if (_count == 0) {
          return Maybe.None;
        }

        var array = _data.array;
        T reference = array[0];

        for (int i = 1; i < _count; i++) {
          if (reference == null) {
            if (array[i] != null) {
              return Maybe.None;
            }
          } else if (!reference.Equals(array[i])) {
            return Maybe.None;
          }
        }

        return Maybe.Some(reference);
      }
    }

    public T[] ToArray() {
      using (this) {
        T[] result = new T[_count];
        Array.Copy(_data.array, result, _count);
        return result;
      }
    }

    public void FillArray(T[] array, int offset = 0) {
      CopyTo(array, offset);
    }

    public List<T> ToList() {
      List<T> result = new List<T>(_count);
      AppendList(result);
      return result;
    }

    public void FillList(List<T> list) {
      list.Clear();
      AppendList(list);
    }

    public void AppendList(List<T> list) {
      using (this) {
        for (int i = 0; i < _count; i++) {
          list.Add(_data.array[i]);
        }
      }
    }

    public HashSet<T> ToHashSet() {
      HashSet<T> set = new HashSet<T>();
      AppendHashSet(set);
      return set;
    }

    public void FillHashSet(HashSet<T> set) {
      set.Clear();
      AppendHashSet(set);
    }

    public void AppendHashSet(HashSet<T> set) {
      using (this) {
        for (int i = 0; i < _count; i++) {
          set.Add(_data.array[i]);
        }
      }
    }

    public Dictionary<K, V> ToDictionary<K, V>(Func<T, K> keySelector, Func<T, V> valueSelector) {
      using (this) {
        Dictionary<K, V> dictionary = new Dictionary<K, V>();

        for (int i = 0; i < _count; i++) {
          dictionary[keySelector(_data.array[i])] = valueSelector(_data.array[i]);
        }

        return dictionary;
      }
    }

    public Dictionary<T, V> ToDictionary<V>(Func<T, V> valueSelector) {
      return ToDictionary(t => t, valueSelector);
    }
  }
}
