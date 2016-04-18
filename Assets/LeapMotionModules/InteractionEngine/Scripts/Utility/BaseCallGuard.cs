using System;
using System.Diagnostics;

namespace Leap.Unity.Interaction {

  public class BaseNotCalledException : Exception {
    public BaseNotCalledException() :
      base("Base implementation was not called.") { }
  }

  public class BaseCallGuard {
    private bool _waitingForBaseCall = false;

    [Conditional("UNITY_ASSERTIONS")]
    public void Begin() {
      _waitingForBaseCall = true;
    }

    [Conditional("UNITY_ASSERTIONS")]
    public void AssertBaseCalled() {
      if (_waitingForBaseCall) {
        var notCalledException = new BaseNotCalledException();
        _waitingForBaseCall = false;
        throw notCalledException;
      }
    }

    [Conditional("UNITY_ASSERTIONS")]
    public void NotifyBaseCalled() {
      _waitingForBaseCall = false;
    }
  }
}
