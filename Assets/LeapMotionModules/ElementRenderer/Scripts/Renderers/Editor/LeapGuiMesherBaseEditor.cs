using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Leap.Unity;
using Leap.Unity.Query;

[CustomEditor(typeof(LeapGuiMesherBase), editorForChildClasses: true, isFallback = true)]
public class LeapGuiMesherBaseEditor : CustomEditorBase {
  private const float MESH_LABEL_WIDTH = 100.0f;
  private SerializedProperty _uv0, _uv1, _uv2, _uv3, _colors, _globalTint, _normals;

  protected override void OnEnable() {
    base.OnEnable();

    _uv0 = serializedObject.FindProperty("_useUv0");
    _uv1 = serializedObject.FindProperty("_useUv1");
    _uv2 = serializedObject.FindProperty("_useUv2");
    _uv3 = serializedObject.FindProperty("_useUv3");
    _colors = serializedObject.FindProperty("_useColors");
    _globalTint = serializedObject.FindProperty("_globalTint");
    _normals = serializedObject.FindProperty("_useNormals");

    specifyCustomDecorator("_shader", drawMeshSettings);
    specifyConditionalDrawing("_useColors", "_globalTint");
    specifyConditionalDrawing(hasTextureFeature, "_atlas");

    specifyCustomDecorator("_shader", renderingSettingsHeader);
  }

  public override void OnInspectorGUI() {
    base.OnInspectorGUI();

    EditorGUI.indentLevel--;
  }

  private void drawMeshSettings(SerializedProperty property) {
    float defaultLabelWidth = EditorGUIUtility.labelWidth;
    EditorGUIUtility.labelWidth = MESH_LABEL_WIDTH;

    EditorGUILayout.Space();
    EditorGUILayout.LabelField("Mesh Settings", EditorStyles.boldLabel);
    EditorGUI.indentLevel++;

    Rect left, right;

    Rect r0 = EditorGUILayout.GetControlRect();

    r0.SplitHorizontally(out left, out right);
    EditorGUI.PropertyField(left, _uv0);
    EditorGUI.PropertyField(right, _uv1);

    Rect r1 = EditorGUILayout.GetControlRect();
    r1.SplitHorizontally(out left, out right);
    EditorGUI.PropertyField(left, _uv2);
    EditorGUI.PropertyField(right, _uv3);

    Rect r2 = EditorGUILayout.GetControlRect();
    r2.SplitHorizontally(out left, out right);
    EditorGUI.PropertyField(left, _colors);
    if (_colors.boolValue) {
      EditorGUI.PropertyField(right, _globalTint);
    }

    Rect r3 = EditorGUILayout.GetControlRect();
    r3.SplitHorizontally(out left, out right);
    EditorGUI.PropertyField(left, _normals);

    EditorGUIUtility.labelWidth = defaultLabelWidth;
  }

  private void renderingSettingsHeader(SerializedProperty property) {
    EditorGUI.indentLevel--;
    EditorGUILayout.Space();

    using (new EditorGUILayout.HorizontalScope()) {
      EditorGUILayout.LabelField("Rendering Settings", EditorStyles.boldLabel);

      if (hasTextureFeature()) {
        var mesher = target as LeapGuiMesherBase;
        if (mesher.IsAtlasDirty) {
          GUI.color = Color.yellow;
        }

        if (GUILayout.Button("Refresh Atlas")) {
          try {
            mesher.RebuildAtlas(new ProgressBar());
            mesher.gui.ScheduleEditorUpdate();
          } finally {
            EditorUtility.ClearProgressBar();
          }
        }

        GUI.color = Color.white;
      }
    }

    EditorGUI.indentLevel++;
  }

  private bool hasTextureFeature() {
    var mesher = target as LeapGuiMesherBase;
    return mesher.group.features.Query().OfType<LeapGuiTextureFeature>().Any();
  }
}
