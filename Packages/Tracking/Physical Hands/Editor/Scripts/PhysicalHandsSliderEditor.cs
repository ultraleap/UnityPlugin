using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

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
            }
            else if (sliderType.enumValueIndex == (int)PhysicalHandsUISlider.SliderType.TWO_DIMENSIONAL)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_twoDimSliderDirection"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_twoDimStartPercentage"));
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
    }

}
