using System.Collections;
using UnityEditor;
using UnityEngine;

public static class LeapModelPairList {
	
	public static void Show (SerializedProperty list) {
		EditorGUILayout.PropertyField(list);
		EditorGUI.indentLevel += 1;
		if (list.isExpanded) {
			EditorGUILayout.PropertyField(list.FindPropertyRelative("Array.size"));
			for (int i = 0; i < list.arraySize; i++) {
				EditorGUI.indentLevel += 2;
				EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(i), new GUIContent("Model Pair: " + i),true);
				EditorGUI.indentLevel -= 2;
				EditorGUILayout.Space();
			}
		}
		EditorGUI.indentLevel -= 1;

	}
}