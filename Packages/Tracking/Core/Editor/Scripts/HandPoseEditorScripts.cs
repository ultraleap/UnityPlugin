using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;


namespace Leap.Unity.HandsModule
{
    [CustomEditor(typeof(HandPoseRecorder))]
    public class HandPoseRecoderEditor : Editor
    {
        private readonly string _assetsPath = "Assets/";

        public override void OnInspectorGUI()
        {

            DrawDefaultInspector();
            HandPoseRecorder poseRecorderScript = (HandPoseRecorder)target;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Pose save path: ");
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(_assetsPath, GUILayout.Width(50));
            poseRecorderScript.SavePath = EditorGUILayout.TextField("HandPoses/");
            EditorGUILayout.EndHorizontal();
            

            if (GUILayout.Button("Save Current Hand Pose"))
            {
                poseRecorderScript.StartSaveCountdown();
            }
        }
    }

[CustomEditor(typeof(HandPoseScriptableObject))]
    public class HandPoseSerializableObjectEditor : CustomEditorBase<HandPoseScriptableObject>
    {
        const float TOGGLE_SIZE = 15.0F;

        Texture _handTex;
        Rect _handTexRect;

        float _boneThresholdSlider = 15f;
        bool _sliderHasChanged = false;
        bool _showFineTuningOptions = false;

