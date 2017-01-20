using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.AnimatedValues;

namespace Leap.Unity.Gui.Space {

  [CustomEditor(typeof(GuiMeshBaker))]
  public class GuiMeshBakerEditor : Editor {

    SerializedProperty textureChannels;
    SerializedProperty texturePropertyNames;
    SerializedProperty atlasSettings;

    SerializedProperty enableVertexColors;
    SerializedProperty bakedTint;
    AnimBool animVertexColors;

    SerializedProperty enableElementMotion;
    SerializedProperty motionType;
    AnimBool animElementMotion;

    SerializedProperty enableTinting;

    SerializedProperty enableBlendShapes;
    SerializedProperty blendShapeSpace;
    AnimBool animBlendShapes;

    void OnEnable() {
      textureChannels = serializedObject.FindProperty("_textureChannels");
      texturePropertyNames = serializedObject.FindProperty("_texturePropertyNames");
      atlasSettings = serializedObject.FindProperty("_atlasSettings");

      enableVertexColors = serializedObject.FindProperty("_enableVertexColors");
      bakedTint = serializedObject.FindProperty("_bakedTint");
      animVertexColors = createAnimBool(enableVertexColors.boolValue);

      enableElementMotion = serializedObject.FindProperty("_enableElementMotion");
      motionType = serializedObject.FindProperty("_motionType");
      animElementMotion = createAnimBool(enableElementMotion.boolValue);

      enableTinting = serializedObject.FindProperty("_enableTinting");

      enableBlendShapes = serializedObject.FindProperty("_enableBlendShapes");
      blendShapeSpace = serializedObject.FindProperty("_blendShapeSpace");
      animBlendShapes = createAnimBool(enableBlendShapes.boolValue);
    }
    public override void OnInspectorGUI() {
      drawTextureChannelInfo();

      drawVertexColorInfo();

      drawElementMotionInfo();

      EditorGUILayout.PropertyField(enableTinting);

      drawBlendShapeInfo();

      serializedObject.ApplyModifiedProperties();
    }

    private void drawTextureChannelInfo() {
      EditorGUILayout.PropertyField(textureChannels);
      EditorGUI.indentLevel++;

      //Make sure array is large enough to hold all the names
      while (texturePropertyNames.arraySize < textureChannels.intValue) {
        texturePropertyNames.InsertArrayElementAtIndex(texturePropertyNames.arraySize);
      }

      for (int i = 0; i < textureChannels.intValue; i++) {
        SerializedProperty propertyName = texturePropertyNames.GetArrayElementAtIndex(i);

        if (i == 0) {
          propertyName.stringValue = "_MainTex";
          EditorGUI.BeginDisabledGroup(true);
        }

        if (string.IsNullOrEmpty(propertyName.stringValue)) {
          propertyName.stringValue = "_Texture" + i;
        }

        EditorGUILayout.PropertyField(propertyName);

        if (i == 0) {
          EditorGUI.EndDisabledGroup();
        }
      }

      if (textureChannels.intValue > 0) {
        EditorGUILayout.PropertyField(atlasSettings, includeChildren: true);
      }
      EditorGUI.indentLevel--;
    }

    private void drawVertexColorInfo() {
      EditorGUILayout.PropertyField(enableVertexColors);
      animVertexColors.target = enableVertexColors.boolValue;

      if (EditorGUILayout.BeginFadeGroup(animVertexColors.faded)) {
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(bakedTint);
        EditorGUI.indentLevel--;
      }
      EditorGUILayout.EndFadeGroup();
    }

    private void drawElementMotionInfo() {
      EditorGUILayout.PropertyField(enableElementMotion);
      animElementMotion.target = enableElementMotion.boolValue;

      if (EditorGUILayout.BeginFadeGroup(animElementMotion.faded)) {
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(motionType);
        EditorGUI.indentLevel--;
      }
      EditorGUILayout.EndFadeGroup();
    }

    private void drawBlendShapeInfo() {
      EditorGUILayout.PropertyField(enableBlendShapes);
      animBlendShapes.target = enableBlendShapes.boolValue;

      if (EditorGUILayout.BeginFadeGroup(animBlendShapes.faded)) {
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(blendShapeSpace);
        EditorGUI.indentLevel--;
      }
      EditorGUILayout.EndFadeGroup();
    }

    private AnimBool createAnimBool(bool enabled) {
      var animBool = new AnimBool(enabled);
      animBool.valueChanged.AddListener(Repaint);
      animBool.speed = 5;
      return animBool;
    }
  }
}
