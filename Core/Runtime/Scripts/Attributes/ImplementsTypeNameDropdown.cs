/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Leap.Unity.Attributes {

  /// <summary>
  /// Place this attribute on a serialized string field to have it render as a dropdown
  /// menu that is automatically populated with implementing types of the type provided
  /// to the attribute. The string field will contain the type name selected by the user.
  /// This can be converted into a Type reference via reflection and used to construct
  /// objects by their type, for example, or to construct ScriptableObjects by their type.
  /// </summary>
  public class ImplementsTypeNameDropdownAttribute : CombinablePropertyAttribute,
                                                     IFullPropertyDrawer {
    
    protected Type _baseType;
    protected List<Type> _implementingTypes = new List<Type>();
    protected GUIContent[] _typeOptions;

    public ImplementsTypeNameDropdownAttribute(Type type) {
      _baseType = type;

#if UNITY_EDITOR
      refreshImplementingTypes();
      refreshTypeOptions();
#endif
    }

#if UNITY_EDITOR
    public void DrawProperty(Rect rect, SerializedProperty property, GUIContent label) {
      int curSelectedIdx = getCurSelectedIdx(property);

      int selectedIdx = EditorGUI.Popup(rect, label, curSelectedIdx, _typeOptions);
      if (selectedIdx != curSelectedIdx) {
        property.stringValue = _implementingTypes[selectedIdx].FullName;
      }

      if (curSelectedIdx == -1 && _implementingTypes.Count > 0) {
        curSelectedIdx = 0;
        property.stringValue = _implementingTypes[curSelectedIdx].FullName;
      }
    }

    private void refreshImplementingTypes() {
      _implementingTypes.Clear();

      foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
        foreach (var type in assembly.GetTypes()) {
          if (_baseType.IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface) {
            _implementingTypes.Add(type);
          }
        }
      }
    }

    private void refreshTypeOptions() {
      _typeOptions = new GUIContent[_implementingTypes.Count];

      for (int i = 0; i < _typeOptions.Length; i++) {
        _typeOptions[i] = new GUIContent(_implementingTypes[i].Name);
      }
    }

    private int getCurSelectedIdx(SerializedProperty property) {
      return _implementingTypes.FindIndex((t => property.stringValue.Equals(t.FullName)));
    }
#endif
  }


}
