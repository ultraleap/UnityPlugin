using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using Leap.Unity;

[CustomEditor(typeof(PropertyRecorder))]
public class PropertyRecorderEditor : CustomEditorBase<PropertyRecorder> {

  protected override void OnEnable() {
    base.OnEnable();

    specifyCustomDrawer("serializedComponents", drawProperties);
  }

  private List<Component> _components = new List<Component>();
  private void drawProperties(SerializedProperty list) {
    //Clear out the nulls
    for (int i = list.arraySize; i-- != 0;) {
      SerializedProperty componentInfoProp = list.GetArrayElementAtIndex(i);
      SerializedProperty componentProp = componentInfoProp.FindPropertyRelative("component");

      if (componentProp.objectReferenceValue == null) {
        list.DeleteArrayElementAtIndex(i);
      }
    }

    target.GetComponents(_components);

    int indexOfRecorder = _components.IndexOf(target);
    if (indexOfRecorder < 0) {
      Debug.LogError("Should alwayus be able to find the recorder!");
      return;
    }

    _components.RemoveAt(indexOfRecorder);

    //Make sure every component is accounted for
    foreach (var component in _components) {
      bool alreadyExists = false;
      for (int i = 0; i < list.arraySize; i++) {
        SerializedProperty componentInfoProp = list.GetArrayElementAtIndex(i);
        SerializedProperty componentProp = componentInfoProp.FindPropertyRelative("component");

        if (componentProp.objectReferenceValue == component) {
          alreadyExists = true;
          break;
        }
      }

      if (!alreadyExists) {
        list.InsertArrayElementAtIndex(list.arraySize);
        SerializedProperty componentInfoprop = list.GetArrayElementAtIndex(list.arraySize - 1);
        SerializedProperty componentProp = componentInfoprop.FindPropertyRelative("component");
        componentProp.objectReferenceValue = component;
      }
    }

    foreach (var component in _components) {
      SerializedProperty expandedProp = null, propertiesProp = null;

      for (int i = 0; i < list.arraySize; i++) {
        SerializedProperty componentInfoProp = list.GetArrayElementAtIndex(i);
        SerializedProperty componentProp = componentInfoProp.FindPropertyRelative("component");
        if (componentProp.objectReferenceValue == component) {
          expandedProp = componentInfoProp.FindPropertyRelative("expanded");
          propertiesProp = componentInfoProp.FindPropertyRelative("bindings");
          break;
        }
      }

      if (expandedProp == null || propertiesProp == null) {
        Debug.LogError("Should always be able to find a property for a component.");
        continue;
      }

      EditorGUILayout.BeginHorizontal();
      expandedProp.boolValue = EditorGUILayout.Foldout(expandedProp.boolValue, component.GetType().Name);

      bool recordAll = false;
      if (GUILayout.Button("Record All")) {
        recordAll = true;
      }

      bool clearAll = false;
      if (GUILayout.Button("Clear All")) {
        clearAll = true;
      }

      EditorGUILayout.EndHorizontal();

      EditorGUI.indentLevel++;

      var allBindings = AnimationUtility.GetAnimatableBindings(target.gameObject, target.gameObject);
      foreach (var binding in allBindings) {
        if (binding.type != component.GetType()) {
          continue;
        }

        var bindingName = ObjectNames.NicifyVariableName(binding.propertyName);
        var bindingProperty = binding.propertyName;

        int isRecordedIndex = -1;
        for (int i = 0; i < propertiesProp.arraySize; i++) {
          if (propertiesProp.GetArrayElementAtIndex(i).stringValue == bindingProperty) {
            isRecordedIndex = i;
            break;
          }
        }

        bool isRecorded = isRecordedIndex >= 0;
        bool shouldBeRecorded = isRecorded;

        if (expandedProp.boolValue) {
          shouldBeRecorded = EditorGUILayout.ToggleLeft(bindingName, isRecorded);
        }

        if (recordAll) {
          shouldBeRecorded = true;
        }

        if (clearAll) {
          shouldBeRecorded = false;
        }

        if (shouldBeRecorded && !isRecorded) {
          propertiesProp.InsertArrayElementAtIndex(propertiesProp.arraySize);
          propertiesProp.GetArrayElementAtIndex(propertiesProp.arraySize - 1).stringValue = bindingProperty;
        } else if (!shouldBeRecorded && isRecorded) {
          int arraySizeBefore = propertiesProp.arraySize;
          propertiesProp.DeleteArrayElementAtIndex(isRecordedIndex);
          if (arraySizeBefore == propertiesProp.arraySize) {
            propertiesProp.DeleteArrayElementAtIndex(isRecordedIndex);
          }
        }
      }

      EditorGUI.indentLevel--;
    }
  }



}
