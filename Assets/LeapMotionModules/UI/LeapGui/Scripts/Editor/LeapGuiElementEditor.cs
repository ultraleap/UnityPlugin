using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Leap.Unity;
using Leap.Unity.Query;

[CanEditMultipleObjects]
[CustomEditor(typeof(LeapGuiElementData), editorForChildClasses: true, isFallback = true)]
public class DefaultFeatureDataEditor : CustomEditorBase {
  protected override void OnEnable() {
    base.OnEnable();
    dontShowScriptField();
  }
}

[CanEditMultipleObjects]
[CustomEditor(typeof(LeapGuiElement))]
public class LeapGuiElementEditor : Editor {

  List<LeapGuiElement> elements = new List<LeapGuiElement>();

  public override void OnInspectorGUI() {
    base.OnInspectorGUI();

    targets.Query().Where(e => e != null).Select(e => e as LeapGuiElement).FillList(elements);

    if (elements.Count == 0) return;

    var gui = elements[0].GetComponentInParent<LeapGui>();
    if (elements.Query().Any(e => e.GetComponentInParent<LeapGui>() != gui)) {
      EditorGUILayout.HelpBox("Cannot edit multiple elements from different gui's.", MessageType.Info);
      return;
    }

    for (int i = 0; i < gui.features.Count; i++) {
      var objs = elements.Query().Select(e => e.data[i]).ToArray();

      var editor = Editor.CreateEditor(objs);

      EditorGUI.BeginChangeCheck();
      EditorGUILayout.LabelField(LeapGuiFeatureNameAttribute.GetFeatureName(gui.features[i].GetType()));
      EditorGUI.indentLevel++;

      editor.OnInspectorGUI();

      EditorGUI.indentLevel--;
      if (EditorGUI.EndChangeCheck()) {
        editor.serializedObject.ApplyModifiedProperties();
      }
    }
  }
}
