using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LeapGuiElement))]
public class LeapGuiElementEditor : Editor {

  SerializedProperty data;

  void OnEnable() {
    data = serializedObject.FindProperty("data");
  }

  public override void OnInspectorGUI() {
    base.OnInspectorGUI();

    for (int i = 0; i < data.arraySize; i++) {
      var dataRef = data.GetArrayElementAtIndex(i);

      var dataObj = dataRef.objectReferenceValue;
      EditorGUILayout.LabelField(dataObj.GetType().Name);
      EditorGUI.indentLevel++;

      SerializedObject sobj = new SerializedObject(dataObj);
      SerializedProperty it = sobj.GetIterator();
      it.NextVisible(true);

      while (it.NextVisible(false)) {
        EditorGUILayout.PropertyField(it);
      }

      EditorGUI.indentLevel--;
    }
  }
}
