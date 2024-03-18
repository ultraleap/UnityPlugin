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
            var sliderType = serializedObject.FindProperty("_sliderType");
            EditorGUILayout.PropertyField(sliderType);

            if (sliderType.enumValueIndex == (int)PhysicalHandsUISlider.SliderType.ONE_DIMENSIONAL)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_sliderDirection"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_startPercentage"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_numberOfSegments"));
            }
            else if (sliderType.enumValueIndex == (int)PhysicalHandsUISlider.SliderType.TWO_DIMENSIONAL)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_twoDimSliderDirection"));

                target._twoDimStartPercentage = CreateVector2Slider("Start Position: ", "_twoDimStartPercentage", 0, 100);
                target._twoDimNumberOfSegments = CreateVector2Slider("Number Of Segments: ", "_twoDimNumberOfSegments", 0, 100);

            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_sliderTravelDistance"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_freezeIfNotActive"));
            


            EditorGUILayout.Space(20);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_connectedButton"));

            EditorGUILayout.Space(20);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_axisChangeFromZero"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_sliderPercentage"));


            serializedObject.ApplyModifiedProperties();
        }

        private Vector2 CreateVector2Slider(string label, string property, float minvalue, float maxvalue)
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
                (serializedObject.FindProperty(property).vector2Value.x, 0, 100,
                GUILayout.ExpandWidth(true), GUILayout.MinWidth(100));
            EditorGUILayout.EndHorizontal();

            //Slider Precentage Y (second element)
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("" + enumName.ElementAt(1) + " Axis:");
            result.y = (int)EditorGUILayout.Slider
                (serializedObject.FindProperty(property).vector2Value.y, 0, 100,
                GUILayout.ExpandWidth(true), GUILayout.MinWidth(100));
            EditorGUIUtility.labelWidth = 0;
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel = 0;

            return result;
        }

    }

}
