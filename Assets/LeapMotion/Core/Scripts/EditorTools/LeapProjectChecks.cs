using Leap.Unity.Query;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Leap.Unity {

  [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
  public class LeapProjectCheckAttribute : Attribute {
    public string header;
    public int order;

    public LeapProjectCheckAttribute(string header, int order) {
      this.header = header;
      this.order = order;
    }
  }

  public static class LeapProjectChecks {

    private struct ProjectCheck {
      public Func<bool> checkFunc;
      public LeapProjectCheckAttribute attribute;
    }

    private static List<ProjectCheck> _projectChecks = null;

    private static void ensureChecksLoaded() {
      if (_projectChecks != null) {
        return;
      }

      _projectChecks = new List<ProjectCheck>();

      var assemblies = AppDomain.CurrentDomain.GetAssemblies();
      foreach (var type in assemblies.Query().SelectMany(a => a.GetTypes())) {
        foreach (var method in type.GetMethods(BindingFlags.Public
                                               | BindingFlags.NonPublic
                                               | BindingFlags.Static)) {
          var attributes = method.GetCustomAttributes(typeof(LeapProjectCheckAttribute),
                                                      inherit: true);
          if (attributes.Length == 0) {
            continue;
          }

          var attribute = attributes[0] as LeapProjectCheckAttribute;
          _projectChecks.Add(new ProjectCheck() {
            checkFunc = () => {
              if (!method.IsStatic) {
                Debug.LogError("Invalid project check definition; project checks must "
                             + "be static methods.");
                return true;
              }
              else if (method.ReturnType == typeof(bool)) {
                return (bool)method.Invoke(null, null);
              }
              else {
                return true;
              }
            },
            attribute = attribute
          });
        }
      }

      _projectChecks.Sort((a, b) => a.attribute.order.CompareTo(b.attribute.order));
    }

    public static void DrawProjectChecksGUI() {
      ensureChecksLoaded();

      // TODO: Draw GUI.
      bool allChecksPassed = true;
      foreach (var projectCheck in _projectChecks) {
        allChecksPassed &= projectCheck.checkFunc();
      }
    }

  }

}
