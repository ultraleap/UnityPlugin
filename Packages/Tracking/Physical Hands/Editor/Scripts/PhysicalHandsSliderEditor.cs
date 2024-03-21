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

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_slideableObject"));

            var sliderType = serializedObject.FindProperty("_sliderType");
            EditorGUILayout.PropertyField(sliderType);

            if (sliderType.enumValueIndex == (int)PhysicalHandsUISlider.SliderType.ONE_DIMENSIONAL)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_sliderDirection"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_startPosition"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_numberOfSegments"));
                target.SliderTravelDistance = CreateAxisAttribute("Slider Travel Distance: ", "SliderTravelDistance");

            }
            else if (sliderType.enumValueIndex == (int)PhysicalHandsUISlider.SliderType.TWO_DIMENSIONAL)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_twoDimSliderDirection"));

                target._twoDimStartPosition = CreateVector2AxisAttribute("Start Position: ", "_twoDimStartPosition");
                target._twoDimNumberOfSegments = CreateVector2AxisAttribute("Number Of Segments: ", "_twoDimNumberOfSegments");
                target.TwoDimSliderTravelDistance = CreateVector2AxisAttribute("Slider Travel Distance: ", "TwoDimSliderTravelDistance");

            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_freezeIfNotActive"));
            
            EditorGUILayout.Space(20);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_connectedButton"));

            EditorGUILayout.Space(20);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_axisChangeFromZero"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_sliderValue"));


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

        private Vector2 CreateVector2AxisAttribute(string label, string property)
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
            result.x = EditorGUILayout.FloatField
                (serializedObject.FindProperty(property).vector2Value.x,
                GUILayout.ExpandWidth(true), GUILayout.MinWidth(100));
            EditorGUILayout.EndHorizontal();

            //Slider Precentage Y (second element)
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("" + enumName.ElementAt(1) + " Axis:");
            result.y = EditorGUILayout.FloatField
                (serializedObject.FindProperty(property).vector2Value.y,
                GUILayout.ExpandWidth(true), GUILayout.MinWidth(100));
            EditorGUIUtility.labelWidth = 0;
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel = 0;

            return result;
        }

        private float CreateAxisAttribute(string label, string property)
        {
            float result = 0f;
            var enumName = serializedObject.FindProperty("_sliderDirection").enumDisplayNames[serializedObject.FindProperty("_sliderDirection").enumValueIndex];

            /// All of this is to set up sliders which change label automatically

            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUIUtility.labelWidth = 50;
            EditorGUI.indentLevel = 1;

            //Slider Precentage X (first element)
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("" + enumName + " Axis:");
            result = EditorGUILayout.FloatField
                (serializedObject.FindProperty(property).floatValue,
                GUILayout.ExpandWidth(true), GUILayout.MinWidth(100));
            EditorGUILayout.EndHorizontal();

            EditorGUIUtility.labelWidth = 0;
            EditorGUI.indentLevel = 0;

            return result;
        }

    }

}
