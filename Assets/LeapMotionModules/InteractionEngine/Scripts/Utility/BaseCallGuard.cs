using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace Leap.Unity.Interaction {

  public class BeginNotCalledException : Exception {
    public BeginNotCalledException(string methodName) :
      base("Begin was not called for " + methodName + ".") { }

    public BeginNotCalledException() :
      base("Begin was not called before calling AssertBaseCalled.") { }
  }

  public class BaseNotCalledException : Exception {
    public BaseNotCalledException(string methodName) :
      base("Base implementation of " + methodName + " was not called.") { }
  }

  public class WrongBaseCalledException : Exception {
    public WrongBaseCalledException(string calledMethod, string correctMethod) :
      base("Base implementation of " + correctMethod + " was not called, " + calledMethod + " was called instead.") { }
  }

  public class BaseCallGuard {
    private Stack<string> _pendingKeys = new Stack<string>();
    private string _justPopped = null;

    [Conditional("UNITY_ASSERTIONS")]
    public void Begin(string methodKey) {
      _pendingKeys.Push(methodKey);
      _justPopped = null;
    }

    [Conditional("UNITY_ASSERTIONS")]
    public void AssertBaseCalled() {
      if (_justPopped == null) {
        new BaseNotCalledException(_pendingKeys.Pop());
      }
    }

    [Conditional("UNITY_ASSERTIONS")]
    public void NotifyBaseCalled(string methodKey) {
      if (_pendingKeys.Count == 0) {
        throw new BeginNotCalledException(methodKey);
      }

      _justPopped = _pendingKeys.Pop();
      if (_justPopped != methodKey) {
        var wrongBaseException = new WrongBaseCalledException(methodKey, _justPopped);
        throw wrongBaseException;
      }
    }
  }
}
