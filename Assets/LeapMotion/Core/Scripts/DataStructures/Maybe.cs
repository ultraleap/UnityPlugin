using System;

namespace Leap.Unity {

  public struct Maybe<T> : IEquatable<Maybe<T>>, IComparable, IComparable<Maybe<T>> {
    public static readonly Maybe<T> None = new Maybe<T>();

    public readonly bool hasValue;

    private readonly T _t;

    public Maybe(T t) {
      hasValue = t != null;
      _t = t;
    }

    public Maybe(ref T t) {
      hasValue = t != null;
      _t = t;
    }

    public static Maybe<T> Some(T t) {
      return new Maybe<T>(ref t);
    }

    public static Maybe<T> Some(ref T t) {
      return new Maybe<T>(ref t);
    }

    public bool TryGetValue(out T t) {
      t = _t;
      return hasValue;
    }

    public void Match(Action<T> ifValue) {
      if (hasValue) {
        ifValue(_t);
      }
    }

    public void Match(Action<T> ifValue, Action ifNot) {
      if (hasValue) {
        ifValue(_t);
      } else {
        ifNot();
      }
    }

    public override int GetHashCode() {
      return hasValue ? _t.GetHashCode() : 0;
    }

    public override bool Equals(object obj) {
      if (obj is Maybe<T>) {
        return Equals((Maybe<T>)obj);
      } else {
        return false;
      }
    }

    public bool Equals(Maybe<T> other) {
      if (hasValue != other.hasValue) {
        return false;
      } else if (hasValue) {
        return _t.Equals(other._t);
      } else {
        return true;
      }
    }

    public int CompareTo(object obj) {
      if (!(obj is Maybe<T>)) {
        throw new ArgumentException();
      } else {
        return CompareTo((Maybe<T>)obj);
      }
    }

    public int CompareTo(Maybe<T> other) {
      if (hasValue != other.hasValue) {
        return hasValue ? 1 : -1;
      } else if (hasValue) {
        IComparable<T> ct = _t as IComparable<T>;
        if (ct != null) {
          return ct.CompareTo(other._t);
        } else {
          IComparable c = _t as IComparable;
          if (c != null) {
            return c.CompareTo(other._t);
          } else {
            return 0;
          }
        }
      } else {
        return 0;
      }
    }

    public static bool operator ==(Maybe<T> maybe0, Maybe<T> maybe1) {
      return maybe0.Equals(maybe1);
    }

    public static bool operator !=(Maybe<T> maybe0, Maybe<T> maybe1) {
      return !maybe0.Equals(maybe1);
    }

    public static bool operator >(Maybe<T> maybe0, Maybe<T> maybe1) {
      return maybe0.CompareTo(maybe1) > 0;
    }

    public static bool operator >=(Maybe<T> maybe0, Maybe<T> maybe1) {
      return maybe0.CompareTo(maybe1) >= 0;
    }

    public static bool operator <(Maybe<T> maybe0, Maybe<T> maybe1) {
      return maybe0.CompareTo(maybe1) < 0;
    }

    public static bool operator <=(Maybe<T> maybe0, Maybe<T> maybe1) {
      return maybe0.CompareTo(maybe1) <= 0;
    }

    public static implicit operator Maybe<T>(T t) {
      return new Maybe<T>(ref t);
    }
  }
}
