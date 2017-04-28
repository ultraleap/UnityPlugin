/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

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
    private Stack<string> _pendingCalls = new Stack<string>();

    [Conditional("UNITY_ASSERTIONS")]
    public void Begin(string methodKey) {
      _pendingKeys.Push(methodKey);
    }

    [Conditional("UNITY_ASSERTIONS")]
    public void AssertBaseCalled() {
      if (_pendingKeys.Count == 0) {
        throw new BeginNotCalledException();
      }

      if (_pendingKeys.Count > _pendingCalls.Count) {
        throw new BaseNotCalledException(_pendingKeys.Peek());
      }

      _pendingKeys.Pop();
      _pendingCalls.Pop();
    }

    [Conditional("UNITY_ASSERTIONS")]
    public void NotifyBaseCalled(string methodKey) {
      if (_pendingKeys.Count == 0) {
        throw new BeginNotCalledException(methodKey);
      }

      _pendingCalls.Push(methodKey);

      var pendingKey = _pendingKeys.Peek();
      if (pendingKey != methodKey) {
        var wrongBaseException = new WrongBaseCalledException(methodKey, pendingKey);
        throw wrongBaseException;
      }
    }
  }
}
