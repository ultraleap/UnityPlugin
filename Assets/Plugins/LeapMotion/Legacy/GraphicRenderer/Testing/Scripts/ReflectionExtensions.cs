/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

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
