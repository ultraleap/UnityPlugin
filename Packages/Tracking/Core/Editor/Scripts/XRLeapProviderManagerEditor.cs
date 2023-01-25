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
    [CustomEditor(typeof(XRLeapProviderManager))]
    public class XRLeapProviderManagerEditor : CustomEditorBase<XRLeapProviderManager>
    {
        SerializedProperty serviceProviderProp;
        SerializedProperty openXRProviderProp;
        SerializedProperty trackingSourceProp;

        protected override void OnEnable()
        {
            base.OnEnable();

            serviceProviderProp = serializedObject.FindProperty("leapXRServiceProvider");
            openXRProviderProp = serializedObject.FindProperty("openXRLeapProvider");
            trackingSourceProp = serializedObject.FindProperty("trackingSource");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(trackingSourceProp);

            EditorGUILayout.Space();

            if (Application.isPlaying)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.PropertyField(serviceProviderProp);
                EditorGUILayout.PropertyField(openXRProviderProp);
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                EditorGUILayout.PropertyField(serviceProviderProp);
                EditorGUILayout.PropertyField(openXRProviderProp);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}