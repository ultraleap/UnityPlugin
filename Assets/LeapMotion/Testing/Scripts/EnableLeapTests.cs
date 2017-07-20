using System;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace Leap.Unity.Testing {

  [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
  public class SetupLeapTestsAttribute : Attribute { }

  public static class EnableLeapTests {

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
  }
}
