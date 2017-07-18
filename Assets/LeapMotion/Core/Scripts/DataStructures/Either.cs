using System;

namespace Leap.Unity {

  public struct Either<A, B> : IEquatable<Either<A, B>>, IComparable, IComparable<Either<A, B>> {
    private bool _isA;
    private readonly A _a;
    private readonly B _b;

    public Maybe<A> a {
      get {
        if (_isA) {
          return Maybe<A>.Some(_a);
        } else {
          return Maybe<A>.None;
        }
      }
    }

    public Maybe<B> b {
      get {
        if (_isA) {
          return Maybe<B>.None;
        } else {
          return Maybe<B>.Some(_b);
        }
      }
    }

    public Either(A a) {
      _isA = true;
      _a = a;
      _b = default(B);
    }

    public Either(ref A a) {
      _isA = true;
      _a = a;
      _b = default(B);
    }

    public Either(B b) {
      _isA = false;
      _b = b;
      _a = default(A);
    }

    public Either(ref B b) {
      _isA = false;
      _b = b;
      _a = default(A);
    }

    public void IfA(Action<A> ifA) {
      if (_isA) {
        ifA(_a);
      }
    }

    public void IfA(Action ifA) {
      if (_isA) {
        ifA();
      }
    }

    public void IfB(Action<B> ifB) {
      if (!_isA) {
        ifB(_b);
      }
    }

    public void IfB(Action ifB) {
      if (!_isA) {
        ifB();
      }
    }

    public void Match(Action<A> ifA, Action<B> ifB) {
      if (_isA) {
        ifA(_a);
      } else {
        ifB(_b);
      }
    }

    public void Match(Action ifA, Action<B> ifB) {
      if (_isA) {
        ifA();
      } else {
        ifB(_b);
      }
    }

    public void Match(Action<A> ifA, Action ifB) {
      if (_isA) {
        ifA(_a);
      } else {
        ifB();
      }
    }

    public bool TryGetA(out A a) {
      a = _a;
      return _isA;
    }

    public bool TryGetB(out B b) {
      b = _b;
      return !_isA;
    }

    public override int GetHashCode() {
      if (_isA) {
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
      if (_isA != other._isA) {
        return false;
      } else if (_isA) {
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
      if (_isA != other._isA) {
        return _isA ? -1 : 1;
      } else if (_isA) {
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
      return new Either<A, B>(ref a);
    }

    public static implicit operator Either<A, B>(B b) {
      return new Either<A, B>(ref b);
    }
  }
}
