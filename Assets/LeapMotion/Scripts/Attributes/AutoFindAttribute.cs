/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using System.Reflection;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
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
    public readonly AutoFindLocations searchLocations;

    public AutoFindAttribute(AutoFindLocations searchLocations = AutoFindLocations.All) {
      this.searchLocations = searchLocations;
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

    private bool search(SerializedProperty property, AutoFindLocations location, Func<Type, UnityEngine.Object> searchDelegate) {
      if ((searchLocations & location) != 0) {
        var value = searchDelegate(fieldInfo.FieldType);
        if (value != null) {
          property.objectReferenceValue = value;
          return true;
        }
      }
      return false;
    }

    [PostProcessScene]
    private static void OnPostProcessScene() {
      MonoBehaviour[] scripts = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();

      Dictionary<KeyValuePair<Type, string>, KeyValuePair<AutoFindAttribute, FieldInfo>> cache = new Dictionary<KeyValuePair<Type, string>, KeyValuePair<AutoFindAttribute, FieldInfo>>();

      for (int j = 0; j < scripts.Length; j++) {
        MonoBehaviour script = scripts[j];

        SerializedObject sObj = new SerializedObject(script);
        SerializedProperty it = sObj.GetIterator();

        Type scriptType = script.GetType();
        bool wasConstrained = false;

        it.NextVisible(true);
        while (it.NextVisible(false)) {
          KeyValuePair<Type, string> key = new KeyValuePair<Type, string>(scriptType, it.name);

          KeyValuePair<AutoFindAttribute, FieldInfo> info;
          if (!cache.TryGetValue(key, out info)) {
            FieldInfo field = scriptType.GetField(it.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
            if (field == null) continue;

            object[] attributes = field.GetCustomAttributes(typeof(AutoFindAttribute), true);

            if (attributes.Length == 0) {
              cache[key] = new KeyValuePair<AutoFindAttribute, FieldInfo>(null, null);
            } else {
              cache[key] = info = new KeyValuePair<AutoFindAttribute, FieldInfo>(attributes[0] as AutoFindAttribute, field);
            }
          }

          AutoFindAttribute attribute = info.Key;

          if (attribute != null) {
            wasConstrained = true;
            attribute.component = script;
            attribute.fieldInfo = info.Value;
            attribute.ConstrainValue(it);
          }
        }

        if (wasConstrained) {
          sObj.ApplyModifiedProperties();
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
