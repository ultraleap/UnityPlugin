using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;

namespace Leap.Unity.Attributes {

  public class DisableIf : CombinablePropertyAttribute, IPropertyDisabler {
    public readonly string propertyName;
    public readonly object testValue;
    public readonly bool disableResult;

    public DisableIf(string propertyName, object equalTo = null, object notEqualTo = null) {
      this.propertyName = propertyName;

      if ((equalTo != null) == (notEqualTo != null)) {
        throw new ArgumentException("Must specify exactly one of 'equalTo' or 'notEqualTo'.");
      }

      if (equalTo != null) {
        testValue = equalTo;
        disableResult = true;
      } else if (notEqualTo != null) {
        testValue = notEqualTo;
        disableResult = false;
      }

      if (!(testValue is bool) && !(testValue is Enum)) {
        throw new ArgumentException("Only values of bool or Enum are allowed in comparisons using DisableIf.");
      }
    }

#if UNITY_EDITOR
    public bool ShouldDisable(SerializedProperty property) {
      SerializedProperty prop = property.serializedObject.FindProperty(propertyName);

      if (prop.propertyType == SerializedPropertyType.Boolean) {
        return (prop.boolValue == (bool)testValue) == disableResult;
      } else if (prop.propertyType == SerializedPropertyType.Enum) {
        return (prop.intValue == (int)testValue) == disableResult;
      } else {
        Debug.LogError("Can only conditionally disable based on boolean or enum types.");
        return false;
      }
    }
#endif
  }
}
