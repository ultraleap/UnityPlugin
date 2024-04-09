using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Leap.Unity.PhysicalHands
{
    [CustomEditor(typeof(PhysicalHandsButtonBase), true)]
    [CanEditMultipleObjects]
    public class PhysicalHandsButtonEditor : CustomEditorBase<PhysicalHandsButtonBase>
    {
        private bool eventsFoldedOut = false;

        public override void OnInspectorGUI()
        {
            target.UpdateDistanceValues();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_pressableObject"), new GUIContent("Pressable Object", "The GameObject representing the button that can be pressed."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_automaticTravelDistance"), new GUIContent("Use Automatic Travel Distance", "Travel distance should be calculated based on how far the pressable object is from this object"));

            EditorGUI.indentLevel = 1;
            if (!serializedObject.FindProperty("_automaticTravelDistance").boolValue)
            {
                target.UpdateDistanceValues();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_buttonTravelDistance"), new GUIContent("Button Travel Distance", "The distance the button can travel when pressed."));
                target.UpdateDistanceValues();
            }
            EditorGUI.indentLevel = 0;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_automaticOffsetDistance"), new GUIContent("Use Automatic Distance Offset", "Offset distance should be calculated based on the size of the pressable object and this objects mesh filters."));

            EditorGUI.indentLevel = 1;
            if (!serializedObject.FindProperty("_automaticOffsetDistance").boolValue)
            {
                target.UpdateDistanceValues();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_buttonTravelOffset"), new GUIContent("Button Travel Offset", "Calculated offset based on the bounds of the pressable object and this object"));
                target.UpdateDistanceValues();
            }

            EditorGUI.indentLevel = 0;
  
            EditorGUILayout.Space(5);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_canBePressedByObjects"), new GUIContent("Can Be Pressed By Objects", "Determines whether the button can be pressed by objects which are not the hand."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_whichHandCanPressButton"), new GUIContent("Which Hand Can Activate Button Presses", "Specifies which hand(s) can press the button."));
            EditorGUILayout.Space(5);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_freezeButtonTravelOnMovement"), new GUIContent("Freeze Button Travel If Moving", "Freeze button travel when base object is moving."));
            if (serializedObject.FindProperty("_freezeButtonTravelOnMovement").boolValue)
            {
                EditorGUI.indentLevel = 1;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_buttonVelocityThreshold"), new GUIContent("Button Movement Velocity Allowance", "How fast can the button move before we consider it moving for the sake of freezing button travel on movement"));
                EditorGUI.indentLevel = 0;
            }

            EditorGUILayout.Space(5);
            // Events
            eventsFoldedOut = EditorGUILayout.BeginFoldoutHeaderGroup(eventsFoldedOut, "Button Events");

            if (eventsFoldedOut)
            {
                EditorGUILayout.LabelField("Button Press", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("OnButtonPressed"), new GUIContent("Button Pressed Event", "Event triggered when the button is pressed."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("OnButtonUnPressed"), new GUIContent("Button UnPressed Event", "Event triggered when the slider button is un-pressed."));
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Button Contact", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("OnHandContact"), new GUIContent("Hand Contact Event", "Event triggered when a physical hand contacts the button."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("OnHandContactExit"), new GUIContent("Hand Leave Contact Event", "Event triggered when a physical hand stops contacting the button."));
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Button Hover", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("OnHandHover"), new GUIContent("Hand Hover Event", "Event triggered when a physical hand hovers the button."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("OnHandHoverExit"), new GUIContent("Hand Leave Hover Event", "Event triggered when a physical hand stops hoverring the button."));
                EditorGUILayout.Space(3);
            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            serializedObject.ApplyModifiedProperties();

        }
    }

    [CustomEditor(typeof(PhysicalHandsButtonToggle), true)]
    public class PhysicalHandsButtonToggleEditor : PhysicalHandsButtonEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

        }
    }
}
