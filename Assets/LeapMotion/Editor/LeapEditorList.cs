using System.Collections;
using UnityEditor;
using UnityEngine;

public static class LeapEditorList {
	
	public static void Show (SerializedProperty list) {
		EditorGUILayout.PropertyField(list);
    EditorGUI.indentLevel += 1;
    if (list.isExpanded) {
      EditorGUILayout.PropertyField(list.FindPropertyRelative("Array.size"));
      for (int i = 0; i < list.arraySize; i++) {
        EditorGUILayout.LabelField("Model Pair: " + i);
        EditorGUI.indentLevel += 1;
        EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(i), GUIContent.none,true);
        EditorGUI.indentLevel -= 1;
        EditorGUILayout.Space();
      }
    }
    EditorGUI.indentLevel -= 1;

	}
}