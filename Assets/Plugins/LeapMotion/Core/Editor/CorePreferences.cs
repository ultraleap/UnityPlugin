/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;
using UnityEditor;

namespace Leap.Unity {

  public static class CorePreferences {

    private const string ALLOW_CLEAR_TRANSFORM_HOTKEY_KEY = 
      "LeapMotion_AllowClearTransformHotkey";
    private const string ALLOW_GROUP_OBJECTS_HOTKEY_KEY = 
      "LeapMotion_AllowGroupObjectsHotkey";
    private const string ALLOW_DESELECT_ALL_HOTKEY_KEY =
      "LeapMotion_AllowDeselectAllHotkey";

    public static bool allowClearTransformHotkey {
      get {
        return EditorPrefs.GetBool(ALLOW_CLEAR_TRANSFORM_HOTKEY_KEY, defaultValue: false);
      }
      set {
        EditorPrefs.SetBool(ALLOW_CLEAR_TRANSFORM_HOTKEY_KEY, value);
      }
    }

    public static bool allowGroupObjectsHotkey {
      get {
        return EditorPrefs.GetBool(ALLOW_GROUP_OBJECTS_HOTKEY_KEY, defaultValue: false);
      }
      set {
        EditorPrefs.SetBool(ALLOW_GROUP_OBJECTS_HOTKEY_KEY, value);
      }
    }

    public static bool allowDeselectAllHotkey {
      get {
        return EditorPrefs.GetBool(ALLOW_DESELECT_ALL_HOTKEY_KEY, defaultValue: false);
      }
      set {
        EditorPrefs.SetBool(ALLOW_DESELECT_ALL_HOTKEY_KEY, value);
      }
    }

    [LeapPreferences("Core", 0)]
    private static void drawCorePreferences() {
      drawPreferencesBool(ALLOW_CLEAR_TRANSFORM_HOTKEY_KEY, "Clear Transforms Hotkey", "When you press Ctrl+E, clear out the local position, rotation, and scale of the selected transforms.");
      drawPreferencesBool(ALLOW_GROUP_OBJECTS_HOTKEY_KEY, "Group Transforms Hotkey", "When you press Ctrl+G, group all selected objects underneath a single new object named Group.");
      drawPreferencesBool(ALLOW_DESELECT_ALL_HOTKEY_KEY, "Deselect All Hotkey", "When you press Ctrl+Shift+D, deselect all objects.");
    }

    private static void drawPreferencesBool(string key, string label, string tooltip) {
      GUIContent content = new GUIContent(label, tooltip);

      bool value = EditorPrefs.GetBool(key, defaultValue: false);
      var newValue = EditorGUILayout.Toggle(content, value);
      if (newValue != value) {
        EditorPrefs.SetBool(key, newValue);
      }
    }
  }
}
