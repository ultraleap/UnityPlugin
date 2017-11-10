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
                                              IPropertyConstrainer,
                                              IFullPropertyDrawer,
                                              ISupportDragAndDrop {

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

        UnityObject implementingObject = FindImplementer(property.objectReferenceValue);

        if (implementingObject == null) {
          Debug.LogError(property.objectReferenceValue.GetType().Name + " does not implement " + type.Name);
        }

        property.objectReferenceValue = implementingObject;
      }
    }

    /// <summary>
    /// Checks if the object or one of its associated GameObject components implements
    /// the interface that this attribute constrains objects to, and returns the object
    /// that implements that interface, or null if none was found.
    /// </summary>
    public UnityObject FindImplementer(UnityObject obj) {
      if (obj.GetType().ImplementsInterface(type)) {
        // All good! This object reference implements the interface.
        return obj;
      }
      else {
        UnityObject implementingObject;

        if (obj is Component) {
          // If the object is a Component, first search the rest of the GameObject 
          // for a component that implements the interface. If found, assign it instead,
          // otherwise null out the property.
          implementingObject = (obj as Component)
                               .GetComponents<Component>()
                               .Query()
                               .Where(c => c.GetType().ImplementsInterface(type))
                               .FirstOrDefault();
        } 
        else {
          // If the object is not a Component, just null out the property.
          implementingObject = null;
        }

        return implementingObject;
      }
    }

    public void DrawProperty(Rect rect, SerializedProperty property, GUIContent label) {
      if (property.objectReferenceValue != null) {
        EditorGUI.ObjectField(rect, property, type, label);
      }
      else {
        EditorGUI.ObjectField(rect, label, null, type, false);
      }
    }

    public Rect GetDropArea(Rect rect, SerializedProperty property) {
      return rect;
    }

    public bool IsDropValid(UnityEngine.Object obj, SerializedProperty property) {
      return FindImplementer(obj) != null;
    }

    public void ProcessDroppedObject(UnityEngine.Object droppedObj, SerializedProperty property) {
      var implementer = FindImplementer(droppedObj);

      if (implementer == null) {
        Debug.LogError(property.objectReferenceValue.GetType().Name
                       + " does not implement " + type.Name);
      }
      else {
        property.objectReferenceValue = implementer;
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
