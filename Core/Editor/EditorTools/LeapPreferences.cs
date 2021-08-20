/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity {

  /// <summary>
  /// This attribute is used to add items to the Leap Motion preferences window.
  /// This allows each module to define their own preferences and still have them
  /// all show up under the same window.  
  /// 
  /// The usage is very similar to the built-in PreferenceItem attribute.  You
  /// add the attribute onto a static method that should be run whenever the 
  /// preference window is visited.  This method is a gui method and should use
  /// GuiLayout and EditorGuiLayout in order to draw the preferences.  You can
  /// specify the name of the preferences as well as an order value to specify
  /// how the preferences are ordered relative to other preferences.
  /// </summary>
  [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
  public class LeapPreferences : Attribute {
    public readonly string header;
    public readonly int order;

    public LeapPreferences(string header, int order) {
      this.header = header;
      this.order = order;
    }

#if UNITY_EDITOR
    private static List<LeapPreferenceItem> _leapPreferenceItems = null;

    private struct LeapPreferenceItem {
      public Action drawPreferenceGui;
      public LeapPreferences attribute;
    }

    private static void ensurePreferenceItemsLoaded() {
      if (_leapPreferenceItems != null) {
        return;
      }

      _leapPreferenceItems = new List<LeapPreferenceItem>();

      var assemblies = AppDomain.CurrentDomain.GetAssemblies();
      foreach (var type in assemblies.SelectMany(a => a.GetTypes())) {
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)) {
          var attributes = method.GetCustomAttributes(typeof(LeapPreferences), inherit: true);
          if (attributes.Length == 0) {
            continue;
          }

          var attribute = attributes[0] as LeapPreferences;
          _leapPreferenceItems.Add(new LeapPreferenceItem() {
            drawPreferenceGui = () => {
              EditorGUILayout.LabelField(attribute.header, EditorStyles.boldLabel);
              using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
                method.Invoke(null, null);
              }
              EditorGUILayout.Space();
              EditorGUILayout.Space();
              EditorGUILayout.Space();
            },
            attribute = attribute
          });
        }
      }

      _leapPreferenceItems.Sort((a, b) => a.attribute.order.CompareTo(b.attribute.order));
    }

  #if UNITY_2018_3_OR_NEWER
    // Implementations Leap Motion settings using the new SettingsProvider API.
    private class LeapMotionSettingsProvider : SettingsProvider {
      public LeapMotionSettingsProvider(string path, SettingsScope scopes = SettingsScope.User)
      : base(path, scopes)
      { }

      public override void OnGUI(string searchContext) {
        DrawPreferencesGUI();
      }
    }
 
    [SettingsProvider]
    static SettingsProvider GetSettingsProvider()
    {
        return new LeapMotionSettingsProvider("Preferences/Leap Motion");
    }
#else
    [PreferenceItem("Leap Motion")]
#endif
    public static void DrawPreferencesGUI() {
      ensurePreferenceItemsLoaded();

      foreach (var item in _leapPreferenceItems) {
        item.drawPreferenceGui();
      }
    }
    #endif
  }
}
