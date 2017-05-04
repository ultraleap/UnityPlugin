using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Leap.Unity.Query;

namespace Leap.Unity.GraphicalRenderer {

  [CustomPropertyDrawer(typeof(LeapMesherBase), useForChildren: true)]
  public class LeapMesherBaseDrawer : CustomPropertyDrawerBase {
    private const float HEADER_HEADROOM = 8;
    private const float MESH_LABEL_WIDTH = 100.0f;
    private const float REFRESH_BUTTON_WIDTH = 100;

    private SerializedProperty _uv0, _uv1, _uv2, _uv3, _colors, _bakedTint, _normals;

    protected override void init(SerializedProperty property) {
      base.init(property);

      _uv0 = property.FindPropertyRelative("_useUv0");
      _uv1 = property.FindPropertyRelative("_useUv1");
      _uv2 = property.FindPropertyRelative("_useUv2");
      _uv3 = property.FindPropertyRelative("_useUv3");
      _colors = property.FindPropertyRelative("_useColors");
      _bakedTint = property.FindPropertyRelative("_bakedTint");
      _normals = property.FindPropertyRelative("_useNormals");

      drawCustom(rect => {
        rect.y += HEADER_HEADROOM;
        EditorGUI.LabelField(rect, "Mesh Settings", EditorStyles.boldLabel);
      }, HEADER_HEADROOM + EditorGUIUtility.singleLineHeight);

      increaseIndent();

      drawCustom(rect => {
        Rect left, right;
        rect.SplitHorizontally(out left, out right);
        EditorGUI.PropertyField(left, _uv0);
        EditorGUI.PropertyField(right, _uv1);
      }, EditorGUIUtility.singleLineHeight);

      drawCustom(rect => {
        Rect left, right;
        rect.SplitHorizontally(out left, out right);
        EditorGUI.PropertyField(left, _uv2);
        EditorGUI.PropertyField(right, _uv3);
      }, EditorGUIUtility.singleLineHeight);

      drawCustom(rect => {
        Rect left, right;
        rect.SplitHorizontally(out left, out right);
        EditorGUI.PropertyField(left, _colors);
        if (_colors.boolValue) {
          EditorGUI.PropertyField(right, _bakedTint);
        }
      }, EditorGUIUtility.singleLineHeight);

      drawCustom(rect => {
        Rect left, right;
        rect.SplitHorizontally(out left, out right);
        EditorGUI.PropertyField(left, _normals);
      }, EditorGUIUtility.singleLineHeight);

      decreaseIndent();

      drawCustom(rect => {
        rect.y += HEADER_HEADROOM;
        rect.height = EditorGUIUtility.singleLineHeight;
        EditorGUI.LabelField(rect, "Rendering Settings", EditorStyles.boldLabel);
      }, HEADER_HEADROOM + EditorGUIUtility.singleLineHeight);

      increaseIndent();

      drawProperty("_shader");
      drawProperty("_layer");
      drawProperty("_atlas");
    }
  }
}
