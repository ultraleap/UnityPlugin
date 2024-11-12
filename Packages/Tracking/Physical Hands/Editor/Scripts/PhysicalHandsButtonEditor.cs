/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEditor;
using UnityEngine;

namespace Leap.PhysicalHands
{
    [CustomEditor(typeof(PhysicalHandsButton), true)]
    [CanEditMultipleObjects]
    public class PhysicalHandsButtonEditor : CustomEditorBase<PhysicalHandsButton>
    {
        private bool eventsFoldedOut = false;

        public override void OnInspectorGUI()
        {
            EditorUtils.DrawScriptField((MonoBehaviour)target);

            WarningsSection();

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

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_whichHandCanPressButton"), new GUIContent("Which Hand Can Activate Button Presses", "Specifies which hand(s) can press the button. \n\nIf you wish to ignore collisions. Use an IgnorePhysicalHands component on the PressableObject."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_usePrimaryHover"), new GUIContent("Use Primary Hover", "When ticked, the button will only register presses when it is primary hovered by either hand \n \n (Only one button can be primary hovered by each hand at a time)."));


            EditorGUILayout.Space(5);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_automaticTravelDistance"), new GUIContent("Use Automatic Travel Distance", "Travel distance will be calculated automaticall based on how far the pressable object is from this object"));

            if (!serializedObject.FindProperty("_automaticTravelDistance").boolValue)
            {
                EditorGUI.indentLevel = 1;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_buttonTravelDistance"), new GUIContent("Button Travel Distance", "The distance the button can travel before it is pressed."));
                EditorGUI.indentLevel = 0;
            }

            EditorGUILayout.BeginHorizontal();
            //EditorGUILayout.LabelField("Button Unpress Threshold");
            EditorGUILayout.Slider(serializedObject.FindProperty("_buttonPressExitThreshold"), 0, 1, new GUIContent("Button Exit Threshold", "How far up should the button travel before it is considered unpressed. " +
                "\n \n The higher the value, the less it needs to be lifted before being considered unpressed. i.e. at 0.8, the button will unpress at 80% of its full travel distance"));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_canBePressedByObjects"), new GUIContent("Can Be Pressed By Objects", "Determines whether the button can be pressed by objects which are not the hand."));

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

        private void WarningsSection()
        {
            var physicalHandsManager = GameObject.FindFirstObjectByType<PhysicalHandsManager>(FindObjectsInactive.Include);
            if (physicalHandsManager == null)
            {
                EditorGUILayout.HelpBox($"There is no Physical Hands Manager in your scene.\nThis button will not work correctly.", MessageType.Warning);
                EditorGUILayout.Space(5);
            }
            else if(GameObject.FindFirstObjectByType<GrabHelper>(FindObjectsInactive.Include) == null)
            {
                EditorGUILayout.HelpBox($"There is no Grab Helper on your Physical Hands Manager.\nThis button will not work correctly.", MessageType.Warning);
                if(GUILayout.Button("Add Grab Helper"))
                {
                    physicalHandsManager.gameObject.AddComponent<GrabHelper>();
                }
                EditorGUILayout.Space(5);
            }

        }
    }
}