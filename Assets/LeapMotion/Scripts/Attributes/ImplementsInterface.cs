/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

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
