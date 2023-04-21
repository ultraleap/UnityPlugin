/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEditor;
using UnityEngine;

namespace Leap.Unity.Interaction
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(GrabBall))]
    public class GrabBallEditor : CustomEditorBase<GrabBall>
    {
        SerializedProperty _useAttachedObjectsXRotation;
        SerializedProperty _xRotation;
        SerializedProperty _restrictGrabBallDistanceFromHead;
        SerializedProperty _maxHorizontalDistanceFromHead;
        SerializedProperty _minHorizontalDistanceFromHead;
        SerializedProperty _maxHeightFromHead;
        SerializedProperty _minHeightFromHead;
        SerializedProperty _drawGrabBallRestrictionGizmos;
        SerializedProperty _attachedObject;
        SerializedProperty _grabBallInteractionBehaviour;
        SerializedProperty _lerpSpeed;

        SerializedProperty _continuouslyRestrictGrabBallDistanceFromHead;

        public override void OnInspectorGUI()
        {
            GetProperties();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((MonoBehaviour)target), GetType(), false);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.LabelField("Setup", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_attachedObject);
            EditorGUILayout.PropertyField(_grabBallInteractionBehaviour);
            EditorGUILayout.PropertyField(_lerpSpeed);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("X Rotation", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_useAttachedObjectsXRotation);

            if (_useAttachedObjectsXRotation.boolValue)
            {
                if (target.attachedObject == null)
                {
                    target.xRotation = 0;
                }
                else
                {
                    target.xRotation = target.attachedObject.rotation.eulerAngles.x;
                }
            }

            EditorGUI.BeginDisabledGroup(_useAttachedObjectsXRotation.boolValue);
            EditorGUILayout.PropertyField(_xRotation);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Grab Ball Restriction", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_restrictGrabBallDistanceFromHead);
            if (_restrictGrabBallDistanceFromHead.boolValue)
            {
                GUILayout.Space(2.5f);
                EditorGUILayout.PropertyField(_maxHorizontalDistanceFromHead);
                EditorGUILayout.PropertyField(_minHorizontalDistanceFromHead);
                EditorGUILayout.PropertyField(_maxHeightFromHead);
                EditorGUILayout.PropertyField(_minHeightFromHead);
                GUILayout.Space(2.5f);
                EditorGUILayout.PropertyField(_drawGrabBallRestrictionGizmos);

                EditorGUILayout.PropertyField(_continuouslyRestrictGrabBallDistanceFromHead);
            }
            EditorGUILayout.Space();

            serializedObject.ApplyModifiedProperties();
        }

        private void GetProperties()
        {
            _attachedObject = serializedObject.FindProperty("attachedObject");
            _grabBallInteractionBehaviour = serializedObject.FindProperty("grabBallInteractionBehaviour");
            _lerpSpeed = serializedObject.FindProperty("lerpSpeed");
            _useAttachedObjectsXRotation = serializedObject.FindProperty("useAttachedObjectsXRotation");
            _xRotation = serializedObject.FindProperty("xRotation");
            _restrictGrabBallDistanceFromHead = serializedObject.FindProperty("restrictGrabBallDistanceFromHead");
            _maxHorizontalDistanceFromHead = serializedObject.FindProperty("maxHorizontalDistanceFromHead");
            _minHorizontalDistanceFromHead = serializedObject.FindProperty("minHorizontalDistanceFromHead");
            _maxHeightFromHead = serializedObject.FindProperty("maxHeightFromHead");
            _minHeightFromHead = serializedObject.FindProperty("minHeightFromHead");
            _drawGrabBallRestrictionGizmos = serializedObject.FindProperty("drawGrabBallRestrictionGizmos");
            _continuouslyRestrictGrabBallDistanceFromHead = serializedObject.FindProperty("continuouslyRestrictGrabBallDistanceFromHead");
        }
    }
}