using System;
using System.Diagnostics;

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
    private string _pendingMethodKey = null;
    private bool _wasBeginCalled = false;

    [Conditional("UNITY_ASSERTIONS")]
    public void Begin(string methodKey) {
      _pendingMethodKey = methodKey;
      _wasBeginCalled = true;
    }

    [Conditional("UNITY_ASSERTIONS")]
    public void AssertBaseCalled() {
      if (!_wasBeginCalled) {
        throw new BeginNotCalledException();
      }
      _wasBeginCalled = false;

      if (_pendingMethodKey != null) {
        var notCalledException = new BaseNotCalledException(_pendingMethodKey);
        _pendingMethodKey = null;
        throw notCalledException;
      }
    }

    [Conditional("UNITY_ASSERTIONS")]
    public void NotifyBaseCalled(string methodKey) {
      if (_pendingMethodKey == null) {
        throw new BeginNotCalledException(methodKey);
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
