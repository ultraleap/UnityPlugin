/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Leap.PhysicalHandsExamples
{
    [CustomEditor(typeof(TwoDimensionalPhysicalHandsSlider))]
    public class TwoDimensionalPhysicalHandsSliderEditor : CustomEditorBase<TwoDimensionalPhysicalHandsSlider>
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

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_slideableObject"), new GUIContent("Slideable Object: ", "The GameObject that acts as the slider."));

            if (target._slideableObject != null)
            {
                // Connected Button
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_connectedButton"), new GUIContent("Connected Button", "The button that interacts with the slider."));
            }

            EditorGUILayout.Space(5);
            serializedObject.FindProperty("SliderTravelDistance").vector2Value = CreateVector2AxisAttribute("Slider Travel Distance: ", "SliderTravelDistance", "The travel distance of the slider (from the central point).");
            serializedObject.FindProperty("_startPosition").vector2Value = CreateVector2SliderAttribute("Start Position: ", "_startPosition", "The starting position of the slider.");

            EditorGUILayout.Space(20);
            serializedObject.FindProperty("_numberOfSegments").vector2Value = CreateVector2AxisAttribute("Number of Segments: ", "_numberOfSegments", "Number of segments for the slider to use (0 = unlimited).");

            EditorGUILayout.Space(5);

            // Slider Events
            eventsFoldedOut = EditorGUILayout.BeginFoldoutHeaderGroup(eventsFoldedOut, "Slider Events");

            if (eventsFoldedOut)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("TwoDimSliderChangeEvent"), new GUIContent("Slider Change Event", "Event triggered when the slider value changes."));

                if (target._connectedButton != null)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("TwoDimSliderButtonPressedEvent"), new GUIContent("Slider Button Pressed Event", "Event triggered when the slider button is pressed."));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("TwoDimSliderButtonUnPressedEvent"), new GUIContent("Slider Button Unpressed Event", "Event triggered when the slider button is released."));
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            serializedObject.ApplyModifiedProperties();
        }

        private Vector2 CreateVector2AxisAttribute(string label, string property, string tooltip)
        {
            Vector2 result = Vector2.zero;
            var enumName = "XZ";

            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUIUtility.labelWidth = 50;
            EditorGUI.indentLevel = 1;

            // Slider Precentage X (first element)
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("" + enumName.ElementAt(0) + " Axis:");
            result.x = EditorGUILayout.FloatField(
                new GUIContent("", tooltip),
                serializedObject.FindProperty(property).vector2Value.x,
                GUILayout.ExpandWidth(true),
                GUILayout.MinWidth(100)
            );
            EditorGUILayout.EndHorizontal();

            // Slider Precentage Y (second element)
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("" + enumName.ElementAt(1) + " Axis:");
            result.y = EditorGUILayout.FloatField(
                new GUIContent("", tooltip),
                serializedObject.FindProperty(property).vector2Value.y,
                GUILayout.ExpandWidth(true),
                GUILayout.MinWidth(100)
            );
            EditorGUIUtility.labelWidth = 0;
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel = 0;

            return result;
        }

        private Vector2 CreateVector2SliderAttribute(string label, string property, string tooltip)
        {
            Vector2 result = Vector2.zero;
            var enumName = "XZ";

            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUIUtility.labelWidth = 50;
            EditorGUI.indentLevel = 1;

            // Slider Precentage X (first element)
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("" + enumName.ElementAt(0) + " Axis:");
            result.x = EditorGUILayout.Slider(
                serializedObject.FindProperty(property).vector2Value.x,
                0, 1
            );
            EditorGUILayout.EndHorizontal();

            // Slider Precentage Y (second element)
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("" + enumName.ElementAt(1) + " Axis:");
            result.y = EditorGUILayout.Slider(
                serializedObject.FindProperty(property).vector2Value.y,
                0, 1
            );
            EditorGUIUtility.labelWidth = 0;
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel = 0;

            return result;
        }

        private Vector2 CreateVector2IntAxisAttribute(string label, string property, string tooltip)
        {
            Vector2 result = Vector2.zero;
            var enumName = "XZ";

            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUIUtility.labelWidth = 50;
            EditorGUI.indentLevel = 1;

            // Slider Precentage X (first element)
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("" + enumName.ElementAt(0) + " Axis:");
            result.x = Mathf.RoundToInt(EditorGUILayout.FloatField(
                new GUIContent("", tooltip),
                serializedObject.FindProperty(property).vector2Value.x,
                GUILayout.ExpandWidth(true),
                GUILayout.MinWidth(100)
            ));
            EditorGUILayout.EndHorizontal();

            // Slider Precentage Y (second element)
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("" + enumName.ElementAt(1) + " Axis:");
            result.y = Mathf.RoundToInt(EditorGUILayout.FloatField(
                new GUIContent("", tooltip),
                serializedObject.FindProperty(property).vector2Value.y,
                GUILayout.ExpandWidth(true),
                GUILayout.MinWidth(100)
            ));
            EditorGUIUtility.labelWidth = 0;
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel = 0;

            return result;
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