/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityObject = UnityEngine.Object;

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
    private Action<UnityObject, object> _cachedDelegate;

    public override void OnPropertyChanged(SerializedProperty property) {
      base.OnPropertyChanged(property);

      if (_cachedDelegate == null) {
        Type type = targets[0].GetType();

        PropertyInfo propertyInfo = type.GetProperty(methodName,
          BindingFlags.Public |
          BindingFlags.NonPublic |
          BindingFlags.Instance
        );
        if (propertyInfo != null) {
          _cachedDelegate = (obj, arg) => propertyInfo.SetValue(obj, arg, null);
        }
        else {
          MethodInfo method = type.GetMethod(methodName,
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.Static |
            BindingFlags.Instance |
            BindingFlags.FlattenHierarchy
          );

          if (method == null) {
            Debug.LogWarning("Could not find a property or method of the name " +
              methodName + " " + "to invoke for the OnChange attribute.");
            return;
          }

          int paramCount = method.GetParameters().Length;
          if (paramCount == 0) {
            _cachedDelegate = (obj, arg) => method.Invoke(obj, null);
          } else if (paramCount == 1) {
            object[] argArray = new object[1];
            _cachedDelegate = (obj, arg) => {
              argArray[0] = arg;
              method.Invoke(obj, argArray);
            };
          } else {
            Debug.LogWarning("Could not invoke the method " + methodName +
              " from OnChange because the method had more than 1 argument.");
          }
        }
      }

      property.serializedObject.ApplyModifiedProperties();

      foreach (var target in targets) {
        object newValue = fieldInfo.GetValue(target);
        _cachedDelegate(target, newValue);
      }
    }
#endif
  }
}
