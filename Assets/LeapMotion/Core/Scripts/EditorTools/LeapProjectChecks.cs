using Leap.Unity.Query;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity {

  /// <summary>
  /// Add this attribute to a settings check. This method will be called often while the
  /// Leap Motion Unity Window is open, so it should be as light-weight as possible!
  /// If you need to do a heavy check that involves scanning the current scene for
  /// example, you should gate the check behind a button.
  /// 
  /// This project check is called during OnGUI in a Vertical layout context, so you
  /// should draw a box containing any messages, buttons, results, warnings, auto-fixes,
  /// and ignores suitable for the check.
  /// 
  /// For "ignore" functionality, use LeapProjectChecks.CheckIgnoreKey(string) and
  /// LeapProjectsChecks.SetIgnoreKey(string) so that ignore actions can be remembered
  /// and cleared by the project checks GUI.
  /// </summary>
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

    /// <summary>
    /// Draws the GUI for project checks. All detected project checks will be run and
    /// their results shown.
    /// </summary>
    public static void DrawProjectChecksGUI() {
      #if UNITY_EDITOR
      ensureChecksLoaded();
      
      bool allChecksPassed = true;
      foreach (var projectCheck in _projectChecks) {
        
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
          allChecksPassed &= projectCheck.checkFunc();
        }
        
      }


      #endif
    }

    #region Ignored Keys via Editor Prefs

    private const string IGNORED_KEYS_PREF = "LeapUnityWindow_IgnoredKeys";

    private static HashSet<string> _backingIgnoredKeys = null;
    /// <summary> Lazily filled via EditorPrefs. </summary>
    private static HashSet<string> _ignoredKeys {
      get {
        if (_backingIgnoredKeys == null) {
          _backingIgnoredKeys
            = splitBySemicolonToSet(EditorPrefs.GetString(IGNORED_KEYS_PREF));
        }
        return _backingIgnoredKeys;
      }
    }

    public static bool CheckIgnoredKey(string editorPrefKey) {
      return _ignoredKeys.Contains(editorPrefKey);
    }

    public static void SetIgnoredKey(string editorPrefKey, bool ignore) {
      if (ignore) {
        _ignoredKeys.Add(editorPrefKey);
      }
      else {
        _ignoredKeys.Remove(editorPrefKey);
      }

      uploadignoredKeyChangesToEditorPrefs();
    }

    public static void ClearAllIgnoredKeys() {
      _ignoredKeys.Clear();

      uploadignoredKeyChangesToEditorPrefs();
    }

    /// <summary>
    /// Breaks out the semicolon-delimited string into a HashSet of strings.
    /// Whitespace is preserved. Empty entries are removed.
    /// </summary>
    private static HashSet<string> splitBySemicolonToSet(string ignoredKeys_semicolonDelimited) {
      var keys = ignoredKeys_semicolonDelimited;
      var set = new HashSet<string>();
      foreach (var key in keys.Split(new char[] { ';' },
                                    StringSplitOptions.RemoveEmptyEntries)) {
        set.Add(key);
      }
      return set;
    }

    private static string joinBySemicolon(HashSet<string> keys) {
      return string.Join(";", keys.Query().ToArray());
    }

    private static void uploadignoredKeyChangesToEditorPrefs() {
      EditorPrefs.SetString(IGNORED_KEYS_PREF, joinBySemicolon(_ignoredKeys));
    }

    #endregion

  }

}
