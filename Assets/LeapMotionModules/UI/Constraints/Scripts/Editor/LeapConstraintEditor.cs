using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections;

namespace Leap.Unity.UI.Constraints {

  public class StackableConstraintsEditor : CustomEditorBase {

    private ReorderableList list;
    protected override void OnEnable() {
      base.OnEnable();
      list = new ReorderableList(serializedObject,
                                 serializedObject.FindProperty("Constraints"),
                                 true, true, true, true);
      list.drawHeaderCallback = drawHeader;
      list.drawElementCallback = drawElement;
      list.elementHeight = EditorGUIUtility.singleLineHeight * 4;
      specifyCustomDrawer("Constraints", doLayoutList);
    }

    private void doLayoutList(SerializedProperty p) {
      list.DoLayoutList();
    }

    private void drawHeader(Rect rect) {
      EditorGUI.LabelField(rect, "Local-Space Constraints");
    }

    private void drawElement(Rect rect, int index, bool isActive, bool isFocused) {
      var element = list.serializedProperty.GetArrayElementAtIndex(index);
      int typeIndex = element.FindPropertyRelative("Type").enumValueIndex;
      Rect r = rect;
      r.height /= 4;

      EditorGUI.PropertyField(r, element.FindPropertyRelative("Type"));
      r.y += EditorGUIUtility.singleLineHeight;

      if (typeIndex == 1 || typeIndex == 2 || typeIndex == 3 || typeIndex == 5) {
        EditorGUI.PropertyField(r, element.FindPropertyRelative("Center"));
        r.y += EditorGUIUtility.singleLineHeight;
      }

      if (typeIndex == 1 || typeIndex == 5) {
        EditorGUI.PropertyField(r, element.FindPropertyRelative("Normal"));
        r.y += EditorGUIUtility.singleLineHeight;
      }

      if (typeIndex == 2 || typeIndex == 4) {
        EditorGUI.PropertyField(r, element.FindPropertyRelative("Radius"));
        r.y += EditorGUIUtility.singleLineHeight;
      }

      if (typeIndex == 3) {
        EditorGUI.PropertyField(r, element.FindPropertyRelative("Extents"));
        r.y += EditorGUIUtility.singleLineHeight;
      }

      if (typeIndex == 0 || typeIndex == 4) {
        EditorGUI.PropertyField(r, element.FindPropertyRelative("Start"));
        r.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(r, element.FindPropertyRelative("End"));
        r.y += EditorGUIUtility.singleLineHeight;
      }
    }

  }

}