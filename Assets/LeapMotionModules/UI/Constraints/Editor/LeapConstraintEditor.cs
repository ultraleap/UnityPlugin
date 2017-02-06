using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections;

namespace Leap.Unity.UI.Constraints {

  [CustomEditor(typeof(LeapConstraint))]
  public class LeapConstraintEditor : CustomEditorBase {

    private ReorderableList list;
    protected override void OnEnable() {
      base.OnEnable();
      list = new ReorderableList(serializedObject,
                                 serializedObject.FindProperty("constraints"),
                                 true, true, true, true);
      list.drawHeaderCallback = drawHeader;
      list.drawElementCallback = drawElement;
      list.elementHeight = EditorGUIUtility.singleLineHeight * 4;
      specifyCustomDrawer("constraints", doLayoutList);
    }

    private void doLayoutList(SerializedProperty p) {
      list.DoLayoutList();
    }

    private void drawHeader(Rect rect) {
      EditorGUI.LabelField(rect, "Local-Space Constraints");
    }

    private void drawElement(Rect rect, int index, bool isActive, bool isFocused) {
      var element = list.serializedProperty.GetArrayElementAtIndex(index);
      int typeIndex = element.FindPropertyRelative("type").enumValueIndex;
      Rect r = rect;
      r.height /= 4;

      EditorGUI.PropertyField(r, element.FindPropertyRelative("type"));
      r.y += EditorGUIUtility.singleLineHeight;

      if (typeIndex == (int)LeapConstraint.ConstraintType.Plane
       || typeIndex == (int)LeapConstraint.ConstraintType.Sphere
       || typeIndex == (int)LeapConstraint.ConstraintType.Box
       || typeIndex == (int)LeapConstraint.ConstraintType.Clip) {
        EditorGUI.PropertyField(r, element.FindPropertyRelative("center"));
        r.y += EditorGUIUtility.singleLineHeight;
      }

      if (typeIndex == (int)LeapConstraint.ConstraintType.Plane
       || typeIndex == (int)LeapConstraint.ConstraintType.Clip) {
        EditorGUI.PropertyField(r, element.FindPropertyRelative("normal"));
        r.y += EditorGUIUtility.singleLineHeight;
      }

      if (typeIndex == (int)LeapConstraint.ConstraintType.Sphere
       || typeIndex == (int)LeapConstraint.ConstraintType.Capsule) {
        EditorGUI.PropertyField(r, element.FindPropertyRelative("radius"));
        r.y += EditorGUIUtility.singleLineHeight;
      }

      if (typeIndex == (int)LeapConstraint.ConstraintType.Box) {
        EditorGUI.PropertyField(r, element.FindPropertyRelative("extents"));
        r.y += EditorGUIUtility.singleLineHeight;
      }

      if (typeIndex == (int)LeapConstraint.ConstraintType.Line
       || typeIndex == (int)LeapConstraint.ConstraintType.Capsule) {
        EditorGUI.PropertyField(r, element.FindPropertyRelative("start"));
        r.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(r, element.FindPropertyRelative("end"));
        r.y += EditorGUIUtility.singleLineHeight;
      }
    }

  }

}