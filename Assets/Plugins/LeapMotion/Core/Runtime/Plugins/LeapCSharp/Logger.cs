/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using System.Reflection;

namespace LeapInternal {
  public static class Logger {

    /// <summary>
    /// Logs message to the a Console.
    /// </summary>
    public static void Log(object message) {
      UnityEngine.Debug.Log(message);
    }

    public static void LogStruct(object thisObject, string title = "") {
      try {
        if (!thisObject.GetType().IsValueType) {
          Log(title + " ---- Trying to log non-struct with struct logger");
          return;
        }
        Log(title + " ---- " + thisObject.GetType().ToString());
        FieldInfo[] fieldInfos;
        fieldInfos = thisObject.GetType().GetFields(
            BindingFlags.Public | BindingFlags.NonPublic // Get public and non-public
            | BindingFlags.Static | BindingFlags.Instance  // Get instance + static
            | BindingFlags.FlattenHierarchy); // Search up the hierarchy

        // write member names
        foreach (FieldInfo fieldInfo in fieldInfos) {
          object obj = fieldInfo.GetValue(thisObject);
          string value = obj == null ? "null" : obj.ToString();
          Log(" -------- Name: " + fieldInfo.Name + ", Value = " + value);
        }
      } catch (Exception exception) {
        Log(exception.Message);
      }
    }
  }
}
