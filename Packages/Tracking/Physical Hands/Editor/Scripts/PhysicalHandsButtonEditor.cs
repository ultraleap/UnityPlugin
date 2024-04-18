using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Leap.Unity.PhysicalHands
{
    [CustomEditor(typeof(PhysicalHandsButton), true)]
    [CanEditMultipleObjects]
    public class PhysicalHandsButtonEditor : CustomEditorBase<PhysicalHandsButton>
    {
        private bool eventsFoldedOut = false;

        public override void OnInspectorGUI()
        {
            EditorUtils.DrawScriptField((MonoBehaviour)target);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_pressableObject"), new GUIContent("Pressable Object", "The GameObject representing the button that can be pressed."));

            EditorGUILayout.Space(5);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_buttonPreset"), new GUIContent("Button Type", "The preset type of button. each one changes the buttons responsiveness and how it reacts to being pressed or springing back. \n \n Test out the presets to find the right one for you"));

            if (serializedObject.FindProperty("_buttonPreset").enumValueIndex == 3)
            {
                EditorGUI.indentLevel = 1;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("springValue"), new GUIContent("Spring Strength", "Strength of a rubber-band pull toward the defined direction."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("damperValue"), new GUIContent("Dampening", "Resistance strength against the Position Spring."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxForceValue"), new GUIContent("Max Force", "Amount of force applied to push the object toward the defined direction."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("bouncinessValue"), new GUIContent("Bonciness", "When the joint hits the limit, it can be made to bounce off it."));
                EditorGUI.indentLevel = 0;
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_automaticTravelDistance"), new GUIContent("Use Automatic Travel Distance", "Travel distance should be calculated based on how far the pressable object is from this object"));
            
            if (!serializedObject.FindProperty("_automaticTravelDistance").boolValue)
            {
                EditorGUI.indentLevel = 1;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_buttonTravelDistance"), new GUIContent("Button Travel Distance", "The distance the button can travel when pressed."));
                EditorGUI.indentLevel = 0;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Button Unpress Threshold");
            serializedObject.FindProperty("_buttonPressExitThreshold").floatValue = 
                EditorGUILayout.Slider(serializedObject.FindProperty("_buttonPressExitThreshold").floatValue, 0, 1);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_canBePressedByObjects"), new GUIContent("Can Be Pressed By Objects", "Determines whether the button can be pressed by objects which are not the hand."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_buttonIgnoreGrabs"), new GUIContent("Ignore Grabbing Button", "Specifies whether grabs should be ignored on the Pressable Object."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_whichHandCanPressButton"), new GUIContent("Which Hand Can Activate Button Presses", "Specifies which hand(s) can press the button."));

            EditorGUILayout.Space(10);

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
            target.UpdateInspectorValues();
        }
    }
}