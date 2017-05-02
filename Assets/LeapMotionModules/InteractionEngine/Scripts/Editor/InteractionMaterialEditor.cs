/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace Leap.Unity.Interaction {

  [CustomEditor(typeof(InteractionMaterial))]
  public class InteractionMaterialEditor : CustomEditorBase {

    private Dictionary<string, TypeData> _propertyToType;

    protected override void OnEnable() {
      base.OnEnable();

      _propertyToType = new Dictionary<string, TypeData>();

      Type targetType = typeof(InteractionMaterial);
      var it = serializedObject.GetIterator();

      while (it.NextVisible(true)) {
        if (it.propertyType == SerializedPropertyType.ObjectReference) {
          FieldInfo fieldInfo = targetType.GetField(it.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
          if (fieldInfo == null) {
            continue;
          }

          Type propertyType = fieldInfo.FieldType;

          var attributeObj = fieldInfo.GetCustomAttributes(typeof(InteractionMaterial.ControllerAttribute), true).FirstOrDefault();
          if (attributeObj == null) {
            continue;
          }

          TypeData data = new TypeData();

          data.controllerAttribute = attributeObj as InteractionMaterial.ControllerAttribute;

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

      SerializedProperty prop = serializedObject.FindProperty("_physicMaterialMode");
      specifyConditionalDrawing(() => prop.intValue == (int)InteractionMaterial.PhysicMaterialModeEnum.Replace, "_replacementMaterial");

      specifyConditionalDrawing("_warpingEnabled", "_warpCurve", "_graphicalReturnTime");

      specifyCustomDrawer("_holdingPoseController", controllerDrawer);
      specifyCustomDrawer("_moveToController", controllerDrawer);
      specifyCustomDrawer("_suspensionController", controllerDrawer);
      specifyCustomDrawer("_throwingController", controllerDrawer);
      specifyCustomDrawer("_layerController", controllerDrawer);
    }

    private void controllerDrawer(SerializedProperty controller) {
      TypeData data;
      if (!_propertyToType.TryGetValue(controller.name, out data)) {
        Debug.LogWarning("Could not find controller data for property " + controller.name);
        return;
      }

      EditorGUILayout.Space();
      EditorGUILayout.LabelField(controller.displayName, EditorStyles.boldLabel);

      Type type;
      if (controller.objectReferenceValue == null) {
        type = typeof(void);
      } else {
        type = controller.objectReferenceValue.GetType();
      }

      int index = data.types.IndexOf(type);
      int newIndex = EditorGUILayout.Popup(index, data.dropdownNames);

      if (newIndex != index) {
        if (controller.objectReferenceValue != null) {
          DestroyImmediate(controller.objectReferenceValue, true);
          controller.objectReferenceValue = null;
        }

        Type newType = data.types[newIndex];
        if (newType != typeof(void)) {
          controller.objectReferenceValue = createObjectOfType(newType);
        }
      }

      if (controller.objectReferenceValue != null) {
        SerializedObject sObj = new SerializedObject(controller.objectReferenceValue);
        SerializedProperty sIt = sObj.GetIterator();

        bool isFirst = true;
        while (sIt.NextVisible(isFirst)) {
          EditorGUI.BeginDisabledGroup(isFirst);
          EditorGUILayout.PropertyField(sIt);
          EditorGUI.EndDisabledGroup();
          isFirst = false;
        }

        sObj.ApplyModifiedProperties();
      }
    }

    private ScriptableObject createObjectOfType(Type type) {
      var newOne = CreateInstance(type);
      newOne.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
      AssetDatabase.AddObjectToAsset(newOne, target);
      return newOne;
    }

    private struct TypeData {
      public InteractionMaterial.ControllerAttribute controllerAttribute;
      public List<Type> types;
      public string[] dropdownNames;
    }
  }
}
