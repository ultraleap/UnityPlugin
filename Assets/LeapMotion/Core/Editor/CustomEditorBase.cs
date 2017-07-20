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
    protected Dictionary<string, List<Action<SerializedProperty>>> _specifiedPostDecorators;
    protected Dictionary<string, List<Func<bool>>> _conditionalProperties;
    protected List<string> _deferredProperties;
    protected bool _showScriptField = true;

    private bool _canCallSpecifyFunctions = false;

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
      throwIfNotInOnEnable("specifyCustomDrawer");

      if (!validateProperty(propertyName)) {
        return;
      }

      _specifiedDrawers[propertyName] = propertyDrawer;
    }

    /// <summary>
    /// Specify a callback to be used to draw a decorator for a specific named property.  Should be called in OnEnable.
    /// </summary>
    protected void specifyCustomDecorator(string propertyName, Action<SerializedProperty> decoratorDrawer) {
      throwIfNotInOnEnable("specifyCustomDecorator");

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
    /// Specify a callback to be used to draw a decorator AFTER a specific named property.
    /// 
    /// Should be called in OnEnable.
    /// </summary>
    protected void specifyCustomPostDecorator(string propertyName, Action<SerializedProperty> decoratorDrawer) {
      throwIfNotInOnEnable("specifyCustomPostDecorator");

      if (!validateProperty(propertyName)) {
        return;
      }

      List<Action<SerializedProperty>> list;
      if (!_specifiedPostDecorators.TryGetValue(propertyName, out list)) {
        list = new List<Action<SerializedProperty>>();
        _specifiedPostDecorators[propertyName] = list;
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
      throwIfNotInOnEnable("specifyConditionalDrawing");

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
      throwIfNotInOnEnable("specifyConditionalDrawing");

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

    protected void hideField(string propertyName) {
      throwIfNotInOnEnable("hideField");

      specifyConditionalDrawing(() => false, propertyName);
    }

    protected void specifyConditionalDrawing(Func<bool> conditional, params string[] dependantProperties) {
      throwIfNotInOnEnable("specifyConditionalDrawing");

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

    /// <summary>
    /// Defer rendering of a property until the end of the inspector.  Deferred properties
    /// are drawn in the REVERSE order they are deferred!  NOT by the order they appear in the 
    /// serialized object!
    /// </summary>
    protected void deferProperty(string propertyName) {
      throwIfNotInOnEnable("deferProperty");

      if (!validateProperty(propertyName)) {
        return;
      }

      _deferredProperties.Insert(0, propertyName);
    }

    protected void drawScriptField(bool disable = true) {
      var scriptProp = serializedObject.FindProperty("m_Script");
      EditorGUI.BeginDisabledGroup(disable);
      EditorGUILayout.PropertyField(scriptProp);
      EditorGUI.EndDisabledGroup();
    }

    protected virtual void OnEnable() {
      try {
        if (serializedObject == null) { }
      } catch (NullReferenceException) {
        DestroyImmediate(this);
        throw new Exception("Cleaning up an editor of type " + GetType() + ".  Make sure to always destroy your editors when you are done with them!");
      }

      _specifiedDrawers = new Dictionary<string, Action<SerializedProperty>>();
      _specifiedDecorators = new Dictionary<string, List<Action<SerializedProperty>>>();
      _specifiedPostDecorators = new Dictionary<string, List<Action<SerializedProperty>>>();
      _conditionalProperties = new Dictionary<string, List<Func<bool>>>();
      _deferredProperties = new List<string>();
      _canCallSpecifyFunctions = true;
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
      _canCallSpecifyFunctions = false;

      _modifiedProperties.Clear();
      SerializedProperty iterator = serializedObject.GetIterator();
      bool isFirst = true;

      while (iterator.NextVisible(isFirst)) {
        if (isFirst && !_showScriptField) {
          isFirst = false;
          continue;
        }

        if (_deferredProperties.Contains(iterator.name)) {
          continue;
        }

        using (new EditorGUI.DisabledGroupScope(isFirst)) {
          drawProperty(iterator);
        }

        isFirst = false;
      }

      foreach (var deferredProperty in _deferredProperties) {
        drawProperty(serializedObject.FindProperty(deferredProperty));
      }

      serializedObject.ApplyModifiedProperties();
    }

    private void drawProperty(SerializedProperty property) {
      List<Func<bool>> conditionalList;
      if (_conditionalProperties.TryGetValue(property.name, out conditionalList)) {
        bool allTrue = true;
        for (int i = 0; i < conditionalList.Count; i++) {
          allTrue &= conditionalList[i]();
        }
        if (!allTrue) {
          return;
        }
      }

      Action<SerializedProperty> customDrawer;

      List<Action<SerializedProperty>> decoratorList;
      if (_specifiedDecorators.TryGetValue(property.name, out decoratorList)) {
        for (int i = 0; i < decoratorList.Count; i++) {
          decoratorList[i](property);
        }
      }

      EditorGUI.BeginChangeCheck();

      if (_specifiedDrawers.TryGetValue(property.name, out customDrawer)) {
        customDrawer(property);
      } else {
        EditorGUILayout.PropertyField(property, true);
      }

      if (EditorGUI.EndChangeCheck()) {
        _modifiedProperties.Add(property.Copy());
      }


      List<Action<SerializedProperty>> postDecoratorList;
      if (_specifiedPostDecorators.TryGetValue(property.name, out postDecoratorList)) {
        for (int i = 0; i < postDecoratorList.Count; i++) {
          postDecoratorList[i](property);
        }
      }
    }

    private void throwIfNotInOnEnable(string methodName) {
      if (!_canCallSpecifyFunctions) {
        throw new InvalidOperationException("Cannot call " + methodName + " from within any other function but OnEnable.  Make sure you also call base.OnEnable as well!");
      }
    }
  }
}
