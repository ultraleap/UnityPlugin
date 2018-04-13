/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using System.Linq;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity.Testing {

  [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
  public class SetupLeapTestsAttribute : Attribute { }

  public static class EnableLeapTests {

#if UNITY_EDITOR
    [MenuItem("Assets/Enable Leap Tests")]
    public static void enableTests() {
      var assemblies = AppDomain.CurrentDomain.GetAssemblies();
      foreach (var type in assemblies.SelectMany(a => a.GetTypes())) {
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)) {
          var attributes = method.GetCustomAttributes(typeof(SetupLeapTestsAttribute), inherit: true);
          if (attributes.Length == 0) {
            continue;
          }

          method.Invoke(null, null);
        }
      }

      string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
      PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, defines + " LEAP_TESTS");
    }
#endif
  }
}
