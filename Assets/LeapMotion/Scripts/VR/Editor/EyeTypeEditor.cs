using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomPropertyDrawer(typeof(EyeType))]
public class EyeTypeEditor : PropertyDrawer {

  public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
    EditorGUI.PropertyField(position, property.FindPropertyRelative("_orderType"));
  }
}
