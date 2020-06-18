/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity {

  public class AppGlobals<T> : MonoBehaviour where T : AppGlobals<T> {

    protected void Awake() {
      if (s_instance != null) {
        Debug.LogWarning("Multiple instances of " + typeof(T) + " detected. " +
          "AppGlobals are supposed to be singletons; the instance that awakens the " +
          "latest will be the ultimate receiver of static instance calls.");
      }
      s_instance = this as T;
    }

    private static T s_instance = null;
    public static T instance {
      get {
        if (s_instance == null) {
          s_instance = FindObjectOfType<T>();
        }
        if (s_instance == null) {
          Debug.LogError("No " + typeof(T) + " instance found. App " +
            "instances are loaded lazily from the Scene hierarchy when first " +
            "requested; did you forget to add a " + typeof(T) + " to " +
            "the scene?");
        }
        return s_instance;
      }
    }

  }

}