        protected override void OnEnable()
        {
            base.OnEnable();
            _handTex = Resources.Load<Texture2D>("HandTex");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Angle of tolerance for pose");
            _sliderHasChanged = false;

            _boneThresholdSlider = target.globalRotation.x;
            target.globalRotation.x =  EditorGUILayout.Slider(target.globalRotation.x, 0f, 90f);

            if (_boneThresholdSlider != target.globalRotation.x)
            {
                _sliderHasChanged = true;
                _boneThresholdSlider = target.globalRotation.x;
            }

            HandPoseScriptableObject serializedObjectScript = (HandPoseScriptableObject)target;
            if(_sliderHasChanged)
            {
                serializedObjectScript.SetAllBoneThresholds(target.globalRotation.x);
            }

            DrawAttachmentPointsEditor();

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

        private void DrawAttachmentPointsEditor()
        {
            // Set up the draw rect space based on the image and available editor space.
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Fingers to detect", EditorStyles.boldLabel);

            _handTexRect = EditorGUILayout.BeginVertical(GUILayout.MinWidth(EditorGUIUtility.currentViewWidth),
                                                         GUILayout.MinHeight(EditorGUIUtility.currentViewWidth * (_handTex.height / (float)_handTex.width)),
                                                         GUILayout.MaxWidth(_handTex.width),
                                                         GUILayout.MaxHeight(_handTex.height));

            Rect imageContainerRect = _handTexRect; imageContainerRect.width = EditorGUIUtility.currentViewWidth - 30F;
            EditorGUI.DrawRect(imageContainerRect, new Color(0.2F, 0.2F, 0.2F));
            imageContainerRect.x += 1; imageContainerRect.y += 1; imageContainerRect.width -= 2; imageContainerRect.height -= 2;
            EditorGUI.DrawRect(imageContainerRect, new Color(0.6F, 0.6F, 0.6F));
            imageContainerRect.x += 1; imageContainerRect.y += 1; imageContainerRect.width -= 2; imageContainerRect.height -= 2;
            EditorGUI.DrawRect(imageContainerRect, new Color(0.2F, 0.2F, 0.2F));

            _handTexRect = new Rect(_handTexRect.x + (imageContainerRect.center.x - _handTexRect.center.x),
                                    _handTexRect.y,
                                    _handTexRect.width,
                                    _handTexRect.height);
            EditorGUI.DrawTextureTransparent(_handTexRect, _handTex);
            EditorGUILayout.Space();

            // Draw the toggles
            EditorGUI.BeginDisabledGroup(false);

            MakeToggle(new Vector2(-0.310F, 0.170F), ref target.DetectThumb); // thumb
            MakeToggle(new Vector2(-0.060F, -0.170F), ref target.DetectIndex); // index
            MakeToggle(new Vector2(0.080F, -0.190F), ref target.DetectMiddle); // middle
            MakeToggle(new Vector2(0.220F, -0.150F), ref target.DetectRing); // ring
            MakeToggle(new Vector2(0.340F, -0.050F), ref target.DetectPinky); // pinky

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndVertical();
        }

        private void MakeToggle(Vector2 offCenterPosImgSpace, ref bool boolToChange)
        {
            if (EditorGUI.Toggle(MakeToggleRect(_handTexRect.center
                                                + new Vector2(offCenterPosImgSpace.x * _handTexRect.width,
                                                              offCenterPosImgSpace.y * _handTexRect.height)),
                                 boolToChange))
            {
                boolToChange = true;
            }
            else
            {
                boolToChange = false;
            }

        }
        private Rect MakeToggleRect(Vector2 centerPos)
        {
            return new Rect(centerPos.x - TOGGLE_SIZE / 2F, centerPos.y - TOGGLE_SIZE / 2F, TOGGLE_SIZE, TOGGLE_SIZE);
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

                if (GUILayout.Button("Add Bone Direction Target"))
                {
                    poseDetectionScript.CreateDefaultFingerDirection();
                }

                GUILayout.Space(10);

                var boneDirectionTargets = serializedObject.FindProperty("BoneDirectionTargets");

                #region boneDirectionTargets 
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Width(Screen.width-20), GUILayout.Height(Mathf.Min(500, boneDirectionTargets.arraySize * 180)));

                

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

                    EditorGUILayout.EndToggleGroup();

                    if (GUILayout.Button("Remove", GUILayout.Width(Screen.width * 0.2f), GUILayout.Height(20)))
                    {
                        poseDetectionScript.RemoveDefaultFingerDirection(i);
                    }
                }
                EditorGUILayout.EndScrollView();
                #endregion

            }
            else
            {
                fineTuningOptionsButtonLabel = "Show Fine Tuning Options";
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("OnPoseDetected"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("WhilePoseDetected"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("OnPoseLost"));

            serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomEditor(typeof(HandPoseEditor), editorForChildClasses: true)]
    public class HandPoseEditorEditor : CustomEditorBase<HandPoseEditor>
    {

        List<HandPoseScriptableObject> handPoses = new List<HandPoseScriptableObject>();

        protected override void OnEnable()
        {
            base.OnEnable();

            // Edit-time pose is only relevant for providers that generate hands.
            // Post-process Providers are a special case and don't generate their own hands.
            specifyConditionalDrawing(() => false, "editTimePose");
        }

        private void OnSceneGUI()
        {
            UpdatePoseEditor();
        }

        private void UpdatePoseEditor()
        {
            var handPoseEditor = (HandPoseEditor)target;
            var handPoseGuids = AssetDatabase.FindAssets("t:HandPoseScriptableObject");
            handPoses.Clear();
            foreach (var guid in handPoseGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                handPoses.Add(AssetDatabase.LoadAssetAtPath<HandPoseScriptableObject>(path));
            }

            if(handPoseEditor.PoseScritableIntName.Count != handPoses.Count)
            {
                handPoseEditor.PoseScritableIntName.Clear();

                for (int i = 0; i < handPoses.Count; i++)
                {
                    var scriptableObject = handPoses.ElementAt(i);
                    handPoseEditor.PoseScritableIntName.Add(i, scriptableObject.name);
                }
            }


            if (handPoseEditor.PoseScritableIntName.Count > 0)
            {
                EditorGUILayout.LabelField("Pose to view");
                handPoseEditor.Selected = EditorGUILayout.Popup(handPoseEditor.Selected, handPoseEditor.PoseScritableIntName.Values.ToArray());
                target.handPose = handPoses.ElementAt(handPoseEditor.Selected);
                EditorGUILayout.Space(10);
            }
            else
            {
                EditorGUILayout.LabelField("No poses found, please record a pose using the pose recorder in order to view a pose.");
                EditorGUILayout.Space(10);
            }
            EditorUtility.SetDirty(target);
        }

        public override void OnInspectorGUI()
        {
            UpdatePoseEditor();

            base.OnInspectorGUI();
        }
    }
}