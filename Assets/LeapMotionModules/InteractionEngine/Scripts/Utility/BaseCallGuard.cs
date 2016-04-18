using System;
using System.Diagnostics;

namespace Leap.Unity.Interaction {

  public class BeginNotCalled : Exception {
    public BeginNotCalled(string methodName) :
      base("BaseCallGuard.Begin was not called for " + methodName + ".") { }
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
    private string _pendingMethodKey = null;

    [Conditional("UNITY_ASSERTIONS")]
    public void Begin(string methodKey) {
      _pendingMethodKey = methodKey;
    }

    [Conditional("UNITY_ASSERTIONS")]
    public void AssertBaseCalled() {
      if (_pendingMethodKey != null) {
        var notCalledException = new BaseNotCalledException(_pendingMethodKey);
        _pendingMethodKey = null;
        throw notCalledException;
      }
    }

    [Conditional("UNITY_ASSERTIONS")]
    public void NotifyBaseCalled(string methodKey) {
      if (_pendingMethodKey == null) {
        throw new BeginNotCalled(methodKey);
      }

      if (_pendingMethodKey != methodKey) {
        var wrongBaseException = new WrongBaseCalledException(methodKey, _pendingMethodKey);
        _pendingMethodKey = null;
        throw wrongBaseException;
      }

      _pendingMethodKey = null;
    }
  }
}
