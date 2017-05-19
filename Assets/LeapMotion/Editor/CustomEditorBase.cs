/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using Leap.Unity.Query;

namespace Leap.Unity {

  public class CustomEditorBase<T> : CustomEditorBase where T : UnityEngine.Object {
    protected new T target;
    protected new T[] targets;

    protected override void OnEnable() {
      base.OnEnable();

      target = base.target as T;
      targets = base.targets.Query().
                             Where(t => t != null).
                             OfType<T>().
                             ToArray();
    }
  }

  public class CustomEditorBase : Editor {
    protected Dictionary<string, Action<SerializedProperty>> _specifiedDrawers;
    protected Dictionary<string, List<Action<SerializedProperty>>> _specifiedDecorators;
    protected Dictionary<string, List<Func<bool>>> _conditionalProperties;
    protected HashSet<string> _beginHorizontalProperties;
    protected HashSet<string> _endHorizontalProperties;
    protected bool _showScriptField = true;

    protected List<SerializedProperty> _modifiedProperties = new List<SerializedProperty>();

    protected void dontShowScriptField() {
      _showScriptField = false;
    }

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
      specifyConditionalDrawing(() => {
        if (conditionalProp.hasMultipleDifferentValues) {
          return false;
        } else {
          return conditionalProp.boolValue;
        }
      }, dependantProperties);
    }

    protected void specifyConditionalDrawing(string enumName, int enumValue, params string[] dependantProperties) {
      if (!validateProperty(enumName)) {
        return;
      }

      SerializedProperty enumProp = serializedObject.FindProperty(enumName);
      specifyConditionalDrawing(() => {
        if (enumProp.hasMultipleDifferentValues) {
          return false;
        } else {
          return enumProp.intValue == enumValue;
        }
      }, dependantProperties);
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

    protected void createHorizonalSection(string beginProperty, string endProperty) {
      validateProperty(beginProperty);
      validateProperty(endProperty);
      _beginHorizontalProperties.Add(beginProperty);
      _endHorizontalProperties.Add(endProperty);
    }

    protected virtual void OnEnable() {
      _specifiedDrawers = new Dictionary<string, Action<SerializedProperty>>();
      _specifiedDecorators = new Dictionary<string, List<Action<SerializedProperty>>>();
      _conditionalProperties = new Dictionary<string, List<Func<bool>>>();
      _beginHorizontalProperties = new HashSet<string>();
      _endHorizontalProperties = new HashSet<string>();
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
        if (isFirst && !_showScriptField) {
          isFirst = false;
          continue;
        }

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

        if (_beginHorizontalProperties.Contains(iterator.name)) {
          EditorGUILayout.BeginHorizontal();
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

        if (_endHorizontalProperties.Contains(iterator.name)) {
          EditorGUILayout.EndHorizontal();
        }

        isFirst = false;
      }

      serializedObject.ApplyModifiedProperties();
    }
  }
}
