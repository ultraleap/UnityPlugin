/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

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

    private static float SHADER_WARNING_HEIGHT = 40;
    private static float SHADER_WARNING_FIX_WIDTH = 50;

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

      SerializedProperty shaderProp;
      tryGetProperty("_shader", out shaderProp);

      drawCustom(rect => {
        if (!shaderProp.hasMultipleDifferentValues) {
          Shader shader = shaderProp.objectReferenceValue as Shader;
          if (VariantEnabler.DoesShaderHaveVariantsDisabled(shader)) {
            Rect left, right;
            rect.SplitHorizontallyWithRight(out left, out right, SHADER_WARNING_FIX_WIDTH);

            EditorGUI.HelpBox(left, "This shader has disabled variants, and will not function until they are enabled.", MessageType.Warning);
            if (GUI.Button(right, "Enable")) {
              if (EditorUtility.DisplayDialog("Import Time Warning!", "The import time for this surface shader can take up to 2-3 minutes!  Are you sure you want to enable?", "Enable", "Not right now")) {
                VariantEnabler.SetShaderVariantsEnabled(shader, enable: true);
                AssetDatabase.Refresh();
              }
            }
          }
        }
      },
      () => {
        Shader shader = shaderProp.objectReferenceValue as Shader;
        if (VariantEnabler.DoesShaderHaveVariantsDisabled(shader)) {
          return SHADER_WARNING_HEIGHT;
        } else {
          return 0;
        }
      });

      drawProperty("_shader");
      drawProperty("_visualLayer", includeChildren: true, disable: EditorApplication.isPlaying);
      drawProperty("_atlas", includeChildren: true, disable: EditorApplication.isPlaying);
    }
  }
}
