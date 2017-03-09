using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Leap.Unity;

[CustomEditor(typeof(LeapGuiMesherBase), editorForChildClasses: true, isFallback = true)]
public class LeapGuiMesherBaseEditor : CustomEditorBase {

  protected override void OnEnable() {
    base.OnEnable();

    createHorizonalSection("_useUv0", "_useUv1");
    createHorizonalSection("_useUv2", "_useUv3");
    createHorizonalSection("_useColors", "_globalTint");

    specifyConditionalDrawing("_useColors", "_globalTint");

    specifyCustomDecorator("_useUv0", meshSettingsHeader);
    specifyCustomDecorator("_shader", renderingSettingsHeader);
  }

  public override void OnInspectorGUI() {
    base.OnInspectorGUI();

    EditorGUI.indentLevel--;
  }

  private void meshSettingsHeader(SerializedProperty property) {
    EditorGUILayout.Space();
    EditorGUILayout.LabelField("Mesh Settings", EditorStyles.boldLabel);
    EditorGUI.indentLevel++;
  }

  private void renderingSettingsHeader(SerializedProperty property) {
    EditorGUI.indentLevel--;
    EditorGUILayout.Space();
    EditorGUILayout.LabelField("Rendering Settings", EditorStyles.boldLabel);
    EditorGUI.indentLevel++;
  }
}
