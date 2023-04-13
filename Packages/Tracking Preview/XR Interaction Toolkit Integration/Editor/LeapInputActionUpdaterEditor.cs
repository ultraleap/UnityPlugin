/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEditor;
using UnityEngine;

namespace Leap.Unity.Preview.InputActions
{
    [CustomEditor(typeof(LeapInputActionUpdater))]
    public class LeapInputActionUpdaterEditor : CustomEditorBase<LeapInputActionUpdater>
    {
        SerializedProperty inputLeapProviderProp;

        SerializedProperty directionSourceProp;
        SerializedProperty isPinchingIsBinaryProp;
        SerializedProperty isGrabbingIsBinaryProp;

        SerializedProperty resetInteractionsProp;

        protected override void OnEnable()
        {
            base.OnEnable();

            inputLeapProviderProp = serializedObject.FindProperty("_inputLeapProvider");

            directionSourceProp = serializedObject.FindProperty("directionSource");
            isPinchingIsBinaryProp = serializedObject.FindProperty("isPinchingIsBinary");
            isGrabbingIsBinaryProp = serializedObject.FindProperty("isGrabbingIsBinary");

            resetInteractionsProp = serializedObject.FindProperty("resetInteractionsOnTrackingLost");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((MonoBehaviour)target), GetType(), false);
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(inputLeapProviderProp);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(directionSourceProp);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(isPinchingIsBinaryProp);
            EditorGUILayout.PropertyField(isGrabbingIsBinaryProp);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(resetInteractionsProp);

            serializedObject.ApplyModifiedProperties();
        }
    }
}