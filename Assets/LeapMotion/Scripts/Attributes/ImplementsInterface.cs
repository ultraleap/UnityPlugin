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

  public static class ImplementsInterfaceExtention {

    // http://stackoverflow.com/a/19317229/2471635
    public static bool ImplementsInterface(this Type type, Type ifaceType) {
      Type[] intf = type.GetInterfaces();
      for (int i = 0; i < intf.Length; i++) {
        if (intf[i] == ifaceType) {
          return true;
        }
      }
      return false;
    }

  }

}
