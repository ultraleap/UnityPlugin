using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Animations;
using UnityEngine.UIElements;
using System.Linq;

namespace Leap.Unity.PhysicalHands
{
    [CustomEditor(typeof(PhysicalHandsUISlider))]
    public class PhysicalHandsSliderEditor : CustomEditorBase<PhysicalHandsUISlider>
    {

        private bool eventsFoldedOut = false;

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_slideableObject"), new GUIContent("Slideable Object", "The GameObject that acts as the slider."));

            var sliderType = serializedObject.FindProperty("_sliderType");
            EditorGUILayout.PropertyField(sliderType, new GUIContent("Slider Type", "The type of slider (one-dimensional or two-dimensional)."));

            if (sliderType.enumValueIndex == (int)PhysicalHandsUISlider.SliderType.ONE_DIMENSIONAL)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_sliderDirection"), new GUIContent("Slider Direction", "The direction in which the slider moves."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_startPosition"), new GUIContent("Start Position", "The starting position of the slider."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_numberOfSegments"), new GUIContent("Number of Segments", "Number of segments for the slider to use (0 = unlimited)."));
                serializedObject.FindProperty("SliderTravelDistance").floatValue = CreateAxisAttribute("Slider Travel Distance: ", "SliderTravelDistance", "The travel distance of the slider (from the central point).");
            }
            else if (sliderType.enumValueIndex == (int)PhysicalHandsUISlider.SliderType.TWO_DIMENSIONAL)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_twoDimSliderDirection"), new GUIContent("Two-Dimensional Slider Direction", "The direction of movement for the two-dimensional slider."));

                serializedObject.FindProperty("_twoDimStartPosition").vector2Value = CreateVector2AxisAttribute("Start Position: ", "_twoDimStartPosition", "Starting position of the two-dimensional slider.");
                serializedObject.FindProperty("_twoDimNumberOfSegments").vector2Value = CreateVector2IntAxisAttribute("Number Of Segments: ", "_twoDimNumberOfSegments", "Number of segments for the two-dimensional slider to use (0 = unlimited).");
                serializedObject.FindProperty("TwoDimSliderTravelDistance").vector2Value = CreateVector2AxisAttribute("Slider Travel Distance: ", "TwoDimSliderTravelDistance", "The travel distance of the two-dimensional slider.");
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_freezeIfNotActive"), new GUIContent("Freeze If Not Active", "Flag to freeze the slider position if not active."));

            EditorGUILayout.Space(20);

            // Connected Button
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_connectedButton"), new GUIContent("Connected Button", "The button that interacts with the slider."));

            EditorGUILayout.Space(20);

            // Axis Change From Zero
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_axisChangeFromZero"), new GUIContent("Axis Change From Zero", "Change in axis from zero position."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_sliderValue"), new GUIContent("Slider Value", "Current value of the slider."));

            // Slider Events
            eventsFoldedOut = EditorGUILayout.BeginFoldoutHeaderGroup(eventsFoldedOut, "Slider Events");

            if (eventsFoldedOut)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("SliderChangeEvent"), new GUIContent("Slider Change Event", "Event triggered when the slider value changes."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("SliderButtonPressedEvent"), new GUIContent("Slider Button Pressed Event", "Event triggered when the slider button is pressed."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("SliderButtonUnPressedEvent"), new GUIContent("Slider Button Unpressed Event", "Event triggered when the slider button is released."));
            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            serializedObject.ApplyModifiedProperties();
        }

        private Vector2 CreateVector2AxisSlider(string label, string property, float minvalue, float maxvalue)
        {
            Vector2 result = Vector2.zero;
            var enumName = serializedObject.FindProperty("_twoDimSliderDirection").enumDisplayNames[serializedObject.FindProperty("_twoDimSliderDirection").enumValueIndex];

            /// All of this is to set up sliders which change label automatically

            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUIUtility.labelWidth = 50;
            EditorGUI.indentLevel = 1;

            //Slider Precentage X (first element)
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("" + enumName.ElementAt(0) + " Axis:");
            result.x = (int)EditorGUILayout.Slider
                (serializedObject.FindProperty(property).vector2Value.x, minvalue, maxvalue,
                GUILayout.ExpandWidth(true), GUILayout.MinWidth(100));
            EditorGUILayout.EndHorizontal();

            //Slider Precentage Y (second element)
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("" + enumName.ElementAt(1) + " Axis:");
            result.y = (int)EditorGUILayout.Slider
                (serializedObject.FindProperty(property).vector2Value.y, minvalue, maxvalue,
                GUILayout.ExpandWidth(true), GUILayout.MinWidth(100));
            EditorGUIUtility.labelWidth = 0;
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel = 0;

            return result;
        }

        private Vector2 CreateVector2AxisAttribute(string label, string property, string tooltip)
        {
            Vector2 result = Vector2.zero;
            var enumName = serializedObject.FindProperty("_twoDimSliderDirection").enumDisplayNames[serializedObject.FindProperty("_twoDimSliderDirection").enumValueIndex];

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

        private Vector2 CreateVector2IntAxisAttribute(string label, string property, string tooltip)
        {
            Vector2 result = Vector2.zero;
            var enumName = serializedObject.FindProperty("_twoDimSliderDirection").enumDisplayNames[serializedObject.FindProperty("_twoDimSliderDirection").enumValueIndex];

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
