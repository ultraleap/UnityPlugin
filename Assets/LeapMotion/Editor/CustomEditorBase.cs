using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace Leap.Unity {

  public class CustomEditorBase : Editor {
    protected Dictionary<string, Action<SerializedProperty>> _specifiedDrawers;
    protected Dictionary<string, List<Action<SerializedProperty>>> _specifiedDecorators;
    protected Dictionary<string, string> _conditionalProperties;

    /// <summary>
    /// Specify a callback to be used to draw a specific named property.  Should be called in OnEnable.
    /// </summary>
    /// <param name="propertyName"></param>
    /// <param name="propertyDrawer"></param>
    protected void specifyCustomDrawer(string propertyName, Action<SerializedProperty> propertyDrawer) {
      if (serializedObject.FindProperty(propertyName) != null) {
        _specifiedDrawers[propertyName] = propertyDrawer;
      } else {
        Debug.LogWarning("Specified a custom drawer for the nonexistant property [" + propertyName + "] !\nWas it renamed or deleted?");
      }
    }

    /// <summary>
    /// Specify a callback to be used to draw a decorator for a specific named property.  Should be called in OnEnable.
    /// </summary>
    /// <param name="propertyName"></param>
    /// <param name="decoratorDrawer"></param>
    protected void specifyCustomDecorator(string propertyName, Action<SerializedProperty> decoratorDrawer) {
      if (serializedObject.FindProperty(propertyName) != null) {

        List<Action<SerializedProperty>> list;
        if (!_specifiedDecorators.TryGetValue(propertyName, out list)) {
          list = new List<Action<SerializedProperty>>();
          _specifiedDecorators[propertyName] = list;
        }

        list.Add(decoratorDrawer);
      } else {
        Debug.LogWarning("Specified a custom drawer for the nonexistant property [" + propertyName + "] !\nWas it renamed or deleted?");
      }
    }

    /// <summary>
    /// Specify a list of properties that should only be displayed if the conditional property has a value of true.
    /// Should be called in OnEnable.
    /// </summary>
    /// <param name="conditionalName"></param>
    /// <param name="dependantProperties"></param>
    protected void specifyConditionalDrawing(string conditionalName, params string[] dependantProperties) {
      for (int i = 0; i < dependantProperties.Length; i++) {
        _conditionalProperties[dependantProperties[i]] = conditionalName;
      }
    }

    protected virtual void OnEnable() {
      _specifiedDrawers = new Dictionary<string, Action<SerializedProperty>>();
      _specifiedDecorators = new Dictionary<string, List<Action<SerializedProperty>>>();
      _conditionalProperties = new Dictionary<string, string>();
    }

    /* 
     * This method draws all visible properties, mirroring the default behavior of OnInspectorGUI. 
     * Individual properties can be specified to have custom drawers.
     */
    public override void OnInspectorGUI() {
      SerializedProperty iterator = serializedObject.GetIterator();
      bool isFirst = true;

      while (iterator.NextVisible(isFirst)) {
        string conditionalPropertyName;
        if (_conditionalProperties.TryGetValue(iterator.name, out conditionalPropertyName)) {
          SerializedProperty conditionalProperty = serializedObject.FindProperty(conditionalPropertyName);
          if (!conditionalProperty.boolValue) {
            continue;
          }
        }

        Action<SerializedProperty> customDrawer;

        List<Action<SerializedProperty>> decoratorList;
        if (_specifiedDecorators.TryGetValue(iterator.name, out decoratorList)) {
          for (int i = 0; i < decoratorList.Count; i++) {
            decoratorList[i](iterator);
          }
        }

        if (_specifiedDrawers.TryGetValue(iterator.name, out customDrawer)) {
          customDrawer(iterator);
        } else {
          using (new EditorGUI.DisabledGroupScope(isFirst)) {
            EditorGUILayout.PropertyField(iterator, true);
          }
        }

        isFirst = false;
      }

      serializedObject.ApplyModifiedProperties();
    }
  }
}
