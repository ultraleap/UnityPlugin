using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.GraphicalRenderer.Tests {

  public static class ReflectionExtensions {

    public static object GetField<T>(this T t, string fieldName) {
      return t.GetType().GetField(fieldName).GetValue(t);
    }

  }
}
