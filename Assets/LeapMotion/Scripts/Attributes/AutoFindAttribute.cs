using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Leap.Unity.Attributes {

  public enum AutoFindLocations {
    Object = 0x01,
    Children = 0x02,
    Parents = 0x04,
    Scene = 0x08,
    All = 0xFFFF
  }

  public class AutoFindAttribute : CombinablePropertyAttribute, IPropertyConstrainer {
    private AutoFindLocations _searchLocations;

    public AutoFindAttribute(AutoFindLocations searchLocations = AutoFindLocations.All) {
      _searchLocations = searchLocations;
    }

#if UNITY_EDITOR
    public void ConstrainValue(SerializedProperty property) {
      if (property.objectReferenceValue != null) return;
      if (component == null) return;
      
      if (search(property, AutoFindLocations.Object, component.GetComponent)) return;
      if (search(property, AutoFindLocations.Parents, component.GetComponentInParent)) return;
      if (search(property, AutoFindLocations.Children, component.GetComponentInChildren)) return;
      if (search(property, AutoFindLocations.Scene, UnityEngine.Object.FindObjectOfType)) return;
    }

    private bool search(SerializedProperty property, AutoFindLocations location,Func<Type, UnityEngine.Object> searchDelegate) {
      if ((_searchLocations & location) != 0) {
        var value = searchDelegate(fieldInfo.FieldType);
        if (value != null) {
          property.objectReferenceValue = value;
          return true;
        }
      }
      return false;
    }

    public override IEnumerable<SerializedPropertyType> SupportedTypes {
      get {
        yield return SerializedPropertyType.ObjectReference;
      }
    }
#endif
  }
}
