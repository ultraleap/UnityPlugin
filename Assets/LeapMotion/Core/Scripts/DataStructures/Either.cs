using System;

namespace Leap.Unity {

  /// <summary>
  /// A data structure that represents either a value of type A or
  /// a value of type B.  The value can never be both A and B.
  /// Neither A nor B can ever be null.
  /// </summary>
  public struct Either<A, B> : IEquatable<Either<A, B>>, IComparable, IComparable<Either<A, B>> {

    /// <summary>
    /// Returns whether or not this Either contains the first value.
    /// </summary>
    public readonly bool isA;

    /// <summary>
    /// Returns whether or not this Either contains the second value.
    /// </summary>
    public bool isB {
      get {
        return !isA;
      }
    }

    private readonly A _a;
    private readonly B _b;

    /// <summary>
    /// Returns a Maybe that contains the value of A if it exists,
    /// or no value if it doesn't.
    /// </summary>
    public Maybe<A> a {
      get {
        if (isA) {
          return Maybe<A>.Some(_a);
        } else {
          return Maybe<A>.None;
        }
      }
    }

    /// <summary>
    /// Returns a Maybe that contains the value of B if it exists,
    /// or no value if it doesn't.
    /// </summary>
    public Maybe<B> b {
      get {
        if (isA) {
          return Maybe<B>.None;
        } else {
          return Maybe<B>.Some(_b);
        }
      }
    }

    /// <summary>
    /// Constructs an Either with a value of A.
    /// </summary>
    public Either(A a) {
      if (a == null) {
        throw new ArgumentNullException("Cannot initialize an Either with a null value.");
      }

      isA = true;
      _a = a;
      _b = default(B);
    }

    /// <summary>
    /// Constructs an Either with a value of B.
    /// </summary>
    public Either(B b) {
      if (b == null) {
        throw new ArgumentNullException("Cannot initialize an Either with a null value.");
      }

      isA = false;
      _b = b;
      _a = default(A);
    }

    /// <summary>
    /// Calls the first delegate with the value of A if it is present,
    /// else calls the second delegate with the value of B.
    /// </summary>
    public void Match(Action<A> ifA, Action<B> ifB) {
      if (isA) {
        if (ifA != null) ifA(_a);
      } else {
        if (ifB != null) ifB(_b);
      }
    }

    /// <summary>
    /// If this either contains the value of A, the out argument is filled with
    /// that value and this method returns true, else it returns false.
    /// </summary>
    public bool TryGetA(out A a) {
      a = _a;
      return isA;
    }

    /// <summary>
    /// If this either contains the value of B, the out argument is filled with
    /// that value and this method returns true, else it returns false.
    /// </summary>
    public bool TryGetB(out B b) {
      b = _b;
      return !isA;
    }

    public override int GetHashCode() {
      if (isA) {
        return _a.GetHashCode();
      } else {
        return _b.GetHashCode();
      }
    }

    public override bool Equals(object obj) {
      if (obj is Either<A, B>) {
        return Equals((Either<A, B>)obj);
      } else {
        return false;
      }
    }

    public bool Equals(Either<A, B> other) {
      if (isA != other.isA) {
        return false;
      } else if (isA) {
        return _a.Equals(other._a);
      } else {
        return _b.Equals(other._b);
      }
    }

    public int CompareTo(object obj) {
      if (!(obj is Either<A, B>)) {
        throw new ArgumentException();
      } else {
        return CompareTo((Either<A, B>)obj);
      }
    }

    public int CompareTo(Either<A, B> other) {
      if (isA != other.isA) {
        return isA ? -1 : 1;
      } else if (isA) {
        IComparable<A> ca = _a as IComparable<A>;
        if (ca != null) {
          return ca.CompareTo(other._a);
        } else {
          IComparable c = _a as IComparable;
          if (c != null) {
            return c.CompareTo(other._b);
          } else {
            return 0;
          }
        }
      } else {
        IComparable<B> cb = _b as IComparable<B>;
        if (cb != null) {
          return cb.CompareTo(other._b);
        } else {
          IComparable c = _b as IComparable;
          if (c != null) {
            return c.CompareTo(other._b);
          } else {
            return 0;
          }
        }
      }
    }

    public static bool operator ==(Either<A, B> either0, Either<A, B> either1) {
      return either0.Equals(either1);
    }

    public static bool operator !=(Either<A, B> either0, Either<A, B> either1) {
      return !either0.Equals(either1);
    }

    public static bool operator >(Either<A, B> either0, Either<A, B> either1) {
      return either0.CompareTo(either1) > 0;
    }

    public static bool operator >=(Either<A, B> either0, Either<A, B> either1) {
      return either0.CompareTo(either1) >= 0;
    }

    public static bool operator <(Either<A, B> either0, Either<A, B> either1) {
      return either0.CompareTo(either1) < 0;
    }

    public static bool operator <=(Either<A, B> either0, Either<A, B> either1) {
      return either0.CompareTo(either1) <= 0;
    }

    public static implicit operator Either<A, B>(A a) {
      return new Either<A, B>(a);
    }

    public static implicit operator Either<A, B>(B b) {
      return new Either<A, B>(b);
    }
  }
}
