using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;
using System;

namespace Leap.Unity.Attributes {

  public class ImplementsInterfaceAttribute : CombinablePropertyAttribute, IPropertyConstrainer {

#pragma warning disable 0414
    private Type type;
#pragma warning restore 0414

    public ImplementsInterfaceAttribute(Type type) {
      if (!type.IsInterface) {
        throw new System.Exception(type.Name + " is not an interface.");
      }
      this.type = type;
    }

#if UNITY_EDITOR
    public void ConstrainValue(SerializedProperty property) {
      if (property.objectReferenceValue != null) {
        if (!property.objectReferenceValue.GetType().ImplementsInterface(type)) {
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
