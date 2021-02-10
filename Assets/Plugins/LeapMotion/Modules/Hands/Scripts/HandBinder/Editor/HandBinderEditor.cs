/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Leap.Unity.HandsModule {

    [CanEditMultipleObjects]
    [CustomEditor(typeof(HandBinder))]
    public class HandBinderEditor : Editor {
        private HandBinder myTarget;

        private Color handModelDebugCol = Color.green;
        private Color leapHandDebugCol = Color.black;
        private Color previousCol = Color.white;

        private Texture handTexture;
        private Texture buttonTexture;
        private Texture downstate;
        private Texture dividerLine;
        private Texture subButton;

        private SerializedProperty handedness;
        private SerializedProperty debugLeapHand;
        private SerializedProperty DebugLeapRotationAxis;
        private SerializedProperty gizmoSize;
        private SerializedProperty debugModelTransforms;
        private SerializedProperty DebugModelRotationAxis;
        private SerializedProperty setPositions;
        private SerializedProperty useMetaBones;
        private SerializedProperty setEditorPose;
        private SerializedProperty customBoneDefinitions;
        private SerializedProperty globalFingerRotationOffset;
        private SerializedProperty boundHand;
        private SerializedProperty offsets;
        private SerializedProperty fineTuning;
        private SerializedProperty debugOptions;
        private SerializedProperty riggingOptions;
        private SerializedProperty armRigging;

        private Vector2[] objectFieldPositions = new Vector2[]
        {
            //Thumb
            new Vector2(-20, -65f),
            new Vector2(-50, -30),
            new Vector2(-80, 0),
            new Vector2(-100, 40),
            //Index
            new Vector2(-20, -20f),
            new Vector2(-10, 50),
            new Vector2(-10, 80),
            new Vector2(-10, 110),
            //Middle
            new Vector2(10, -20f),
            new Vector2(25, 50),
            new Vector2(30, 80),
            new Vector2(40, 110),
            //Ring
            new Vector2(30, -30f),
            new Vector2(55, 30),
            new Vector2(70, 60),
            new Vector2(80, 85),
            //Pinky
            new Vector2(40, -50),
            new Vector2(80f, 0),
            new Vector2(100, 18),
            new Vector2(120, 35),
            //Wrist
            new Vector2(0, -100)
        };

        private GUIStyle buttonStyle;
        private GUIStyle subButtonStyle;
        private Rect imageRect;
        private Color warningColor = new Color(1f, 0.5529412f, 0f);
        private Color green = new Color32(140, 234, 40, 255);

        /// <summary>
        /// Assign the serialized properties
        /// </summary>
        private void SerializedProperties() {
            handedness = serializedObject.FindProperty("handedness");
            debugLeapHand = serializedObject.FindProperty("DebugLeapHand");
            DebugLeapRotationAxis = serializedObject.FindProperty("DebugLeapRotationAxis");
            gizmoSize = serializedObject.FindProperty("GizmoSize");
            debugModelTransforms = serializedObject.FindProperty("DebugModelTransforms");
            DebugModelRotationAxis = serializedObject.FindProperty("DebugModelRotationAxis");
            setPositions = serializedObject.FindProperty("SetPositions");
            useMetaBones = serializedObject.FindProperty("UseMetaBones");
            setEditorPose = serializedObject.FindProperty("SetEditorPose");
            globalFingerRotationOffset = serializedObject.FindProperty("GlobalFingerRotationOffset");
            customBoneDefinitions = serializedObject.FindProperty("CustomBoneDefinitions");
            handedness = serializedObject.FindProperty("handedness");
            fineTuning = serializedObject.FindProperty("fineTuning");
            debugOptions = serializedObject.FindProperty("debugOptions");
            riggingOptions = serializedObject.FindProperty("riggingOptions");
            armRigging = serializedObject.FindProperty("armRigging");
            boundHand = serializedObject.FindProperty("boundHand");
            offsets = serializedObject.FindProperty("offsets");

            handTexture = Resources.Load<Texture>("Editor_hand");
            buttonTexture = Resources.Load<Texture>("Editor_Documentation_Green_Upstate");
            downstate = Resources.Load<Texture>("Editor_Documentation_Green_Downstate");
            dividerLine = Resources.Load<Texture>("Editor_Divider_line");
            subButton = Resources.Load<Texture>("secondary_button");
        }

        private void OnEnable() {
            serializedObject.Update();
            myTarget = (HandBinder)target;

            if(myTarget.gameObject.name.ToUpper().Contains("Left".ToUpper())) {
                myTarget.handedness = Chirality.Left;
            }
            if(myTarget.gameObject.name.ToUpper().Contains("Right".ToUpper())) {
                myTarget.handedness = Chirality.Right;
            }

            SerializedProperties();
        }

        /// <summary>
        /// Set the GUIStyle of the varius GUI elements we render
        /// </summary>
        private void SetUp() {
            buttonStyle = new GUIStyle(GUI.skin.button) {
                alignment = TextAnchor.MiddleCenter,
                normal = new GUIStyleState() {
                    textColor = Color.black,
                    background = (Texture2D)buttonTexture,
                },
                active = new GUIStyleState() {
                    textColor = Color.white,
                    background = (Texture2D)downstate,
                },
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                stretchHeight = true,
                fixedHeight = 50,
            };

            subButtonStyle = new GUIStyle(GUI.skin.button) {
                alignment = TextAnchor.MiddleCenter,
                normal = new GUIStyleState() {
                    textColor = Color.white,
                    background = (Texture2D)subButton,
                },
                stretchHeight = true,
                fixedHeight = 20,
            };

            previousCol = GUI.color;
        }

        /// <summary>
        /// Draw the inspector GUI
        /// </summary>
        public override void OnInspectorGUI() {
            serializedObject.Update();
            SetUp();
            DrawHandGraphic();
            DrawAutoRigButton();
            ShowBindingOptions();
            ShowDebugOptions();
            ShowFineTuningOptions();
            ShowDocumentationWidow();

            if(myTarget.needsResetting && setEditorPose.boolValue == false) {
                myTarget.ResetHand();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHandGraphic() {
            //Draw the hand texture with object fields
            var middleOfInspector = EditorGUIUtility.currentViewWidth / 2;
            var middleOfImage = middleOfInspector - (handedness.intValue == 0 ? handTexture.width : -handTexture.width) / 2;
            imageRect = new Rect(middleOfImage, 30, handedness.intValue == 0 ? handTexture.width : -handTexture.width, handTexture.height);
            GUILayout.BeginVertical(GUILayout.MinWidth(imageRect.width), GUILayout.MinHeight(imageRect.height));
            EditorGUI.DrawPreviewTexture(imageRect, handTexture);

            int objectFieldPositionIndex = 0;

            for(int fingerIndex = 0; fingerIndex < boundHand.FindPropertyRelative("fingers").arraySize; fingerIndex++) {
                var fingerProperty = boundHand.FindPropertyRelative("fingers").GetArrayElementAtIndex(fingerIndex);

                for(int boneIndex = 0; boneIndex < fingerProperty.FindPropertyRelative("boundBones").arraySize; boneIndex++) {
                    if(boneIndex % 4 == 0 && !useMetaBones.boolValue) {
                        objectFieldPositionIndex++;
                        continue;
                    }

                    var boneProperty = fingerProperty.FindPropertyRelative("boundBones").GetArrayElementAtIndex(boneIndex);
                    DrawObjectField(objectFieldPositions[objectFieldPositionIndex], Event.current, boneProperty, (Finger.FingerType)fingerIndex, (Bone.BoneType)boneIndex);

                    objectFieldPositionIndex++;
                }
            }
            DrawObjectField(objectFieldPositions[20], Event.current, boundHand.FindPropertyRelative("wrist"), Finger.FingerType.TYPE_UNKNOWN, Bone.BoneType.TYPE_INVALID);

            EditorGUILayout.Space();
            GUILayout.EndVertical();
            GUILayout.Space(40);
        }

        /// <summary>
        /// Create a field on the hand that we can assign transforms to
        /// </summary>
        /// <param name="offset">The offset applied to position this object field on the hand visual</param>
        /// <param name="e"></param>
        /// <param name="index">The index of the boundGameobject between 0 - 20</param>
        private void DrawObjectField(Vector2 offset, Event e, SerializedProperty boneProperty, Finger.FingerType fingerIndex, Bone.BoneType boneIndex) {
            var objectRef = boneProperty.FindPropertyRelative("boundTransform");
            var beforeTransform = objectRef.objectReferenceValue as Transform;

            //Check to see if this bone is valid
            bool isAssignedTo = beforeTransform != null;

            //The size of the field
            float referencePointSize = 13;
            offset.x = handedness.intValue == 1 ? -offset.x + 8 : offset.x;
            var center = imageRect.center - offset;
            var newRect = new Rect(center.x, center.y, referencePointSize, referencePointSize);
            var maxSize = new Rect(center.x, center.y, referencePointSize * 6, referencePointSize);
            //Check if the cursor is inside the rect
            var overContent = maxSize.Contains(e.mousePosition);
            newRect = overContent ? maxSize : newRect;

            //Choose a color based on validity
            var color = isAssignedTo ? green : warningColor;
            //Change the color that the gui is stuled based on the validity
            GUI.color = color;

            //Draw the object field
            EditorGUI.ObjectField(newRect, objectRef, GUIContent.none);
            var afterTransform = objectRef.objectReferenceValue as Transform;

            GUI.color = previousCol;

            //Check to see if there is a bone assigned
            if(isAssignedTo) {
                //If there is a bone assigned but it is not the same as the bone that we have
                if(beforeTransform != afterTransform && afterTransform != null) {
                    if(EditorUtility.DisplayDialog("Bind GameObject",
                            "Are you sure you want to overwrite this bound GameObject?", "Yes", "No")) {
                        AssignTransform(boneProperty, afterTransform);
                    }
                }
            }
            else {
                if(afterTransform != null) {
                    //Wrist
                    if(fingerIndex == Finger.FingerType.TYPE_UNKNOWN && boneIndex == Bone.BoneType.TYPE_INVALID) {
                        AssignTransform(boneProperty, afterTransform, true);
                    }
                    //AssignTransform(boneProperty, afterTransform);
                    AutoAssignChildrenBones(afterTransform, fingerIndex, boneIndex);
                }
            }
        }

        private void AutoAssignChildrenBones(Transform child, Finger.FingerType fingerType, Bone.BoneType boneType) {
            if(boneType == Bone.BoneType.TYPE_INVALID) {
                return;
            }

            var fingerProperty = boundHand.FindPropertyRelative("fingers").GetArrayElementAtIndex((int)fingerType);
            var boneProperty = fingerProperty.FindPropertyRelative("boundBones").GetArrayElementAtIndex((int)boneType);
            var objectRef = boneProperty.FindPropertyRelative("boundTransform");
            objectRef.objectReferenceValue = child;

            AssignTransform(boneProperty, child);

            if(boneType == Bone.BoneType.TYPE_DISTAL) {
                return;
            }
            else if(child.childCount > 0) {
                AutoAssignChildrenBones(child.GetChild(0), fingerType, (Bone.BoneType)((int)boneType + 1));
            }
        }

        /// <summary>
        /// Assign the new transform to the serialized property
        /// </summary>
        /// <param name="boneProperty"></param>
        /// <param name="boundTransform"></param>
        private void AssignTransform(SerializedProperty boneProperty, Transform boundTransform, bool resetOverride = false) {
            //Setting a new bone has to be done when the hand is not an in editor pose, so doing this will reset the hand
            if(setEditorPose.boolValue == true && resetOverride == false) {
                myTarget.ResetHand();
                //Ensure the scene gets updated after the hand resets
            }

            var startTransform = boneProperty.FindPropertyRelative("startTransform");

            startTransform.FindPropertyRelative("position").vector3Value = boundTransform.localPosition;
            startTransform.FindPropertyRelative("rotation").vector3Value = boundTransform.localRotation.eulerAngles;
        }

        private void DrawAutoRigButton() {
            //Draw the Auto Rig Button
            if(Selection.gameObjects.Length == 1 && GUILayout.Button("Auto Bind", buttonStyle)) {
                if(EditorUtility.DisplayDialog("AutoRig",
                "Are you sure you want to discard all your changes and run the Auto Bind process?", "Yes", "No")) {
                    Undo.RegisterFullObjectHierarchyUndo(myTarget.gameObject, "AutoRig");
                    HandBinderAutoBinder.AutoRig(myTarget);
                    serializedObject.Update();
                }
            }
            EditorGUILayout.Space();
        }

        private void ShowBindingOptions() {
            //Drop down for rigging options
            riggingOptions.boolValue = GUILayout.Toggle(riggingOptions.boolValue, !riggingOptions.boolValue ? "Show Binding Options" : "Hide Binding Options", subButtonStyle);
            EditorGUILayout.Space();
            GUI.color = Color.white;
            if(riggingOptions.boolValue) {
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(handedness);
                useMetaBones.boolValue = GUILayout.Toggle(useMetaBones.boolValue, "Use Metacarpal  Bones");
                setPositions.boolValue = GUILayout.Toggle(setPositions.boolValue, "Set the positions of the fingers");
                EditorGUILayout.PropertyField(customBoneDefinitions);
                EditorGUILayout.Space();

                armRigging.boolValue = GUILayout.Toggle(armRigging.boolValue, "Arm Binding", "Button");

                if(armRigging.boolValue) {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(boundHand.FindPropertyRelative("elbow").FindPropertyRelative("boundTransform"), new GUIContent("Elbow Transform"));
                    if(EditorGUI.EndChangeCheck()) {
                        if(boundHand.FindPropertyRelative("elbow").objectReferenceValue != null) {
                            var t = boundHand.FindPropertyRelative("elbow").objectReferenceValue as Transform;
                            if(t != null) {
                                boundHand.FindPropertyRelative("elbow").FindPropertyRelative("startTransform").FindPropertyRelative("position").vector3Value = t.localPosition;
                                boundHand.FindPropertyRelative("elbow").FindPropertyRelative("startTransform").FindPropertyRelative("rotation").vector3Value = t.localRotation.eulerAngles;

                                //Calculate the elbow length when the elbow gets assigned
                                if(myTarget.boundHand.wrist.boundTransform != null) {
                                    myTarget.elbowLength = (myTarget.boundHand.wrist.boundTransform.position - t.position).magnitude;
                                }
                            }
                        }
                    }
                    EditorGUILayout.PropertyField(boundHand.FindPropertyRelative("elbow").FindPropertyRelative("offset").FindPropertyRelative("position"), new GUIContent("Elbow Position Offset"));
                    EditorGUILayout.PropertyField(boundHand.FindPropertyRelative("elbow").FindPropertyRelative("offset").FindPropertyRelative("rotation"), new GUIContent("Elbow Rotation Offset"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("elbowLength"));
                }
                EditorGUILayout.Space();
                GUILayout.Label(dividerLine);
                EditorGUILayout.Space();
            }
        }

        private void ShowDebugOptions() {
            //Draw the debugging options
            debugOptions.boolValue = GUILayout.Toggle(debugOptions.boolValue, !debugOptions.boolValue ? "Show Debug Options" : "Hide Debug Options", subButtonStyle);
            EditorGUILayout.Space();
            if(debugOptions.boolValue) {
                EditorGUILayout.Space();
                GUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(debugLeapHand);
                if(debugLeapHand.boolValue) {
                    leapHandDebugCol = EditorGUILayout.ColorField(GUIContent.none, leapHandDebugCol, false, false, false);
                }
                GUILayout.EndHorizontal();
                EditorGUILayout.PropertyField(DebugLeapRotationAxis);
                GUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(debugModelTransforms);
                if(debugModelTransforms.boolValue)
                    handModelDebugCol = EditorGUILayout.ColorField(GUIContent.none, handModelDebugCol, false, false, false);
                GUILayout.EndHorizontal();
                EditorGUILayout.PropertyField(DebugModelRotationAxis);
                EditorGUILayout.PropertyField(gizmoSize);
                setEditorPose.boolValue = GUILayout.Toggle(setEditorPose.boolValue, setEditorPose.boolValue ? "Reset Hand" : "Align with Leap Pose", "Button");
                EditorGUILayout.Space();
                GUILayout.Label(dividerLine);
                EditorGUILayout.Space();
            }
        }

        private void ShowFineTuningOptions() {
            //Draw the fine tuning options
            fineTuning.boolValue = GUILayout.Toggle(fineTuning.boolValue, !fineTuning.boolValue ? "Show Fine Tuning Options" : "Hide Fine Tuning Options", subButtonStyle);
            EditorGUILayout.Space();
            if(fineTuning.boolValue) {
                EditorGUILayout.Space();
                GUI.color = Color.white;
                GUILayout.BeginVertical("Box");
                EditorGUILayout.PropertyField(boundHand.FindPropertyRelative("wrist").FindPropertyRelative("offset").FindPropertyRelative("position"), new GUIContent("Wrist Position Offset"));
                EditorGUILayout.PropertyField(boundHand.FindPropertyRelative("wrist").FindPropertyRelative("offset").FindPropertyRelative("rotation"), new GUIContent("Wrist Rotation Offset"));
                EditorGUILayout.Space();
                GUI.color = previousCol;
                GUILayout.EndVertical();
                EditorGUILayout.PropertyField(globalFingerRotationOffset);
                EditorGUILayout.Space();
                if(Selection.gameObjects.Length == 1 && GUILayout.Button("Recalculate Offsets")) {
                    Undo.RegisterFullObjectHierarchyUndo(myTarget.gameObject, "Recalculate Offsets");
                    HandBinderAutoBinder.EstimateWristRotationOffset(myTarget);
                }
                EditorGUILayout.Space();
                GUILayout.Label(dividerLine);
                EditorGUILayout.Space();

                for(int offsetIndex = 0; offsetIndex < offsets.arraySize; offsetIndex++) {
                    SerializedProperty boundType = offsets.GetArrayElementAtIndex(offsetIndex);
                    BoundTypes previousBoundType = myTarget.offsets[offsetIndex];
                    SerializedProperty offsetProperty = BoundTypeToOffsetProperty((BoundTypes)boundType.intValue);
                    SerializedProperty offsetRotation = offsetProperty.FindPropertyRelative("rotation");
                    SerializedProperty offsetPosition = offsetProperty.FindPropertyRelative("position");

                    GUILayout.BeginVertical("Box");
                    GUILayout.BeginHorizontal("Box");
                    EditorGUILayout.PropertyField(boundType, GUIContent.none);

                    if(GUILayout.Button(EditorGUIUtility.IconContent("d_Toolbar Minus"))) {
                        if(boundType.intValue != (int)BoundTypes.WRIST) {
                            offsetRotation.vector3Value = Vector3.zero;
                            offsetPosition.vector3Value = Vector3.zero;
                        }
                        offsets.DeleteArrayElementAtIndex(offsetIndex);
                        break;
                    }
                    GUILayout.EndHorizontal();

                    //Check to see if the user has changed the value
                    if((int)previousBoundType != boundType.intValue) {
                        //Check to see if any of the offsets are the same as this one
                        if(myTarget.offsets.Any(x => (int)x == boundType.intValue)) {
                            boundType.intValue = (int)previousBoundType;
                        }
                        else {
                            offsetRotation.vector3Value = Vector3.zero;
                            offsetPosition.vector3Value = Vector3.zero;
                            offsetProperty = BoundTypeToOffsetProperty((BoundTypes)boundType.intValue);
                            offsetRotation = offsetProperty.FindPropertyRelative("rotation");
                            offsetPosition = offsetProperty.FindPropertyRelative("position");
                        }
                    }

                    EditorGUILayout.PropertyField(offsetPosition);
                    EditorGUILayout.PropertyField(offsetRotation);
                    GUILayout.EndVertical();
                    EditorGUILayout.Space();
                    GUILayout.Label(dividerLine);
                    EditorGUILayout.Space();
                }

                GUILayout.BeginHorizontal("Box");
                GUILayout.Label("Add Finger Offset");
                if(GUILayout.Button(EditorGUIUtility.IconContent("d_Toolbar Plus"))) {
                    if(offsets.arraySize < 22) {
                        offsets.InsertArrayElementAtIndex(offsets.arraySize);
                        var offset = offsets.GetArrayElementAtIndex(offsets.arraySize - 1);

                        var enumList = new List<int>();

                        for(int i = 0; i < 22; i++) {
                            enumList.Add(i);
                        }

                        var result = enumList.Where(typeA => myTarget.offsets.All(typeB => (int)typeB != typeA)).FirstOrDefault();

                        offset.intValue = result;
                    }
                }
                GUILayout.EndHorizontal();
                EditorGUILayout.Space();
                GUILayout.Label(dividerLine);
                EditorGUILayout.Space();
            }
        }

        private void ShowDocumentationWidow() {
            //Draw a button for the user to open the set up guide
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            if(GUILayout.Button("Setup Guide", subButtonStyle)) {
                var window = (HandBinderDocumentationWindow)EditorWindow.GetWindow(typeof(HandBinderDocumentationWindow));
                window.Show();
            }
            GUILayout.Space(20);
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Convert a BoundType to the offsetProperty for the bound transform
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private SerializedProperty BoundTypeToOffsetProperty(BoundTypes boundType) {
            if(boundType == BoundTypes.WRIST) {
                return boundHand.FindPropertyRelative("wrist").FindPropertyRelative("offset");
            }
            else if(boundType == BoundTypes.ELBOW) {
                return boundHand.FindPropertyRelative("elbow").FindPropertyRelative("offset");
            }
            else if(!HandBinderUtilities.boundTypeMapping.ContainsKey(boundType)) {
                return null;
            }

            (Finger.FingerType fingerType, Bone.BoneType boneType) fingerBoneType = HandBinderUtilities.boundTypeMapping[boundType];
            return FingerOffsetPropertyFromLeapTypes(fingerBoneType.fingerType, fingerBoneType.boneType);
        }

        private SerializedProperty FingerOffsetPropertyFromLeapTypes(Finger.FingerType fingerType, Bone.BoneType boneType) {
            return boundHand.FindPropertyRelative("fingers").GetArrayElementAtIndex((int)fingerType).FindPropertyRelative("boundBones").GetArrayElementAtIndex((int)boneType).FindPropertyRelative("offset");
        }

        /// <summary>
        /// Draw extra gizmos in the scene to help the user while they edit variables
        /// </summary>
        private void OnSceneGUI() {
            //Update the editor pose, this will only get called when the object is selected.
            if(!Application.isPlaying) {
                //EditorHandPose();
            }

            myTarget = (HandBinder)target;
            if(myTarget == null) {
                return;
            }

            //Draw the leap hand
            if(myTarget.DebugLeapHand) {
                Handles.color = leapHandDebugCol;
                foreach(var finger in myTarget.LeapHand.Fingers) {
                    var index = 0;

                    foreach(var bone in finger.bones) {
                        Handles.SphereHandleCap(-1, bone.PrevJoint.ToVector3(), Quaternion.identity, myTarget.GizmoSize, EventType.Repaint);
                        if((index + 1) <= finger.bones.Length - 1) {
                            Handles.DrawLine(finger.bones[index].PrevJoint.ToVector3(), finger.bones[index + 1].PrevJoint.ToVector3());
                        }

                        if(DebugLeapRotationAxis.boolValue) {
                            DrawLeapBasis(bone, gizmoSize.floatValue * 4);
                        }
                        index++;
                    }
                }

                Handles.SphereHandleCap(-1, myTarget.LeapHand.WristPosition.ToVector3(), Quaternion.identity, myTarget.GizmoSize, EventType.Repaint);

                var elbowPosition = myTarget.LeapHand.WristPosition.ToVector3() - (myTarget.LeapHand.Arm.Basis.zBasis.ToVector3().normalized * myTarget.elbowLength);
                Handles.SphereHandleCap(-1, elbowPosition, Quaternion.identity, myTarget.GizmoSize, EventType.Repaint);
                Handles.DrawLine(elbowPosition, myTarget.LeapHand.WristPosition.ToVector3());
            }

            //Draw the bound Gameobjects
            if(myTarget.DebugModelTransforms) {
                Handles.color = handModelDebugCol;
                for(int finger = 0; finger < myTarget.boundHand.fingers.Length; finger++) {
                    for(int bone = 0; bone < myTarget.boundHand.fingers[finger].boundBones.Length; bone++) {
                        var target = myTarget.boundHand.fingers[finger].boundBones[bone].boundTransform;
                        if(target != null) {
                            if(myTarget.DebugModelTransforms) {
                                Handles.DrawWireDisc(target.position, target.right, gizmoSize.floatValue);
                                Handles.DrawWireDisc(target.position, target.up, gizmoSize.floatValue);
                                Handles.DrawWireDisc(target.position, target.forward, gizmoSize.floatValue);
                            }

                            if(DebugModelRotationAxis.boolValue) {
                                DrawTransformBasis(target, gizmoSize.floatValue * 4);
                            }
                        }
                    }
                }

                //Draw the wrist Gizmo
                if(myTarget.boundHand.wrist.boundTransform != null) {
                    var target = myTarget.boundHand.wrist.boundTransform;
                    Handles.DrawWireDisc(target.position, target.right, gizmoSize.floatValue);
                    Handles.DrawWireDisc(target.position, target.up, gizmoSize.floatValue);
                    Handles.DrawWireDisc(target.position, target.forward, gizmoSize.floatValue);
                }
            }
        }

        private void DrawLeapBasis(Leap.Bone bone, float size) {
            Vector3 middle, y, x, z;

            middle = bone.PrevJoint.ToVector3();
            y = bone.Basis.xBasis.ToVector3();
            x = bone.Basis.yBasis.ToVector3();
            z = bone.Basis.zBasis.ToVector3();

            Handles.color = Color.green;
            Handles.DrawLine(middle, middle + y.normalized * size);
            Handles.color = Color.red;
            Handles.DrawLine(middle, middle + x.normalized * size);
            Handles.color = Color.blue;
            Handles.DrawLine(middle, middle + z.normalized * size);
            Handles.color = leapHandDebugCol;
        }

        private void DrawTransformBasis(Transform bone, float size) {
            Vector3 middle, y, x, z;

            var prevCol = Handles.color;
            middle = bone.position;
            y = bone.up;
            x = bone.right;
            z = bone.forward;

            Handles.color = green;
            Handles.DrawLine(middle, middle + y.normalized * size);
            Handles.color = Color.red;
            Handles.DrawLine(middle, middle + x.normalized * size);
            Handles.color = Color.blue;
            Handles.DrawLine(middle, middle + z.normalized * size);
            Handles.color = prevCol;
        }
    }
}