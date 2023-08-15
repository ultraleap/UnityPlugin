using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace Leap.Unity.Interaction.PhysicsHands
{
    [CustomEditor(typeof(PhysicsBone)), CanEditMultipleObjects]
    public class PhysicsBoneEditor : Editor
    {
        SerializedProperty _isBoneHovering;
        SerializedProperty _isBoneContacting;
        SerializedProperty _isBoneGrabbable;
        SerializedProperty _isBoneGrabbing;
        SerializedProperty _objectDistance;
        SerializedProperty _displacementAmount;

        public override void OnInspectorGUI()
        {
            GetProperties();

            GUI.enabled = false;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"), true, new GUILayoutOption[0]);

            GUI.enabled = true;
            EditorGUILayout.LabelField("Bone States", EditorStyles.boldLabel);
            GUI.enabled = false;
            EditorGUILayout.PropertyField(_isBoneHovering);
            EditorGUILayout.PropertyField(_isBoneContacting);
            EditorGUILayout.PropertyField(_isBoneGrabbable);
            EditorGUILayout.PropertyField(_isBoneGrabbing);

            EditorGUILayout.Space();
            GUI.enabled = true;
            EditorGUILayout.LabelField("Bone Values", EditorStyles.boldLabel);
            GUI.enabled = false;
            EditorGUILayout.PropertyField(_objectDistance);
            EditorGUILayout.PropertyField(_displacementAmount);
            GUI.enabled = true;


        }

        private void GetProperties()
        {
            _isBoneHovering = serializedObject.FindProperty("<IsBoneHovering>k__BackingField");
            _isBoneContacting = serializedObject.FindProperty("<IsBoneContacting>k__BackingField");
            _isBoneGrabbable = serializedObject.FindProperty("<IsBoneReadyToGrab>k__BackingField");
            _isBoneGrabbing = serializedObject.FindProperty("<IsBoneGrabbing>k__BackingField");
            _objectDistance = serializedObject.FindProperty("_objectDistance");
            _displacementAmount = serializedObject.FindProperty("<DisplacementAmount>k__BackingField");
        }
    }
}