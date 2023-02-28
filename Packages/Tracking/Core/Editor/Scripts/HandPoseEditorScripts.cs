using UnityEditor;
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
                poseRecorderScript.StartSaveCountdown();
            }
        }
    }

    [CustomEditor(typeof(HandPoseScriptableObject))]
    public class HandPoseSerializableObjectEditor : Editor
    {
        float _boneThresholdSlider = 15f;
        float _previousBoneThresholdSlider = 15f;
        bool _sliderHasChanged = false;
        bool _showFineTuningOptions = false;


        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Angle of tolerance for pose");
            _sliderHasChanged = false;
            _boneThresholdSlider =  EditorGUILayout.Slider(_previousBoneThresholdSlider, 0f, 90f);
            if (_boneThresholdSlider != _previousBoneThresholdSlider)
            {
                _sliderHasChanged = true;
                _previousBoneThresholdSlider = _boneThresholdSlider;
            }


            HandPoseScriptableObject serializedObjectScript = (HandPoseScriptableObject)target;
            if(_sliderHasChanged)
            {
                serializedObjectScript.SetAllBoneThresholds(_boneThresholdSlider);
            }

            GUILayout.Space(15);

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
        Vector2 scrollPosition;

        bool _showFineTuningOptions = false;
        string fineTuningOptionsButtonLabel = "Show Fine Tuning Options";

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((MonoBehaviour)target), GetType(), false);
            EditorGUI.EndDisabledGroup();
            HandPoseDetector poseDetectionScript = (HandPoseDetector)target;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_posesToDetect"));
            //EditorGUILayout.PropertyField(serializedObject.FindProperty("_handPoseValidator"));

            poseDetectionScript.CheckBothHands = EditorGUILayout.Toggle("Check Both Hands?", poseDetectionScript.CheckBothHands);

            if (!poseDetectionScript.CheckBothHands)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("ChiralityToCheck"));
            }



            if (GUILayout.Button(fineTuningOptionsButtonLabel))
            {
                _showFineTuningOptions = !_showFineTuningOptions;
            }

            if (_showFineTuningOptions)
            {
                fineTuningOptionsButtonLabel = "Hide Fine Tuning Options";
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_leapProvider"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_hysteresisThreshold"));

                if (GUILayout.Button("Add Bone Direction Target"))
                {
                    poseDetectionScript.CreateDefaultFingerDirection();
                }

                GUILayout.Space(10);

                #region boneDirectionTargets 
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Width(Screen.width-20), GUILayout.Height(500));

                var boneDirectionTargets = serializedObject.FindProperty("BoneDirectionTargets");

                for (int i = 0; i < boneDirectionTargets.arraySize; i++)
                {
                    var boneDirectionTarget = boneDirectionTargets.GetArrayElementAtIndex(i);
                    boneDirectionTarget.FindPropertyRelative("enabled").boolValue = EditorGUILayout.BeginToggleGroup("Toggle: "+ 
                        boneDirectionTarget.FindPropertyRelative("typeOfDirectionCheck").enumNames.GetValue(boneDirectionTarget.FindPropertyRelative("typeOfDirectionCheck").enumValueIndex) +
                        " Direction " + " " + i.ToString(), boneDirectionTarget.FindPropertyRelative("enabled").boolValue);

                    EditorGUILayout.PropertyField(boneDirectionTarget.FindPropertyRelative("typeOfDirectionCheck"));

                    switch (boneDirectionTarget.FindPropertyRelative("typeOfDirectionCheck").enumValueIndex)
                    {
                        case 0: //OBJECT
                            {
                                EditorGUILayout.PropertyField(boneDirectionTarget.FindPropertyRelative("poseTarget"));
                                break;
                            }
                        case 1: //WORLD
                            {
                                EditorGUILayout.PropertyField(boneDirectionTarget.FindPropertyRelative("axisToFace"));
                                break;
                            }
                        case 2: //CAMERALOCAL
                            {
                                EditorGUILayout.PropertyField(boneDirectionTarget.FindPropertyRelative("axisToFace"));
                                break;
                            }
                    }

                    EditorGUILayout.PropertyField(boneDirectionTarget.FindPropertyRelative("isPalmDirection"));
                    if (!boneDirectionTarget.FindPropertyRelative("isPalmDirection").boolValue)
                    {
                        EditorGUILayout.PropertyField(boneDirectionTarget.FindPropertyRelative("fingerTypeForPoint"));
                        EditorGUILayout.PropertyField(boneDirectionTarget.FindPropertyRelative("boneForPoint"));

                    }
                    EditorGUILayout.PropertyField(boneDirectionTarget.FindPropertyRelative("rotationThreshold"));

                    if (GUILayout.Button("Remove", GUILayout.Width(Screen.width * 0.2f), GUILayout.Height(20)))
                    {
                        poseDetectionScript.RemoveDefaultFingerDirection(i);
                    }

                    
                    EditorGUILayout.EndToggleGroup();


                }
                EditorGUILayout.EndScrollView();
                #endregion

            }
            else
            {
                fineTuningOptionsButtonLabel = "Show Fine Tuning Options";
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("OnPoseDetected"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("OnPoseLost"));

            serializedObject.ApplyModifiedProperties();
        }

            

            //DrawDefaultInspector();

    }
}