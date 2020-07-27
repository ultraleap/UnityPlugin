/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;
using System;
using System.Collections.Generic;
using Leap.Unity.Query;
using System.Text.RegularExpressions;

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

        UnityObject implementingObject = FindImplementer(property,
                                                         property.objectReferenceValue);

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
    public UnityObject FindImplementer(SerializedProperty property, UnityObject fromObj) {

      // Don't use fieldInfo here because it is very convenient for it to be left null
      // on the CombinablePropertyAttribute when dealing with generic-type situations
      // where the ImplementsInterface class has to be constructed manually in a custom
      // editor. (Specific case: StreamConnectorEditor.cs)

      bool isTypeDefinitelyIncompatible;
      {
        isTypeDefinitelyIncompatible = !this.type.IsAssignableFrom(fromObj.GetType());

        // We have to make an exception when a GameObject is dragged into a field whose
        // type is a Component, because it's expected that we can use GetComponent in
        // that case. (We might still fail later if the component isn't found.)
        var objIsGameObject = fromObj.GetType() == typeof(GameObject);
        if (objIsGameObject) {
          isTypeDefinitelyIncompatible = false;
        }

        // We also make an exception when a Component is dragged into a field that
        // doesn't directly satisfy the interface because it might have other Components
        // on its GameObject that _do_ satisfy the field, again via GetComponent.
        var objIsComponent = typeof(Component).IsAssignableFrom(fromObj.GetType());
        if (objIsComponent) {
          isTypeDefinitelyIncompatible = false;
        }

        // However, you can't assign a ScriptableObject to a field expecting a Component,
        // or vice-versa, no matter what.

        var fieldIsScriptableObject
            = fieldInfo.FieldType.IsAssignableFrom(typeof(ScriptableObject));
        if (fieldIsScriptableObject && (objIsComponent || objIsGameObject)) {
          isTypeDefinitelyIncompatible = true;
        }

        var fieldTakesComponent
            = typeof(Component).IsAssignableFrom(fieldInfo.FieldType);
        if (fieldTakesComponent && (!objIsComponent && !objIsGameObject)) {
          isTypeDefinitelyIncompatible = true;
        }
      }
      if (isTypeDefinitelyIncompatible) {
        return null;
      }

      if (fromObj.GetType().ImplementsInterface(type)) {
        // All good! This object reference implements the interface.
        return fromObj;
      }
      else {
        UnityObject implementingObject;

        if (fromObj is GameObject) {
          fromObj = (fromObj as GameObject).transform;
        }

        if (fromObj is Component) {
          // If the object is a Component, first search the rest of the GameObject 
          // for a component that implements the interface. If found, assign it instead,
          // otherwise null out the property.
          implementingObject = (fromObj as Component)
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

    public bool IsDropValid(UnityObject[] draggedObjects, SerializedProperty property) {
      return draggedObjects.Query().Any(o => FindImplementer(property, o) != null);
    }

    public void ProcessDroppedObjects(UnityObject[] droppedObjects,
                                      SerializedProperty property) {

      var implementer = droppedObjects.Query()
                                      .FirstOrDefault(o => FindImplementer(property, o));

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
