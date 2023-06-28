using UnityEditor;
using UnityEngine;

namespace Leap.Unity.Interaction.PhysicsHands
{
    [CustomEditor(typeof(PhysicsIgnoreHelpers)), CanEditMultipleObjects]
    public class PhysicsIgnoreHelpersEditor : Editor
    {
        SerializedProperty _disableAllHandCollisions;
        SerializedProperty _disableSpecificHandCollisions;
        SerializedProperty _disabledHandedness;

        public override void OnInspectorGUI()
        {
            GetProperties();

            EditorGUILayout.HelpBox("This script will prevent the physics hands helpers from being applied to your object.\n" +
                "This allows you to easily prevent important objects from being affected by the player.", MessageType.Info);

            GUI.enabled = false;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"), true, new GUILayoutOption[0]);
            GUI.enabled = true;

            GUI.enabled = !_disableSpecificHandCollisions.boolValue;
            EditorGUILayout.PropertyField(_disableAllHandCollisions);
            GUI.enabled = true;

            GUI.enabled = !_disableAllHandCollisions.boolValue;
            EditorGUILayout.PropertyField(_disableSpecificHandCollisions);
            if (_disableSpecificHandCollisions.boolValue)
            {
                EditorGUILayout.PropertyField(_disabledHandedness);
            }
            GUI.enabled = true;
            serializedObject.ApplyModifiedProperties();
        }

        private void GetProperties()
        {
            _disableAllHandCollisions = serializedObject.FindProperty("DisableAllHandCollisions");
            _disableSpecificHandCollisions = serializedObject.FindProperty("DisableSpecificHandCollisions");
            _disabledHandedness = serializedObject.FindProperty("DisabledHandedness");
        }
    }
}