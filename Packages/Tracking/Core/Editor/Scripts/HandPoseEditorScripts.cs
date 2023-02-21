using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;


namespace Leap.Unity.HandsModule
{
    [CustomEditor(typeof(HandPoseRecorder))]
    public class HandPoseRecoderEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            HandPoseRecorder poseRecorderScript = (HandPoseRecorder)target;
            if (GUILayout.Button("Save Current Hand Pose"))
            {
                poseRecorderScript.SaveCurrentHandPose();
            }
        }
    }

    [CustomEditor(typeof(HandPoseScriptableObject))]
    public class HandPoseSerializableObjectEditor : Editor
    {
        float _boneThresholdSlider = 15f;
        bool _showFineTuningOptions = false;

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Angle of tolerance for pose");

            _boneThresholdSlider =  EditorGUILayout.Slider(_boneThresholdSlider, 0f, 90f);
            HandPoseScriptableObject serializedObjectScript = (HandPoseScriptableObject)target;
            serializedObjectScript.SetAllBoneThresholds(_boneThresholdSlider);

            GUILayout.Space(15);
            //EditorGUILayout.PropertyField(serializedObject.FindProperty("_careAboutOrientation"));

            if (GUILayout.Button("Show Fine Tuning Options"))
            {
                _showFineTuningOptions = !_showFineTuningOptions;
            }
            
            if (_showFineTuningOptions)
            {
                DrawDefaultInspector();
            }
            
        }
    }

    [CustomEditor(typeof(HandPoseDetector))]
    public class PoseDetectionEditor : Editor
    {
        bool _showFineTuningOptions = false;
        public override void OnInspectorGUI()
        {
            HandPoseDetector poseDetectionScript = (HandPoseDetector)target;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_posesToDetect"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_handPoseValidator"));

            poseDetectionScript.CheckBothHands = EditorGUILayout.Toggle("Check Both Hands?", poseDetectionScript.CheckBothHands);

            if (!poseDetectionScript.CheckBothHands)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("ChiralityToCheck"));
            }

            serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Show Fine Tuning Options"))
            {
                _showFineTuningOptions = !_showFineTuningOptions;
            }

            if (_showFineTuningOptions)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_leapProvider"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_hysteresisThreshold"));
            }

            DrawDefaultInspector();

        }
    }

   




}