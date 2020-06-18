/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

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

  /// <summary>
  /// Utility class for working with project checks. Note, most features are only
  /// available in the Editor.
  /// </summary>
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

      if (_ignoredKeys != null && _ignoredKeys.Count > 0) {
        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope()) {
          GUILayout.FlexibleSpace();

          using (new EditorGUILayout.VerticalScope()) {
            GUILayout.Space(4f);
            GUILayout.Label("Some project checks have been ignored.");
          }

          if (GUILayout.Button(new GUIContent("Reset Ignore Flags",
                "Un-ignore any project checks that have been ignored."))) {
            ClearAllIgnoredKeys();
          }

          EditorGUILayout.Space();
        }
      }
      #endif
    }

    #region Ignored Keys via Editor Prefs

    private const string IGNORED_KEYS_PREF = "LeapUnityWindow_IgnoredKeys";

    #if UNITY_EDITOR
    private static HashSet<string> _backingIgnoredKeys = null;
    #endif
    /// <summary> Lazily filled via EditorPrefs. </summary>
    private static HashSet<string> _ignoredKeys {
      get {
        #if UNITY_EDITOR
        if (_backingIgnoredKeys == null) {
          _backingIgnoredKeys
            = splitBySemicolonToSet(EditorPrefs.GetString(IGNORED_KEYS_PREF));
        }
        return _backingIgnoredKeys;
        #else
        return null;
        #endif
      }
    }

    public static bool CheckIgnoredKey(string editorPrefKey) {
      #if UNITY_EDITOR
      return _ignoredKeys.Contains(editorPrefKey);
      #else
      return false;
      #endif
    }

    public static void SetIgnoredKey(string editorPrefKey, bool ignore) {
      #if UNITY_EDITOR
      if (ignore) {
        _ignoredKeys.Add(editorPrefKey);
      }
      else {
        _ignoredKeys.Remove(editorPrefKey);
      }

      uploadignoredKeyChangesToEditorPrefs();
      #endif
    }

    public static void ClearAllIgnoredKeys() {
      #if UNITY_EDITOR
      _ignoredKeys.Clear();

      uploadignoredKeyChangesToEditorPrefs();
      #endif
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
      #if UNITY_EDITOR
      EditorPrefs.SetString(IGNORED_KEYS_PREF, joinBySemicolon(_ignoredKeys));
      #endif
    }

#endregion

  }

}
