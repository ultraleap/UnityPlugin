using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace Leap.Unity.Interaction {

  [CustomEditor(typeof(InteractionMaterial2))]
  public class InteractionMaterial2Editor : Editor {

    private Dictionary<string, TypeData> _propertyToType;

    void OnEnable() {
      _propertyToType = new Dictionary<string, TypeData>();

      Type targetType = typeof(InteractionMaterial2);
      var it = serializedObject.GetIterator();

      while (it.NextVisible(true)) {
        if (it.propertyType == SerializedPropertyType.ObjectReference) {
          FieldInfo fieldInfo = targetType.GetField(it.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
          if (fieldInfo == null) {
            continue;
          }

          Type propertyType = fieldInfo.FieldType;

          var attributeObj = fieldInfo.GetCustomAttributes(typeof(InteractionMaterial2.ControllerAttribute), true).FirstOrDefault();
          if (attributeObj == null) {
            continue;
          }

          TypeData data = new TypeData();

          data.controllerAttribute = attributeObj as InteractionMaterial2.ControllerAttribute;

          data.types = AppDomain.CurrentDomain.GetAssemblies()
                                .SelectMany(s => s.GetTypes())
                                .Where(p => p.IsSubclassOf(propertyType))
                                .OrderBy(t => t.Name)
                                .ToList();

          if (data.controllerAttribute.AllowNone) {
            data.types.Insert(0, typeof(void));
          }

          data.dropdownNames = data.types.Select(t => {
            if (t == typeof(void)) {
              return "None";
            } else {
              return t.Name;
            }
          }).ToArray();

          _propertyToType[it.name] = data;
        }
      }
    }

    public override void OnInspectorGUI() {
      SerializedProperty it = serializedObject.GetIterator();

      while (it.NextVisible(true)) {
        TypeData data;
        if (_propertyToType.TryGetValue(it.name, out data)) {
          EditorGUILayout.Space();
          EditorGUILayout.LabelField(it.displayName);

          Type type;
          if (it.objectReferenceValue == null) {
            type = typeof(void);
          } else {
            type = it.objectReferenceValue.GetType();
          }

          int index = data.types.IndexOf(type);
          int newIndex = EditorGUILayout.Popup(index, data.dropdownNames);

          if (newIndex != index) {
            if (it.objectReferenceValue != null) {
              DestroyImmediate(it.objectReferenceValue, true);
              it.objectReferenceValue = null;
            }

            Type newType = data.types[newIndex];
            if (newType != typeof(void)) {
              it.objectReferenceValue = createObjectOfType(newType);
            }
          }

          if (it.objectReferenceValue != null) {
            SerializedObject sObj = new SerializedObject(it.objectReferenceValue);
            SerializedProperty sIt = sObj.GetIterator();

            sIt.NextVisible(true);
            while (sIt.NextVisible(false)) {
              EditorGUILayout.PropertyField(sIt);
            }

            sObj.ApplyModifiedProperties();
          }
        } else {
          EditorGUILayout.PropertyField(it);
        }
      }


      serializedObject.ApplyModifiedProperties();
    }

    private ScriptableObject createObjectOfType(Type type) {
      var newOne = CreateInstance(type);
      newOne.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
      AssetDatabase.AddObjectToAsset(newOne, target);
      return newOne;
    }

    private struct TypeData {
      public InteractionMaterial2.ControllerAttribute controllerAttribute;
      public List<Type> types;
      public string[] dropdownNames;
    }
  }
}
