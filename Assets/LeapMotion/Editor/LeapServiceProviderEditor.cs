using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Leap.Unity {

  [CustomEditor(typeof(LeapServiceProvider))]
  public class LeapServiceProviderEditor : Editor {
    public static string _interpolationToggleProperty = "_useInterpolation";
    public static List<string> _interpolationProperties = new List<string> { "_interpolationDelay" };

    public static string _overrideDeviceToggleProperty = "_overrideDeviceType";
    public static List<string> _deviceTypeProperties = new List<string> { "_overrideDeviceTypeWith" };

    public override void OnInspectorGUI() {
      serializedObject.Update();
      SerializedProperty properties = serializedObject.GetIterator();

      bool useInterpolation = serializedObject.FindProperty(_interpolationToggleProperty).boolValue;
      bool overrideDeviceType = serializedObject.FindProperty(_overrideDeviceToggleProperty).boolValue;

      bool useEnterChildren = true;
      while (properties.NextVisible(useEnterChildren)) {
        useEnterChildren = false;

        if (_interpolationProperties.Contains(properties.name) && !useInterpolation) {
          continue;
        }

        if (_deviceTypeProperties.Contains(properties.name) && !overrideDeviceType) {
          continue;
        }

        EditorGUILayout.PropertyField(properties, true);
      }

      serializedObject.ApplyModifiedProperties();
    }
  }
}
