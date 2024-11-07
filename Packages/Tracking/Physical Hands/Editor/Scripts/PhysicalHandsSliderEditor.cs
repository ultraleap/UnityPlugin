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
    [CustomEditor(typeof(PhysicalHandsSlider))]
    public class PhysicalHandsSliderEditor : CustomEditorBase<PhysicalHandsSlider>
    {
        private bool eventsFoldedOut = false;

        public override void OnInspectorGUI()
        {
            EditorUtils.DrawScriptField((MonoBehaviour)target);

            if (target._slideableObject != null && target._slideableObject.transform.localRotation != Quaternion.identity)
            {
                EditorGUILayout.HelpBox("Warning! Slideable object cannot be rotated. This will cause unexpected behaviour. \n " +
                    "Please rotate the slider instead, leaving slideable object rotation 0,0,0", MessageType.Warning);
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_slideableObject"), new GUIContent("Slideable Object", "The GameObject that acts as the slider."));

            // Connected Button
            if (target._slideableObject != null)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_connectedButton"), new GUIContent("Connected Button", "The button that interacts with the slider."));
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_sliderDirection"), new GUIContent("Slider Direction", "The axis on which the slider moves."));

            EditorGUILayout.Space(5);
            serializedObject.FindProperty("_sliderTravelDistance").floatValue = CreateAxisAttribute("Slider Travel Distance: ", "_sliderTravelDistance", "The travel distance of the slider (from the central point).");
            EditorGUILayout.Slider(serializedObject.FindProperty("_startPosition"), 0, 1, new GUIContent("Start Position", "The starting position of the slider."));

            EditorGUILayout.Space(20);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_numberOfSegments"), new GUIContent("Number of Segments", "Number of segments for the slider to use (0 = unlimited). 2 segments will create a 0/1 slider which snaps to the ends."));

            EditorGUILayout.Space(5);

            // Slider Events
            eventsFoldedOut = EditorGUILayout.BeginFoldoutHeaderGroup(eventsFoldedOut, "Slider Events");

            if (eventsFoldedOut)
            {

                EditorGUILayout.PropertyField(serializedObject.FindProperty("SliderChangeEvent"), new GUIContent("Slider Change Event", "Event triggered when the slider value changes."));

                if (target._connectedButton != null)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("SliderButtonPressedEvent"), new GUIContent("Slider Button Pressed Event", "Event triggered when the slider button is pressed."));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("SliderButtonUnPressedEvent"), new GUIContent("Slider Button Unpressed Event", "Event triggered when the slider button is released."));
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            serializedObject.ApplyModifiedProperties();
        }

        private float CreateAxisAttribute(string label, string property, string tooltip)
        {
            float result = 0f;
            var enumName = serializedObject.FindProperty("_sliderDirection").enumDisplayNames[serializedObject.FindProperty("_sliderDirection").enumValueIndex];

            EditorGUILayout.LabelField(new GUIContent(label, tooltip), EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUIUtility.labelWidth = 50;
            EditorGUI.indentLevel = 1;

            // Slider Precentage X (first element)
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("" + enumName + " Axis:");
            result = EditorGUILayout.FloatField(
                new GUIContent("", tooltip),
                serializedObject.FindProperty(property).floatValue,
                GUILayout.ExpandWidth(true),
                GUILayout.MinWidth(100)
            );
            EditorGUILayout.EndHorizontal();

            EditorGUIUtility.labelWidth = 0;
            EditorGUI.indentLevel = 0;

            return result;
        }

    }

}