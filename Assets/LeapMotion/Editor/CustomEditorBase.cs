using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace Leap.Unity {

  public class CustomEditorBase : Editor {
    protected Dictionary<string, Action<SerializedProperty>> _specifiedDrawers;
    protected Dictionary<string, List<Action<SerializedProperty>>> _specifiedDecorators;
    protected Dictionary<string, List<Func<bool>>> _conditionalProperties;

    protected List<SerializedProperty> _modifiedProperties = new List<SerializedProperty>();

    /// <summary>
    /// Specify a callback to be used to draw a specific named property.  Should be called in OnEnable.
    /// </summary>
    /// <param name="propertyName"></param>
    /// <param name="propertyDrawer"></param>
    protected void specifyCustomDrawer(string propertyName, Action<SerializedProperty> propertyDrawer) {
      if (!validateProperty(propertyName)) {
        return;
      }

      _specifiedDrawers[propertyName] = propertyDrawer;
    }

    /// <summary>
    /// Specify a callback to be used to draw a decorator for a specific named property.  Should be called in OnEnable.
    /// </summary>
    /// <param name="propertyName"></param>
    /// <param name="decoratorDrawer"></param>
    protected void specifyCustomDecorator(string propertyName, Action<SerializedProperty> decoratorDrawer) {
      if (!validateProperty(propertyName)) {
        return;
      }

      List<Action<SerializedProperty>> list;
      if (!_specifiedDecorators.TryGetValue(propertyName, out list)) {
        list = new List<Action<SerializedProperty>>();
        _specifiedDecorators[propertyName] = list;
      }

      list.Add(decoratorDrawer);
    }

    /// <summary>
    /// Specify a list of properties that should only be displayed if the conditional property has a value of true.
    /// Should be called in OnEnable.
    /// </summary>
    /// <param name="conditionalName"></param>
    /// <param name="dependantProperties"></param>
    protected void specifyConditionalDrawing(string conditionalName, params string[] dependantProperties) {
      if (!validateProperty(conditionalName)) {
        return;
      }

      SerializedProperty conditionalProp = serializedObject.FindProperty(conditionalName);
      specifyConditionalDrawing(() => conditionalProp.boolValue, dependantProperties);
    }

    protected void specifyConditionalDrawing(Func<bool> conditional, params string[] dependantProperties) {
      for (int i = 0; i < dependantProperties.Length; i++) {
        string dependant = dependantProperties[i];

        if (!validateProperty(dependant)) {
          continue;
        }

        List<Func<bool>> list;
        if (!_conditionalProperties.TryGetValue(dependant, out list)) {
          list = new List<Func<bool>>();
          _conditionalProperties[dependant] = list;
        }
        list.Add(conditional);
      }
    }

    protected virtual void OnEnable() {
      _specifiedDrawers = new Dictionary<string, Action<SerializedProperty>>();
      _specifiedDecorators = new Dictionary<string, List<Action<SerializedProperty>>>();
      _conditionalProperties = new Dictionary<string, List<Func<bool>>>();
    }

    protected bool validateProperty(string propertyName) {
      if (serializedObject.FindProperty(propertyName) == null) {
        Debug.LogWarning("Property " + propertyName + " does not exist, was it removed or renamed?");
        return false;
      }
      return true;
    }

    /* 
     * This method draws all visible properties, mirroring the default behavior of OnInspectorGUI. 
     * Individual properties can be specified to have custom drawers.
     */
    public override void OnInspectorGUI() {
      _modifiedProperties.Clear();
      SerializedProperty iterator = serializedObject.GetIterator();
      bool isFirst = true;

      while (iterator.NextVisible(isFirst)) {
        List<Func<bool>> conditionalList;
        if (_conditionalProperties.TryGetValue(iterator.name, out conditionalList)) {
          bool allTrue = true;
          for (int i = 0; i < conditionalList.Count; i++) {
            allTrue &= conditionalList[i]();
          }
          if (!allTrue) {
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

        EditorGUI.BeginChangeCheck();

        if (_specifiedDrawers.TryGetValue(iterator.name, out customDrawer)) {
          customDrawer(iterator);
        } else {
          using (new EditorGUI.DisabledGroupScope(isFirst)) {
            EditorGUILayout.PropertyField(iterator, true);
          }
        }

        if (EditorGUI.EndChangeCheck()) {
          _modifiedProperties.Add(iterator.Copy());
        }

        isFirst = false;
      }

      serializedObject.ApplyModifiedProperties();
    }
  }
}
