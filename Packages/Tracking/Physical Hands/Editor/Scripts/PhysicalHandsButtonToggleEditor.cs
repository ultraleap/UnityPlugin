using UnityEditor;
using UnityEngine;

namespace Leap.PhysicalHands
{
    [CustomEditor(typeof(PhysicalHandsButtonToggle), true)]
    [CanEditMultipleObjects]
    public class PhysicalHandsButtonToggleEditor : PhysicalHandsButtonEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_untoggleWhenPressed"), new GUIContent("Untoggle When Pressed", "When the toggle is toggled, should it be capable of untoggling when pressed? \n\nYou can always untoggle via code or events."));

            serializedObject.ApplyModifiedProperties();
        }
    }
}