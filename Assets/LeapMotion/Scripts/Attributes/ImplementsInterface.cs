using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;
using System;

namespace Leap.Unity.Attributes {

  public class ImplementsInterfaceAttribute : CombinablePropertyAttribute, IPropertyConstrainer {

    private Type type;

    public ImplementsInterfaceAttribute(Type type) {
      this.type = type;
    }

#if UNITY_EDITOR
    public void ConstrainValue(SerializedProperty property) {
      if (property.objectReferenceValue != null) {
        if (!type.ImplementsInterface(property.objectReferenceValue.GetType())) {
          Debug.LogError(property.objectReferenceValue.GetType().Name + " does not implement " + type.Name);
          property.objectReferenceValue = null;
        }
      }
    }

    public override IEnumerable<SerializedPropertyType> SupportedTypes {
      get {
        yield return SerializedPropertyType.ObjectReference;
      }
    }

#endif
  }

}
