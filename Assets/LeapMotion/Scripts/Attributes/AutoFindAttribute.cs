using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Leap.Unity.Attributes {

  public class AutoFindAttribute : CombinablePropertyAttribute, IPropertyConstrainer {

#if UNITY_EDITOR
    public void ConstrainValue(SerializedProperty property) {
      if (property.objectReferenceValue != null) return;
      if (component == null) return;

      property.objectReferenceValue = component.GetComponentInChildren(fieldInfo.FieldType);
      if (property.objectReferenceValue != null) return;

      property.objectReferenceValue = component.GetComponentInParent(fieldInfo.FieldType);
      if (property.objectReferenceValue != null) return;

      property.objectReferenceValue = UnityEngine.Object.FindObjectOfType(fieldInfo.FieldType);
    }
#endif
  }
}
