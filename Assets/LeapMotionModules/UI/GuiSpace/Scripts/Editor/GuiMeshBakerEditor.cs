using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.AnimatedValues;

namespace Leap.Unity.Gui.Space {

  [CustomEditor(typeof(GuiMeshBaker))]
  public class GuiMeshBakerEditor : Editor {

    SerializedProperty shader;
    SerializedProperty space;

    SerializedProperty textureChannels;
    SerializedProperty atlasSettings;

    SerializedProperty enableVertexNormals;

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
      shader = serializedObject.FindProperty("_shader");

      space = serializedObject.FindProperty("_space");
      textureChannels = serializedObject.FindProperty("_textureChannels");
      atlasSettings = serializedObject.FindProperty("_atlasSettings");

      enableVertexNormals = serializedObject.FindProperty("_enableVertexNormals");

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
      EditorGUILayout.PropertyField(shader);

      EditorGUILayout.PropertyField(space);

      drawTextureChannelInfo();

      EditorGUILayout.PropertyField(enableVertexNormals);

      drawVertexColorInfo();

      drawElementMotionInfo();

      EditorGUILayout.PropertyField(enableTinting);

      drawBlendShapeInfo();

      serializedObject.ApplyModifiedProperties();
    }

    private void drawTextureChannelInfo() {
      int newSize = EditorGUILayout.IntField("Texture Channels", textureChannels.arraySize);
      if (newSize < 0) newSize = 0;

      while (textureChannels.arraySize > newSize) {
        textureChannels.DeleteArrayElementAtIndex(textureChannels.arraySize - 1);
      }

      while (textureChannels.arraySize < newSize) {
        textureChannels.InsertArrayElementAtIndex(textureChannels.arraySize);
      }

      EditorGUI.indentLevel++;

      for (int i = 0; i < textureChannels.arraySize; i++) {
        SerializedProperty element = textureChannels.GetArrayElementAtIndex(i);
        SerializedProperty propertyName = element.FindPropertyRelative("propertyName");
        SerializedProperty channel = element.FindPropertyRelative("channel");

        if (string.IsNullOrEmpty(propertyName.stringValue)) {
          propertyName.stringValue = "_MainTex";
        }

        if (channel.intValue == 0) {
          channel.intValue = (int)GuiMeshBaker.UVChannel.UV0;
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(propertyName, GUIContent.none);
        EditorGUILayout.PropertyField(channel, GUIContent.none);
        EditorGUILayout.EndHorizontal();
      }

      if (textureChannels.arraySize > 0) {
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
