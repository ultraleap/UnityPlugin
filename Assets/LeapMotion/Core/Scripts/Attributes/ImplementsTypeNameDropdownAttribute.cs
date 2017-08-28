using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
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
    
    private Type _baseType;
    private List<Type> _implementingTypes = new List<Type>();
    private GUIContent[] _typeOptions;

    public ImplementsTypeNameDropdownAttribute(Type type) {
      _baseType = type;

      refreshImplementingTypes();
      refreshTypeOptions();
    }

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
  }


}