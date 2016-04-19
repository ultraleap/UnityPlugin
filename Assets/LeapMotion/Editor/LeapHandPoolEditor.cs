using UnityEngine;
using UnityEditor;
using System.Collections;

namespace Leap.Unity {
  [CustomEditor(typeof(HandPool))]
  public class LeapHandPoolEditor : CustomEditorBase {

    protected override void OnEnable() {
      base.OnEnable();

      specifyCustomDrawer("ModelCollection", show);
      specifyCustomDrawer("ModelPool", showModelPool);
    }

    private void showModelPool(SerializedProperty modelPool) {
      if (Application.isPlaying) {
        EditorGUILayout.PropertyField(modelPool, true);
      }
    }

    private void show(SerializedProperty list) {
      EditorGUILayout.PropertyField(list);
      EditorGUI.indentLevel += 1;
      if (list.isExpanded) {
        EditorGUILayout.PropertyField(list.FindPropertyRelative("Array.size"));
        for (int i = 0; i < list.arraySize; i++) {
          EditorGUI.indentLevel += 2;
          EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(i), new GUIContent("Model Pair: " + i), true);
          EditorGUI.indentLevel -= 2;
          EditorGUILayout.Space();
        }
      }
      EditorGUI.indentLevel -= 1;
    }


  }
}
