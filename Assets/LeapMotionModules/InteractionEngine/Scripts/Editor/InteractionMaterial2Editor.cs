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

          TypeData data = new TypeData();
          data.types = AppDomain.CurrentDomain.GetAssemblies()
                                .SelectMany(s => s.GetTypes())
                                .Where(p => p.IsSubclassOf(propertyType))
                                .ToList();

          data.dropdownNames = new string[] { "None" }.Concat(data.types.Select(t => t.Name)).ToArray();

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

          int index;
          if (it.objectReferenceValue == null) {
            index = 0;
          } else {
            index = data.types.IndexOf(it.objectReferenceValue.GetType()) + 1;
          }

          int newIndex = EditorGUILayout.Popup(index, data.dropdownNames);

          if (newIndex != index) {
            if (it.objectReferenceValue != null) {
              DestroyImmediate(it.objectReferenceValue, true);
              it.objectReferenceValue = null;
            }

            if (newIndex != 0) {
              var newOne = CreateInstance(data.types[newIndex - 1]);
              newOne.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
              AssetDatabase.AddObjectToAsset(newOne, target);
              it.objectReferenceValue = newOne;
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

    private struct TypeData {
      public List<Type> types;
      public string[] dropdownNames;
    }


  }
}
