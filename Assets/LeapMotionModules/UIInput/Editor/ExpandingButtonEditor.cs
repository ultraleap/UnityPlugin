using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEngine.UI;

namespace UnityEditor.UI
{
    [CustomEditor(typeof(ExpandingButton), true)]
    [CanEditMultipleObjects]
    public class ExpandingButtonEditor : SelectableEditor
    {
        SerializedProperty m_ExpandSpeed;
        SerializedProperty m_ContractSpeed;
        SerializedProperty m_LayerOneFloatDistance;
        SerializedProperty m_LayerTwoFloatDistance;
        SerializedProperty m_LayerOne;
        SerializedProperty m_LayerTwo;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_ExpandSpeed = serializedObject.FindProperty("ExpandSpeed");
            m_ContractSpeed = serializedObject.FindProperty("ContractSpeed");
            m_LayerOneFloatDistance = serializedObject.FindProperty("LayerOneFloatDistance");
            m_LayerTwoFloatDistance = serializedObject.FindProperty("LayerTwoFloatDistance");
            m_LayerOne = serializedObject.FindProperty("LayerOne");
            m_LayerTwo = serializedObject.FindProperty("LayerTwo");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            serializedObject.Update();
            EditorGUILayout.PropertyField(m_ExpandSpeed);
            EditorGUILayout.PropertyField(m_ContractSpeed);
            EditorGUILayout.PropertyField(m_LayerOneFloatDistance);
            EditorGUILayout.PropertyField(m_LayerTwoFloatDistance);
            EditorGUILayout.PropertyField(m_LayerOne);
            EditorGUILayout.PropertyField(m_LayerTwo);
            serializedObject.ApplyModifiedProperties();
        }
    }
}