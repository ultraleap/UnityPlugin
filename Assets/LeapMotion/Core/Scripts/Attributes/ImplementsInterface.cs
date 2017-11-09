/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using System;
using System.Collections.Generic;
using Leap.Unity.Query;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityObject = UnityEngine.Object;

namespace Leap.Unity.Attributes {

  public class ImplementsInterfaceAttribute : CombinablePropertyAttribute,
                                              IPropertyConstrainer {

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
        var objectReferenceValue = property.objectReferenceValue;

        if (objectReferenceValue.GetType().ImplementsInterface(type)) {
          // All good! This object reference implements the interface.
          return;
        }
        else {
          UnityObject implementingObject;

          if (objectReferenceValue is Component) {
            // If the object is a Component, first search the rest of the GameObject 
            // for a component that implements the interface. If found, assign it instead,
            // otherwise null out the property.
            implementingObject = (objectReferenceValue as Component)
                                 .GetComponents<Component>()
                                 .Query()
                                 .Where(c => c.GetType().ImplementsInterface(type))
                                 .FirstOrDefault();
          } 
          else {
            // If the object is not a Component, just null out the property.
            implementingObject = null;
          }

          if (implementingObject == null) {
            Debug.LogError(property.objectReferenceValue.GetType().Name + " does not implement " + type.Name);
          }

          property.objectReferenceValue = implementingObject;
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
