using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Leap.Unity;

[CustomEditor(typeof(LeapGuiElement))]
public class LeapGuiElementEditor : CustomEditorBase {

  void OnEnable() {
    base.OnEnable();

    specifyCustomDrawer("data", drawData);
  }

  private void drawData(SerializedProperty property) {
    for (int i = 0; i < property.arraySize; i++) {
      var dataRef = property.GetArrayElementAtIndex(i);

      var dataObj = dataRef.objectReferenceValue;
      EditorGUILayout.LabelField(LeapGuiFeatureNameAttribute.GetFeatureName((dataObj as LeapGuiElementData).feature.GetType()));
      EditorGUI.indentLevel++;

      EditorGUI.BeginChangeCheck();

      SerializedObject sobj = new SerializedObject(dataObj);
      SerializedProperty it = sobj.GetIterator();
      it.NextVisible(true);

      while (it.NextVisible(false)) {
        EditorGUILayout.PropertyField(it);
      }

      EditorGUI.indentLevel--;

      if (EditorGUI.EndChangeCheck()) {
        sobj.ApplyModifiedProperties();
        (dataObj as LeapGuiElementData).feature.isDirty = true;
      }
    }
  }
}
