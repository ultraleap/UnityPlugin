using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Leap.Unity
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
            poseRecorderScript.savePath = EditorGUILayout.TextField("HandPoses/");
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Save Current Hand Pose"))
            {
                poseRecorderScript.SaveCurrentHandPose();
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
            DrawFingerPointsEditor();

            DrawJointRotationThresholds();

            string hysterisisTooltp = "How many degrees away from the original threshold must the user move to " +
                "stop the detection of each joint for the pose. This helps to avoid flickering detection when on the boundaries of thresholds";

            target.hysteresisThreshold = EditorGUILayout.FloatField(new GUIContent("Hysteresis Threshold:", hysterisisTooltp), target.hysteresisThreshold);

            EditorGUILayout.Space();

            if (GUILayout.Button("Show Extra Options"))
            {
                _showFineTuningOptions = !_showFineTuningOptions;
            }

            if (_showFineTuningOptions)
            {
                DrawDefaultInspector();
            }

            EditorUtility.SetDirty(target);
        }

        private void DrawJointRotationThresholds()
        {
            string thresholdTooltip = "Rotation thresholds relate to how close in degrees the joint's rotation must be to the pose before it will be considered detected.";

            GUILayout.Space(15);
            EditorGUILayout.LabelField(new GUIContent("Finger Joint Rotation Thresholds", thresholdTooltip), EditorStyles.boldLabel);
            EditorGUILayout.LabelField(new GUIContent("Global Joint Rotation Threshold", thresholdTooltip));

            _sliderHasChanged = false;

            _boneThresholdSlider = EditorGUILayout.Slider(target.globalRotation, 0f, 90f);

            if (_boneThresholdSlider != target.globalRotation)
            {
                _sliderHasChanged = true;
            }

            if (_sliderHasChanged)
            {
                target.SetAllBoneThresholds(_boneThresholdSlider);
            }

            EditorGUILayout.LabelField("Key:");
            EditorGUILayout.LabelField("Flex = Flexion/Curl, Abd = Abduction/Splay");

            GUILayout.Space(5);

            for (int fingerID = 0; fingerID < target.fingerJointRotationThresholds.Length; fingerID++)
            {
                if (!ShouldShowFinger(fingerID))
                {
                    continue;
                }

                EditorGUILayout.LabelField(new GUIContent(GetFingerName(fingerID) + " Joint Thresholds", thresholdTooltip), EditorStyles.boldLabel);

                float labelWidth = EditorGUIUtility.labelWidth;

                for (int jointID = 0; jointID < target.fingerJointRotationThresholds[fingerID].jointThresholds.Length; jointID++)
                {
                    if (jointID == 0)
                    {
                        EditorGUIUtility.labelWidth = 30;

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(new GUIContent(GetJointName(jointID), thresholdTooltip), GUILayout.Width(120));
                        GUILayout.FlexibleSpace();
                        var flex = EditorGUILayout.FloatField(new GUIContent("Flex:", thresholdTooltip), target.fingerJointRotationThresholds[fingerID].jointThresholds[jointID].x, GUILayout.Width(80));
                        var abd = EditorGUILayout.FloatField(new GUIContent("Abd:", thresholdTooltip), target.fingerJointRotationThresholds[fingerID].jointThresholds[jointID].y, GUILayout.Width(80));
                        EditorGUILayout.EndHorizontal();

                        target.fingerJointRotationThresholds[fingerID].jointThresholds[jointID] = new Vector2(flex, abd);
                    }
                    else if (fingerID == 0 || (fingerID != 0 && jointID != 2)) // Only present distal for thumbs
                    {
                        EditorGUIUtility.labelWidth = 30;

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(new GUIContent(GetJointName(jointID), thresholdTooltip), GUILayout.Width(120));
                        GUILayout.FlexibleSpace();
                        var flex = EditorGUILayout.FloatField(new GUIContent("Flex:", thresholdTooltip), target.fingerJointRotationThresholds[fingerID].jointThresholds[jointID].x, GUILayout.Width(80));
                        GUILayout.Space(83);
                        EditorGUILayout.EndHorizontal();

                        target.fingerJointRotationThresholds[fingerID].jointThresholds[jointID] = Vector2.one * flex;
                    }
                }
                EditorGUIUtility.labelWidth = labelWidth;
                GUILayout.Space(15);
            }
        }

        string GetJointName(int jointID)
        {
            switch (jointID)
            {
                case 0:
                    return "Proximal Joint";
                case 1:
                    return "Intermediate Joint";
                case 2:
                    return "Distal Joint";
            }

            return "Joint " + jointID;
        }

        string GetFingerName(int fingerID)
        {
            switch (fingerID)
            {
                case 0:
                    return "Thumb";
                case 1:
                    return "Index";
                case 2:
                    return "Middle";
                case 3:
                    return "Ring";
                case 4:
                    return "Pinky";
            }

            return "Finger " + fingerID;
        }

        bool ShouldShowFinger(int fingerID)
        {
            switch (fingerID)
            {
                case 0:
                    return target.detectThumb;
                case 1:
                    return target.detectIndex;
                case 2:
                    return target.detectMiddle;
                case 3:
                    return target.detectRing;
                case 4:
                    return target.detectPinky;
            }

            return true;
        }

        private void DrawFingerPointsEditor()
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

            MakeToggle(new Vector2(-0.310F, 0.170F), ref target.detectThumb); // thumb
            MakeToggle(new Vector2(-0.060F, -0.170F), ref target.detectIndex); // index
            MakeToggle(new Vector2(0.080F, -0.190F), ref target.detectMiddle); // middle
            MakeToggle(new Vector2(0.220F, -0.150F), ref target.detectRing); // ring
            MakeToggle(new Vector2(0.340F, -0.050F), ref target.detectPinky); // pinky

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
        bool _showFineTuningOptions = false;
        string fineTuningOptionsButtonLabel = "Show Fine Tuning Options";

        private enum FingerName
        {
            Thumb = 0,
            IndexFinger = 1,
            MiddleFinger = 2,
            RingFinger = 3,
            PinkyFinger = 4,
            Palm = 5
        }
        FingerName fingerName = FingerName.IndexFinger;
        private enum BoneName
        {
            Proximal = 1,
            Intermediate = 2,
            Distal = 3
        }
        BoneName boneName = BoneName.Distal;

        List<bool> sourceFoldout = new List<bool>();

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((MonoBehaviour)target), GetType(), false);
            EditorGUI.EndDisabledGroup();

            HandPoseDetector poseDetectionScript = (HandPoseDetector)target;

            GUILayout.Space(10);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("poseToDetect"));

            EditorGUI.indentLevel = 2;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("posesToDetect"), new GUIContent("Pose variations: "));
            EditorGUI.indentLevel = 0;

            GUILayout.Space(10);

            poseDetectionScript.checkBothHands = EditorGUILayout.Toggle("Detect both hands?", poseDetectionScript.checkBothHands);

            if (!poseDetectionScript.checkBothHands)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("chiralityToCheck"));
            }
            GUILayout.Space(10);

            #region Bone Directions 

            EditorGUILayout.LabelField("Direction Rules", EditorStyles.boldLabel);
            var thinGreyLine = EditorGUILayout.GetControlRect(false, GUILayout.Height(2), GUILayout.Width(Screen.width));
            EditorGUI.DrawRect(thinGreyLine, Color.grey);

            string poseDirectionExplanationText = "Rules allow you to limit when your pose will be detected. \n\n" +
                "These rules can be made up of many optional targets and are tied to the direction of specific parts of a hand. \ne.g. in the direction of your index finger.";

            GUILayout.BeginVertical();
            EditorStyles.textField.clipping = TextClipping.Overflow;
            var textArea = EditorStyles.textArea.CalcHeight(new GUIContent(poseDirectionExplanationText), Screen.width);
            EditorStyles.textField.wordWrap = true;
            GUILayout.Label(poseDirectionExplanationText, EditorStyles.textField);
            GUILayout.EndVertical();

            GUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(" + Add a new rule for finger/palm"))
            {
                poseDetectionScript.CreatePoseRule();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            SerializedProperty sources = serializedObject.FindProperty("poseRules");
            if (sourceFoldout.Count != sources.arraySize)
            {
                sourceFoldout.Clear();
                foreach (var item in sources)
                {
                    sourceFoldout.Add(true);
                }
            }

            for (int i = 0; i < sources.arraySize; i++)
            {
                var source = sources.GetArrayElementAtIndex(i);
                fingerName = (FingerName)source.FindPropertyRelative("finger").intValue;

                sourceFoldout[i] = EditorGUILayout.BeginFoldoutHeaderGroup(sourceFoldout.ElementAt(i),
                "Rule for " + fingerName.ToString() + " direction");

                if (sourceFoldout[i])
                {
                    GUILayout.BeginVertical("box");
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Observed part of the hand");

                    source.FindPropertyRelative("finger").intValue = (int)(FingerName)EditorGUILayout.EnumPopup(fingerName);

                    int boneNumber = (int)boneName;
                    EditorGUILayout.EndHorizontal();

                    GUILayout.Space(10);
                    EditorGUILayout.LabelField("Targets:", EditorStyles.boldLabel);

                    EditorStyles.textField.clipping = TextClipping.Overflow;
                    EditorStyles.textField.wordWrap = true;
                    GUILayout.Label("At least one of the following targets need to be met for the event to activate.", EditorStyles.miniLabel);
                    GUILayout.Space(5);

                    GUILayout.Space(5);
                    var thinnerGreyLine = EditorGUILayout.GetControlRect(false, GUILayout.Height(1), GUILayout.Width(Screen.width));
                    EditorGUI.DrawRect(thinnerGreyLine, Color.grey);

                    var directionList = source.FindPropertyRelative("directions");
                    for (int j = 0; j < directionList.arraySize; j++)
                    {
                        EditorGUI.indentLevel = 0;
                        var direction = directionList.GetArrayElementAtIndex(j);

                        direction.FindPropertyRelative("enabled").boolValue = EditorGUILayout.BeginToggleGroup(GetTargetName(direction), direction.FindPropertyRelative("enabled").boolValue);

                        source.FindPropertyRelative("bone").intValue = boneNumber;

                        EditorGUI.indentLevel = 1;

                        EditorGUILayout.PropertyField(direction.FindPropertyRelative("typeOfDirectionCheck"), new GUIContent("Direction relative to:"));

                        switch (direction.FindPropertyRelative("typeOfDirectionCheck").enumValueIndex)
                        {
                            case 0: //OBJECT
                                {
                                    EditorGUILayout.PropertyField(direction.FindPropertyRelative("poseTarget"), new GUIContent("Object to face:"));
                                    break;
                                }
                            case 1: //WORLD
                                {
                                    EditorGUILayout.PropertyField(direction.FindPropertyRelative("axisToFace"), new GUIContent("Axis to face:"));
                                    break;
                                }
                            case 2: //CAMERALOCAL
                                {
                                    EditorGUILayout.PropertyField(direction.FindPropertyRelative("axisToFace"), new GUIContent("Axis to face:"));
                                    break;
                                }
                        }

                        EditorGUILayout.PropertyField(direction.FindPropertyRelative("rotationThreshold"), new GUIContent("Rotation threshold:"));
                        EditorGUILayout.EndToggleGroup();

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(15);

                        if (GUILayout.Button("Remove target"))
                        {
                            poseDetectionScript.RemoveRuleDirection(i, j);
                        }

                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();
                        GUILayout.Space(5);

                        var thinnerGreyLine2 = EditorGUILayout.GetControlRect(false, GUILayout.Height(1), GUILayout.Width(Screen.width));
                        EditorGUI.DrawRect(thinnerGreyLine2, Color.grey);
                        GUILayout.Space(5);
                    }
                    GUILayout.Space(5);
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("+ Add target"))
                    {
                        poseDetectionScript.CreateRuleDirection(i);
                    }
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();


                    GUILayout.EndVertical();

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Remove rule"))
                    {
                        poseDetectionScript.RemoveRule(i);
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUI.DrawRect(thinGreyLine, Color.grey);
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
                GUILayout.Space(10);
            }
            GUILayout.Space(20);
            #endregion

            EditorGUILayout.LabelField("Pose Events", EditorStyles.boldLabel);
            var thinGreyLine2 = EditorGUILayout.GetControlRect(false, GUILayout.Height(2), GUILayout.Width(Screen.width));
            EditorGUI.DrawRect(thinGreyLine2, Color.grey);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("OnPoseDetected"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("WhilePoseDetected"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("OnPoseLost"));

            if (GUILayout.Button(fineTuningOptionsButtonLabel))
            {
                _showFineTuningOptions = !_showFineTuningOptions;
            }

            if (_showFineTuningOptions)
            {
                fineTuningOptionsButtonLabel = "Hide Fine Tuning Options";
                EditorGUILayout.PropertyField(serializedObject.FindProperty("leapProvider"));
            }
            else
            {
                fineTuningOptionsButtonLabel = "Show Fine Tuning Options";
            }
            serializedObject.ApplyModifiedProperties();
        }
        string GetTargetName(SerializedProperty direction)
        {
            HandPoseDetector.TypeOfDirectionCheck dir = (HandPoseDetector.TypeOfDirectionCheck)direction.FindPropertyRelative("typeOfDirectionCheck").enumValueIndex;

            string name = "";

            name += dir.ToString() + " ";

            switch (dir)
            {
                case HandPoseDetector.TypeOfDirectionCheck.TowardsObject:
                    break;
                case HandPoseDetector.TypeOfDirectionCheck.WorldDirection:
                    name += (HandPoseDetector.AxisToFace)direction.FindPropertyRelative("axisToFace").enumValueIndex;
                    break;
                case HandPoseDetector.TypeOfDirectionCheck.CameraDirection:
                    name += (HandPoseDetector.AxisToFace)direction.FindPropertyRelative("axisToFace").enumValueIndex;
                    break;
            }

            return name;
        }
    }

    [CustomEditor(typeof(HandPoseEditor), editorForChildClasses: true)]
    public class HandPoseEditorEditor : CustomEditorBase<HandPoseEditor>
    {
        bool _showFineTuningOptions = false;

        protected override void OnEnable()
        {
            base.OnEnable();

            // Edit-time pose is only relevant for providers that generate hands.
            // Post-process Providers are a special case and don't generate their own hands.
            specifyConditionalDrawing(() => false, "editTimePose");
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((MonoBehaviour)target), GetType(), false);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(10);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("handPose"));
            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Edit Pose", GUILayout.Width(Screen.width / 2), GUILayout.Height(30)))
            {
                EditorGUIUtility.PingObject(target.handPose);
                Selection.SetActiveObjectWithContext(target.handPose, target.handPose);
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(10);

            if (GUILayout.Button("Show Extra Options"))
            {
                _showFineTuningOptions = !_showFineTuningOptions;
            }

            if (_showFineTuningOptions)
            {
                base.OnInspectorGUI();
            }
        }
    }

    [CustomEditor(typeof(HandPoseViewer), editorForChildClasses: true)]
    public class HandPoseViewerEditor : CustomEditorBase<HandPoseViewer>
    {
        protected override void OnEnable()
        {
            base.OnEnable();

            // Edit-time pose is only relevant for providers that generate hands.
            // Post-process Providers are a special case and don't generate their own hands.
            specifyConditionalDrawing(() => false, "editTimePose");
        }
    }
}