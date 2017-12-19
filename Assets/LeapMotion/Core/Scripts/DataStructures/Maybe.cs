/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;

namespace Leap.Unity {
  using Query;

  public static class Maybe {
    public static readonly NoneType None = new NoneType();

    public static Maybe<T> Some<T>(T value) {
      return new Maybe<T>(value);
    }

    public static void MatchAll<A, B>(Maybe<A> maybeA, Maybe<B> maybeB, Action<A, B> action) {
      maybeA.Match(a => {
        maybeB.Match(b => {
          action(a, b);
        });
      });
    }

    public static void MatchAll<A, B, C>(Maybe<A> maybeA, Maybe<B> maybeB, Maybe<C> maybeC, Action<A, B, C> action) {
      maybeA.Match(a => {
        maybeB.Match(b => {
          maybeC.Match(c => {
            action(a, b, c);
          });
        });
      });
    }

    public static void MatchAll<A, B, C, D>(Maybe<A> maybeA, Maybe<B> maybeB, Maybe<C> maybeC, Maybe<D> maybeD, Action<A, B, C, D> action) {
      maybeA.Match(a => {
        maybeB.Match(b => {
          maybeC.Match(c => {
            maybeD.Match(d => {
              action(a, b, c, d);
            });
          });
        });
      });
    }

    public struct NoneType { }
  }

  /// <summary>
  /// A struct that represents a value that could or could not exist.  Unlike
  /// the built-int nullable types, you are unable to access the value unless
  /// it does exist, and will never recieve a null value.
  /// </summary>
  public struct Maybe<T> : IEquatable<Maybe<T>>, IComparable, IComparable<Maybe<T>> {

    /// <summary>
    /// Returns a Maybe for this type that represents no value.
    /// </summary>
    public readonly static Maybe<T> None = new Maybe<T>();

    /// <summary>
    /// Returns whether or not this Maybe contains a value.
    /// </summary>
    public readonly bool hasValue;

    /// <summary>
    /// Gets the value, or the type's default if it doesn't exist.
    /// </summary>
    public T valueOrDefault {
      get {
        T value;
        if (TryGetValue(out value)) {
          return value;
        }
        return default(T);
      }
    }

    private readonly T _t;

    /// <summary>
    /// Constructs a Maybe given a value. If the value is not null, this maybe will have
    /// a value. If the value is null, this maybe will have no value. For value types,
    /// the Maybe struct will always have a value. (Use Maybe.None to refer to "no value.")
    /// </summary>
    public Maybe(T t) {
      if (Type<T>.isValueType) {
        hasValue = true;
      }
      else {
        hasValue = t != null;
      }

      _t = t;
    }

    /// <summary>
    /// Constructs a Maybe given a specific value. This value needs to always be
    /// non-null if the type is a reference type.
    /// </summary>
    public static Maybe<T> Some(T t) {
      if (!Type<T>.isValueType && t == null) {
        throw new ArgumentNullException("Cannot use Some with a null argument.");
      }

      return new Maybe<T>(t);
    }

    /// <summary>
    /// If this Maybe has a value, the out argument is filled with that value and
    /// this method returns true, else it returns false.
    /// </summary>
    public bool TryGetValue(out T t) {
      t = _t;
      return hasValue;
    }

    /// <summary>
    /// If this Maybe has a value, the delegate is called with that value.
    /// </summary>
    public void Match(Action<T> ifValue) {
      if (hasValue) {
        ifValue(_t);
      }
    }

    /// <summary>
    /// If this Maybe has a value, the first delegate is called with that value,
    /// else the second delegate is called.
    /// </summary>
    public void Match(Action<T> ifValue, Action ifNot) {
      if (hasValue) {
        if (ifValue != null) ifValue(_t);
      } else {
        ifNot();
      }
    }

    /// <summary>
    /// If this Maybe has a value, the first delegate is called with that value,
    /// else the second delegate is called.
    /// </summary>
    public K Match<K>(Func<T, K> ifValue, Func<K> ifNot) {
      if (hasValue) {
        if (ifValue != null) {
          return ifValue(_t);
        } else {
          return default(K);
        }
      } else {
        return ifNot();
      }
    }

    public QueryWrapper<T, Maybe<T>.MaybeOp> Query() {
      return new QueryWrapper<T, MaybeOp>(new MaybeOp(this));
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
      return new Maybe<T>(t);
    }

    public static implicit operator Maybe<T>(Maybe.NoneType none) {
      return Maybe<T>.None;
    }

    public struct MaybeOp : IQueryOp<T> {
      public Maybe<T> _value;
      public bool _hasReturned;

      public MaybeOp(Maybe<T> value) {
        _value = value;
        _hasReturned = false;
      }

      public bool TryGetNext(out T t) {
        if (_hasReturned) {
          t = default(T);
          return false;
        } else {
          if (_value.hasValue) {
            t = _value._t;
            _hasReturned = true;
            return true;
          } else {
            t = default(T);
            _hasReturned = true;
            return false;
          }
        }
      }

      public void Reset() {
        _hasReturned = false;
      }
    }
  }
}
