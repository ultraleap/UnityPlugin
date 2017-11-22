using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity {

  public static class Type<T> {
    public static readonly bool isValueType;

    static Type() {
      isValueType = typeof(T).IsValueType;
    }
  }

}