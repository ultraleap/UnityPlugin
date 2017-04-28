/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Leap.Unity.Attributes {

  /// <summary>
  /// Use the OnChange attribute to recieve a callback whenever a field
  /// is changed.  The callback can be in the form of:
  ///  - A method accepting zero arguments
  ///  - A method accepting a single argument matching the field type 
  ///      (new value is passed in as the argument)
  ///  - A property matching the type of the field 
  ///      (the setter is called with the new value)
  /// </summary>
  public class OnEditorChangeAttribute : CombinablePropertyAttribute {
    public readonly string methodName;

    public OnEditorChangeAttribute(string methodName) {
      this.methodName = methodName;
    }

#if UNITY_EDITOR
    private Action<object> _cachedDelegate;

    public override void OnPropertyChanged(SerializedProperty property) {
      base.OnPropertyChanged(property);

      if (_cachedDelegate == null) {
        Type type = component.GetType();

        PropertyInfo propertyInfo = type.GetProperty(methodName, BindingFlags.Public |
                                                                 BindingFlags.NonPublic |
                                                                 BindingFlags.Instance);
        if (propertyInfo != null) {
          _cachedDelegate = arg => propertyInfo.SetValue(component, arg, null);
        } else {
          MethodInfo method = type.GetMethod(methodName, BindingFlags.Public |
                                                         BindingFlags.NonPublic |
                                                         BindingFlags.Static |
                                                         BindingFlags.Instance);

          if (method == null) {
            Debug.LogWarning("Could not find a property or method of the name " + methodName + " " +
                              "to invoke for the OnChange attribute.");
            return;
          }

          int paramCount = method.GetParameters().Length;
          if (paramCount == 0) {
            _cachedDelegate = arg => method.Invoke(component, null);
          } else if (paramCount == 1) {
            object[] argArray = new object[1];
            _cachedDelegate = arg => {
              argArray[0] = arg;
              method.Invoke(component, argArray);
            };
          } else {
            Debug.LogWarning("Could not invoke the method " + methodName + " from OnChange " +
                             "because the method had more than 1 argument.");
          }
        }
      }

      property.serializedObject.ApplyModifiedProperties();
      object newValue = fieldInfo.GetValue(component);
      _cachedDelegate(newValue);
    }
#endif
  }
}
