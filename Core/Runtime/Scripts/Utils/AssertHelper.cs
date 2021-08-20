/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

public static class AssertHelper {

  [Conditional("UNITY_EDITOR")]
  public static void AssertRuntimeOnly(string message = null) {
    message = message ?? "Assert failed because game was not in Play Mode.";
    Assert.IsTrue(Application.isPlaying, message);
  }

  [Conditional("UNITY_EDITOR")]
  public static void AssertEditorOnly(string message = null) {
    message = message ?? "Assert failed because game was in Play Mode.";
    Assert.IsFalse(Application.isPlaying, message);
  }

  [Conditional("UNITY_ASSERTIONS")]
  public static void Implies(bool condition, bool result, string message = "") {
    if (condition) {
      Assert.IsTrue(result, message);
    }
  }

  [Conditional("UNITY_ASSERTIONS")]
  public static void Implies(bool condition, Func<bool> result, string message = "") {
    if (condition) {
      Implies(condition, result(), message);
    }
  }

  [Conditional("UNITY_ASSERTIONS")]
  public static void Implies(string conditionName, bool condition, string resultName, bool result) {
    Implies(condition, result, "When " + conditionName + " is true, " + resultName + " must always be true.");
  }

  [Conditional("UNITY_ASSERTIONS")]
  public static void Implies(string conditionName, bool condition, string resultName, Func<bool> result) {
    if (condition) {
      Implies(conditionName, condition, resultName, result());
    }
  }

  [Conditional("UNITY_ASSERTIONS")]
  public static void Contains<T>(T value, IEnumerable<T> collection, string message = "") {
    if (!collection.Contains(value)) {
      string result = "The value " + value + " was not found in the collection [";

      bool isFirst = true;
      foreach (T v in collection) {
        if (!isFirst) {
          result += ", ";
          isFirst = false;
        }

        result += v.ToString();
      }

      result += "]\n" + message;
      Assert.IsTrue(false, result);
    }
  }
}
