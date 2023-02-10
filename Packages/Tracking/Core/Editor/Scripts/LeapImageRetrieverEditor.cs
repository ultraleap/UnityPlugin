/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEditor;
using UnityEngine;

namespace Leap.Unity
{
    [CustomEditor(typeof(LeapImageRetriever))]
    public class LeapImageRetrieverEditor : CustomEditorBase<LeapImageRetriever>
    {

        private GUIContent _textureGUIContent;
        private GUIContent _distortionTextureGUIContent;

        protected override void OnEnable()
        {
            base.OnEnable();

            _textureGUIContent = new GUIContent("Sensor Texture");
            _distortionTextureGUIContent = new GUIContent("Distortion Texture");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (Application.isPlaying)
            {
                var data = target.TextureData;
                var dataType = typeof(Object);

                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField(_textureGUIContent, data.TextureData.CombinedTexture, dataType, true);
                EditorGUILayout.ObjectField(_distortionTextureGUIContent, data.Distortion.CombinedTexture, dataType, true);
                EditorGUI.EndDisabledGroup();
            }
        }
    }
}