using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(LeapImageRetriever))]
public class LeapImageRetrieverEditor : Editor {

  private GUIContent _brightTextureGUIContent;
  private GUIContent _rawTextureGUIContent;
  private GUIContent _distortionTextureGUIContent;

  void OnEnable() {
    _brightTextureGUIContent = new GUIContent("Bright Texture");
    _rawTextureGUIContent = new GUIContent("Raw Texture");
    _distortionTextureGUIContent = new GUIContent("Distortion Texture");
  }

  public override void OnInspectorGUI() {
    serializedObject.Update();
    SerializedProperty properties = serializedObject.GetIterator();

    bool useEnterChildren = true;
    while (properties.NextVisible(useEnterChildren) == true) {
      useEnterChildren = false;
      EditorGUILayout.PropertyField(properties, true);
    }

    if (Application.isPlaying) {
      LeapImageRetriever retriever = target as LeapImageRetriever;
      var data = retriever.TextureData;
      var dataType = typeof(Object);

      EditorGUI.BeginDisabledGroup(true);
      EditorGUILayout.ObjectField(_brightTextureGUIContent, data.BrightTexture.CombinedTexture, dataType, true);
      EditorGUILayout.ObjectField(_rawTextureGUIContent, data.RawTexture.CombinedTexture, dataType, true);
      EditorGUILayout.ObjectField(_distortionTextureGUIContent, data.Distortion.CombinedTexture, dataType, true);
      EditorGUI.EndDisabledGroup();
    }

    serializedObject.ApplyModifiedProperties();
  }

}
