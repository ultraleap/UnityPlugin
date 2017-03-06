﻿using UnityEngine;
using UnityEditor;

namespace Leap.Unity{
  [CustomEditor(typeof(LeapImageRetriever))]
  public class LeapImageRetrieverEditor : CustomEditorBase<LeapImageRetriever> {

    private GUIContent _brightTextureGUIContent;
    private GUIContent _rawTextureGUIContent;
    private GUIContent _distortionTextureGUIContent;

    protected override void OnEnable() {
      base.OnEnable();

      _brightTextureGUIContent = new GUIContent("Bright Texture");
      _rawTextureGUIContent = new GUIContent("Raw Texture");
      _distortionTextureGUIContent = new GUIContent("Distortion Texture");
    }

    public override void OnInspectorGUI() {
      base.OnInspectorGUI();

      if (Application.isPlaying) {
        var data = target.TextureData;
        var dataType = typeof(Object);

        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.ObjectField(_brightTextureGUIContent, data.BrightTexture.CombinedTexture, dataType, true);
        EditorGUILayout.ObjectField(_rawTextureGUIContent, data.RawTexture.CombinedTexture, dataType, true);
        EditorGUILayout.ObjectField(_distortionTextureGUIContent, data.Distortion.CombinedTexture, dataType, true);
        EditorGUI.EndDisabledGroup();
      }
    }
  }
}
