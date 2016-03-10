using System;
using System.Collections;
using System.Collections.Generic;

namespace InteractionEngine {

  public class OneToOneMap<A, B> : IEnumerable<KeyValuePair<A, B>> {
    private Dictionary<A, B> _aToB = new Dictionary<A, B>();
    private Dictionary<B, A> _bToA = new Dictionary<B, A>();

    public int Count {
      get {
        return _aToB.Count;
      }
    }

    public void Add(A a, B b) {
      if (_aToB.ContainsKey(a)) {
        throw new InvalidOperationException("Cannot add " + getPairString(a, b) + " to map because it conflicts with " +
                                            getPairString(a, _aToB[a]) + ".");
      }
      if (_bToA.ContainsKey(b)) {
        throw new InvalidOperationException("Cannot add " + getPairString(a, b) + " to map because it conflicts with " +
                                            getPairString(_bToA[b], b) + ".");
      }

      _aToB[a] = b;
      _bToA[b] = a;
    }

    public bool Contains(A a) {
      return _aToB.ContainsKey(a);
    }

    public bool Contains(B b) {
      return _bToA.ContainsKey(b);
    }

    public bool Contains(A a, B b) {
      return _aToB.ContainsKey(a) && _bToA.ContainsKey(b);
    }

    public bool Remove(A a) {
      if (!Contains(a)) {
        return false;
      }

      var b = _aToB[a];
      _aToB.Remove(a);
      _bToA.Remove(b);
      return true;
    }

    public bool Remove(B b) {
      if (!Contains(b)) {
        return false;
      }

      var a = _bToA[b];
      _aToB.Remove(a);
      _bToA.Remove(b);
      return true;
    }

    public bool Remove(A a, B b) {
      if (!Contains(a, b)) {
        return false;
      }

      _aToB.Remove(a);
      _bToA.Remove(b);
      return true;
    }

    public A this[B b] {
      get {
        return _bToA[b];
      }
      set {
        _aToB[value] = b;
        _bToA[b] = value;
      }
    }

    public B this[A a] {
      get {
        return _aToB[a];
      }
      set {
        _aToB[a] = value;
        _bToA[value] = a;
      }
    }

    public Dictionary<A, B>.KeyCollection Keys {
      get {
        return _aToB.Keys;
      }
    }

    public Dictionary<B, A>.KeyCollection Values {
      get {
        return _bToA.Keys;
      }
    }

    public IEnumerator<KeyValuePair<A, B>> GetEnumerator() {
      return _aToB.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
      return _aToB.GetEnumerator();
    }

    private string getPairString(A a, B b) {
      return "[" + a + ", " + b + "]";
    }
  }
}
