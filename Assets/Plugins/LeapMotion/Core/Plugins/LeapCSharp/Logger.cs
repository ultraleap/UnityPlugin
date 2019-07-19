/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
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
