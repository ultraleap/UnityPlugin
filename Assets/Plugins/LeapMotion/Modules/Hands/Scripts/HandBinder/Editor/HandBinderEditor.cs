﻿using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Leap.Unity.HandsModule {

    [CanEditMultipleObjects]
    [CustomEditor(typeof(HandBinder))]
    public class HandBinderEditor : Editor {
        private HandBinder myTarget;

        private static Color handModelDebugCol = Color.green;
        private static Color leapHandDebugCol = Color.black;
        private static Color previousCol = Color.white;

        private Texture handTexture;
        private Texture buttonTexture;
        private Texture downstate;
        private Texture dividerLine;
        private Texture buttonGrey;
        private Texture buttonGreyDown;

        private List<bool> bonesValid = new List<bool>();
        private bool fineTuning;
        private bool debugOptions;
        private bool riggingOptions;
        private bool armRigging;

        private SerializedProperty handedness;
        private SerializedProperty debugLeapHand;
        private SerializedProperty gizmoSize;
        private SerializedProperty debugModelTransforms;
        private SerializedProperty setPositions;
        private SerializedProperty useMetaBones;
        private SerializedProperty setEditorPose;
        private SerializedProperty customBoneDefinitions;
        private SerializedProperty globalFingerRotationOffset;
        private SerializedProperty wristRotationOffset;
        private SerializedProperty boundGameobjects;
        private SerializedProperty startTransforms;
        private SerializedProperty offsets;

        private SerializedProperty elbow;
        private SerializedProperty shoulder;
        private SerializedProperty elbowRotationOffset;
        private SerializedProperty elbowPositionOffset;
        private SerializedProperty shoulderRotationOffset;
        private SerializedProperty elbowOffset;
        private SerializedProperty shoulderOffset;

        private GUIStyle buttonStyle;
        private GUIStyle statusStyle;
        private GUIStyle subButtonStyle;

        private Rect imageRect;

        private static float statusTimer;

        /// <summary>
        /// Assign the serialized properties
        /// </summary>
        private void SerializedProperties() {
            handedness = serializedObject.FindProperty("handedness");
            debugLeapHand = serializedObject.FindProperty("DebugLeapHand");
            gizmoSize = serializedObject.FindProperty("GizmoSize");
            debugModelTransforms = serializedObject.FindProperty("DebugModelTransforms");
            setPositions = serializedObject.FindProperty("SetPositions");
            useMetaBones = serializedObject.FindProperty("UseMetaBones");
            setEditorPose = serializedObject.FindProperty("SetEditorPose");
            globalFingerRotationOffset = serializedObject.FindProperty("GlobalFingerRotationOffset");
            wristRotationOffset = serializedObject.FindProperty("WristRotationOffset");
            boundGameobjects = serializedObject.FindProperty("BoundGameobjects");
            startTransforms = serializedObject.FindProperty("StartTransforms");
            customBoneDefinitions = serializedObject.FindProperty("CustomBoneDefinitions");
            offsets = serializedObject.FindProperty("Offsets");

            elbow = serializedObject.FindProperty("elbow");
            shoulder = serializedObject.FindProperty("shoulder");
            elbowRotationOffset = serializedObject.FindProperty("elbowRotationOffset");
            elbowPositionOffset = serializedObject.FindProperty("elbowPositionOffset");
            shoulderRotationOffset = serializedObject.FindProperty("shoulderRotationOffset");
            elbowOffset = serializedObject.FindProperty("elbowOffset");
            shoulderOffset = serializedObject.FindProperty("shoulderOffset");

            handTexture = Resources.Load<Texture>("Editor_hand");
            buttonTexture = Resources.Load<Texture>("Editor_Documentation_Green_Upstate");
            downstate = Resources.Load<Texture>("Editor_Documentation_Green_Downstate");
            dividerLine = Resources.Load<Texture>("Editor_Divider_line");
            buttonGrey = Resources.Load<Texture>("Editor_Button_Grey");
            buttonGreyDown = Resources.Load<Texture>("Editor_Button_Grey_Down");

            myTarget = (HandBinder)target;
        }

        private void OnEnable() {
            serializedObject.Update();

            SerializedProperties();

            //Only do this if no bones have been assigned
            if(myTarget.BoundGameobjects.All(x => x == null)) {
                if(myTarget.gameObject.name.ToUpper().Contains("Left".ToUpper())) {
                    handedness.enumValueIndex = (int)Chirality.Left;
                }
                else if(myTarget.gameObject.name.ToUpper().Contains("Right".ToUpper())) {
                    handedness.enumValueIndex = (int)Chirality.Right;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Set the GUIStyle of the varius GUI elements we render
        /// </summary>
        private void StyleSetUp() {
            statusStyle = new GUIStyle() {
                alignment = TextAnchor.MiddleCenter,
                normal = new GUIStyleState() {
                    textColor = Color.white,
                },
                fontSize = 15,
            };

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
                    textColor = Color.black,
                    background = (Texture2D)buttonGrey,
                },
                active = new GUIStyleState() {
                    textColor = Color.white,
                    background = (Texture2D)buttonGreyDown
                },
                fontSize = 15,
                stretchHeight = true,
                fixedHeight = 30,
            };

            previousCol = GUI.color;
        }

        /// <summary>
        /// Draw the inspector GUI
        /// </summary>
        public override void OnInspectorGUI() {
            serializedObject.Update();
            StyleSetUp();

            //Figure out the middle of the inspector
            var middleOfInspector = EditorGUIUtility.currentViewWidth / 2;

            //Now create an offset so that the texture sits in the middle
            var middleOfImage = middleOfInspector - (handedness.intValue == 0 ? handTexture.width : -handTexture.width) / 2;

            //Create the rect that the texture will use
            imageRect = new Rect(middleOfImage, 30, handedness.intValue == 0 ? handTexture.width : -handTexture.width, handTexture.height);

            ////Begin drawing an area to display the hand
            GUILayout.BeginVertical(GUILayout.MinWidth(imageRect.width), GUILayout.MinHeight(imageRect.height));
            EditorGUI.DrawPreviewTexture(imageRect, handTexture);
            bonesValid.Clear();

            //Draw the gizmos for the hand graphic in the inspector
            DrawHandWithObjectFields(handedness.intValue == 0);

            EditorGUILayout.Space();

            GUILayout.EndVertical();

            statusTimer -= Time.deltaTime;
            if(statusTimer > 0) {
                GUILayout.Space(20);

                var valid = bonesValid.All(x => x == false);
                GUI.color = valid ? Color.green : Color.red;
                var status = (valid ? "Status : Completed Successfully" : "Status : Completed With Errors");
                EditorGUILayout.LabelField(status, statusStyle);
                GUI.color = previousCol;
                statusTimer -= Time.deltaTime;
            }

            GUILayout.Space(20);

            if(GUILayout.Button("Auto Rig Hand", buttonStyle)) {
                statusTimer = 5;
                //Allow the user to undo the auto rig
                Undo.RegisterCompleteObjectUndo(myTarget, "Autorig");
                HandBinderAutoRigger.AutoRig(myTarget);
            }

            EditorGUILayout.Space();
            //Choose if this hand is the left or right hand

            GUI.color = riggingOptions ? Color.green : Color.white;
            if(GUILayout.Button(!riggingOptions ? "Show Rigging Options" : "Hide Rigging Options")) {
                riggingOptions = !riggingOptions;
            }

            GUI.color = Color.white;

            if(riggingOptions) {
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(handedness);
                useMetaBones.boolValue = GUILayout.Toggle(useMetaBones.boolValue, "Use Metacarpal  Bones");
                setPositions.boolValue = GUILayout.Toggle(setPositions.boolValue, "Set the positions of the fingers");
                EditorGUILayout.PropertyField(customBoneDefinitions);
                EditorGUILayout.Space();

                GUI.color = armRigging ? Color.green : Color.white;
                if(GUILayout.Button("Arm Rigging")) {
                    armRigging = !armRigging;
                }
                GUI.color = Color.white;
                GUILayout.Space(armRigging ? 0 : 10);

                if(armRigging) {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(elbow);
                    if(EditorGUI.EndChangeCheck()) {

                        if(elbow.objectReferenceValue != null) {
                            var t = (elbow.objectReferenceValue) as GameObject;
                            if(t != null) {
                                elbowOffset.FindPropertyRelative("position").vector3Value = t.transform.localPosition;
                                elbowOffset.FindPropertyRelative("rotation").vector3Value = t.transform.localRotation.eulerAngles;
                            }
                        }
                    }

                    EditorGUILayout.PropertyField(elbowPositionOffset);
                    EditorGUILayout.PropertyField(elbowRotationOffset);

                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(shoulder);
                    if(EditorGUI.EndChangeCheck()) {
                        if(((shoulder.objectReferenceValue) as Transform) != null) {
                            var shoulderOffset = new HandBinder.Offset();
                            shoulderOffset.position = ((shoulder.objectReferenceValue) as Transform).transform.localPosition;
                            shoulderOffset.rotation = ((shoulder.objectReferenceValue) as Transform).localRotation.eulerAngles;
                            myTarget.shoulderOffset = shoulderOffset;
                        }

                        if(shoulder.objectReferenceValue != null) {
                            var t = (shoulder.objectReferenceValue) as GameObject;
                            if(t != null) {
                                shoulderOffset.FindPropertyRelative("position").vector3Value = t.transform.localPosition;
                                shoulderOffset.FindPropertyRelative("rotation").vector3Value = t.transform.localRotation.eulerAngles;
                            }
                        }
                    }

                    EditorGUILayout.PropertyField(shoulderRotationOffset);
                    EditorGUILayout.Space();
                }
            }

            GUI.color = debugOptions ? Color.green : Color.white;
            if(GUILayout.Button(!debugOptions ? "Show Debug Options" : "Hide Debug Options")) {
                debugOptions = !debugOptions;
            }
            GUI.color = Color.white;

            if(debugOptions) {
                EditorGUILayout.Space();

                GUI.color = debugLeapHand.boolValue ? Color.green : previousCol;
                GUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(debugLeapHand);
                if(debugLeapHand.boolValue)
                    leapHandDebugCol = EditorGUILayout.ColorField(GUIContent.none, leapHandDebugCol, false, false, false);
                GUILayout.EndHorizontal();

                GUI.color = debugModelTransforms.boolValue ? Color.green : previousCol;
                GUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(debugModelTransforms);
                if(debugModelTransforms.boolValue)
                    handModelDebugCol = EditorGUILayout.ColorField(GUIContent.none, handModelDebugCol, false, false, false);
                GUILayout.EndHorizontal();

                GUI.color = previousCol;
                EditorGUILayout.PropertyField(gizmoSize);
                EditorGUILayout.PropertyField(setEditorPose);

                if(setEditorPose.boolValue != myTarget.SetEditorPose) {
                    if(setEditorPose.boolValue == false) {
                        myTarget.ResetHand();
                    }
                    else {
                    }
                }

                if(GUILayout.Button("Reset Hand")) {
                    myTarget.ResetHand();
                    setEditorPose.boolValue = false;
                }
            }

            GUI.color = fineTuning ? Color.green : Color.white;
            if(GUILayout.Button(!fineTuning ? "Show Fine Tuning Options" : "Hide Fine Tuning Options")) {
                fineTuning = !fineTuning;
            }
            GUI.color = Color.white;

            if(fineTuning) {
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(wristRotationOffset);
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(globalFingerRotationOffset);

                EditorGUILayout.Space();

                if(GUILayout.Button("Recalculate Offsets")) {
                    HandBinderAutoRigger.CalculateWristRotationOffset(myTarget);
                    Undo.RegisterCreatedObjectUndo(myTarget, "Recalculate Offsets");
                }

                EditorGUILayout.Space();
                GUILayout.Label(dividerLine);
                EditorGUILayout.Space();

                for(int i = 0; i < offsets.arraySize; i++) {
                    var element = offsets.GetArrayElementAtIndex(i);
                    var fingerType = element.FindPropertyRelative("fingerType");
                    var boneType = element.FindPropertyRelative("boneType");
                    var rotation = element.FindPropertyRelative("rotation");
                    var position = element.FindPropertyRelative("position");

                    var fingerName = fingerType.enumNames[element.FindPropertyRelative("fingerType").enumValueIndex];
                    var boneName = boneType.enumNames[element.FindPropertyRelative("boneType").enumValueIndex];

                    //EditorGUILayout.PropertyField(element, true);

                    EditorGUILayout.BeginHorizontal();
                    //GUILayout.Label(name.stringValue);
                    EditorGUILayout.PropertyField(fingerType, GUIContent.none);
                    EditorGUILayout.PropertyField(boneType, GUIContent.none);
                    if(GUILayout.Button(EditorGUIUtility.IconContent("Toolbar Minus"))) {
                        offsets.DeleteArrayElementAtIndex(i);
                        continue;
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginVertical();
                    GUILayout.BeginHorizontal();
                    //GUILayout.Label(EditorGUIUtility.IconContent("MoveTool"));
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("position"));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    //GUILayout.Label(EditorGUIUtility.IconContent("RotateTool"));
                    rotation.vector3Value = EditorGUILayout.Vector3Field("Rotation", rotation.vector3Value);

                    GUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();

                    EditorGUILayout.Space();
                    GUILayout.Label(dividerLine);
                    EditorGUILayout.Space();
                }
                if(GUILayout.Button("Add Finger Offset + ", subButtonStyle)) {
                    offsets.InsertArrayElementAtIndex(offsets.arraySize);
                }
            }

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            if(GUILayout.Button("Setup Guide")) {
                var window = (HandBinderDocumentationWindow)EditorWindow.GetWindow(typeof(HandBinderDocumentationWindow));
                window.Show();
            }
            GUILayout.Space(20);
            GUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Draw the hand with all the transforms on the hand you can bind
        /// </summary>
        /// <param name="isLeft"></param>
        private void DrawHandWithObjectFields(bool isLeft) {
            Vector2[] positions = new Vector2[]
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

            for(int i = 0; i < boundGameobjects.arraySize; i++) {
                if(i % 4 == 0 && !useMetaBones.boolValue && i != boundGameobjects.arraySize - 1) {
                    continue;
                }

                CreateObjectField(positions[i], Event.current, i);
            }
        }

        /// <summary>
        /// Create a field on the hand that we can assign transforms to
        /// </summary>
        /// <param name="offset">The offset applied to position this object field on the hand visual</param>
        /// <param name="e"></param>
        /// <param name="index">The index of the boundGameobject between 0 - 20</param>
        private void CreateObjectField(Vector2 offset, Event e, int index) {
            var boundObject = boundGameobjects.GetArrayElementAtIndex(index);
            var beforeTransform = boundObject.objectReferenceValue as Transform;

            //Check to see if this bone is valid
            bool isAssignedTo = beforeTransform != null;
            bonesValid.Add(isAssignedTo);

            //The size of the field
            float referencePointSize = 13;
            offset.x = handedness.intValue == 1 ? -offset.x + 12f : offset.x;
            var center = imageRect.center - offset;
            var newRect = new Rect(center.x, center.y, referencePointSize, referencePointSize);
            var maxSize = new Rect(center.x, center.y, referencePointSize * 6, referencePointSize);
            //Check if the cursor is inside the rect
            var overContent = maxSize.Contains(e.mousePosition);
            newRect = overContent ? maxSize : newRect;

            //Choose a color based on validity
            var color = isAssignedTo ? Color.green : Color.red;
            //Change the color that the gui is stuled based on the validity
            GUI.color = color;

            //Draw the object field
            EditorGUI.ObjectField(newRect, boundObject, GUIContent.none);
            var afterTransform = boundObject.objectReferenceValue as Transform;

            GUI.color = previousCol;

            //Check to see if there is a bone assigned
            if(isAssignedTo) {
                //If there is a bone assigned but it is not the same as the bone that we have
                if(beforeTransform != afterTransform && afterTransform != null) {
                    AssignTransform(index, afterTransform);
                }
            }
            else {
                if(afterTransform != null) {
                    AssignTransform(index, afterTransform);
                }
            }
        }

        private void AssignTransform(int index, Transform boundTransform) {
            //Setting a new bone has to be done when the hand is not an in editor pose, so doing this will reset the hand
            if(setEditorPose.boolValue == true) {
                myTarget.ResetHand();
                setEditorPose.boolValue = false;
            }

            var startT = startTransforms.GetArrayElementAtIndex(index);

            Finger.FingerType fingerType;
            Bone.BoneType boneType;
            HandBinderAutoRigger.IndexToType(index, out fingerType, out boneType);

            startT.FindPropertyRelative("fingerType").intValue = (int)fingerType;
            startT.FindPropertyRelative("boneType").intValue = (int)boneType;
            startT.FindPropertyRelative("position").vector3Value = boundTransform.localPosition;
            startT.FindPropertyRelative("rotation").vector3Value = boundTransform.localRotation.eulerAngles;
        }

        /// <summary>
        /// Set the Editor Pose
        /// </summary>
        private void EditorHandPose() {
            if(myTarget != null && myTarget.enabled) {
                MakeLeapHand(myTarget);

                if(myTarget.SetEditorPose) {
                    if(myTarget.GetLeapHand() == null) {
                        myTarget.InitHand();
                        myTarget.BeginHand();
                        myTarget.UpdateHand();
                    }
                    else {
                        myTarget.UpdateHand();
                    }
                }
            }
        }

        /// <summary>
        /// Makes a new leap hand so we can use it to set an editor pose
        /// </summary>
        /// <param name="binder"></param>
        private void MakeLeapHand(HandBinder binder) {
            LeapProvider provider = null;

            //First try to get the provider from a parent HandModelManager
            if(binder.transform.parent != null) {
                var manager = binder.transform.parent.GetComponent<HandModelManager>();
                if(manager != null) {
                    provider = manager.leapProvider;
                }
            }

            //If not found, use any old provider from the Hands.Provider getter
            if(provider == null) {
                provider = Hands.Provider;
            }

            Hand hand = null;
            //If we found a provider, pull the hand from that
            if(provider != null) {
                var frame = provider.CurrentFrame;

                if(frame != null) {
                    hand = frame.Get(binder.Handedness);
                }
            }

            //If we still have a null hand, construct one manually
            if(hand == null) {
                hand = TestHandFactory.MakeTestHand(binder.Handedness == Chirality.Left, unitType: TestHandFactory.UnitType.LeapUnits);
                hand.Transform(binder.transform.GetLeapMatrix());
            }

            binder.LeapHand = hand;
        }

        /// <summary>
        /// Draw extra gizmos in the scene to help the user while they edit variables
        /// </summary>
        private void OnSceneGUI() {
            myTarget = (HandBinder)target;

            //Update the editor pose, this will only get called when the object is selected.
            if(!Application.isPlaying) {
                EditorHandPose();
            }

            //Draw the leap hand
            if(myTarget.DebugLeapHand) {
                Handles.color = leapHandDebugCol;

                foreach(var finger in myTarget.LeapHand.Fingers) {
                    var index = 0;

                    foreach(var bone in finger.bones) {
                        Handles.SphereHandleCap(-1, bone.PrevJoint.ToVector3(), Quaternion.identity, myTarget.GizmoSize, EventType.Repaint);
                        if((index + 1) <= finger.bones.Length - 1)
                            Handles.DrawLine(finger.bones[index].PrevJoint.ToVector3(), finger.bones[index + 1].PrevJoint.ToVector3());
                        index++;
                    }
                }
                Handles.SphereHandleCap(-1, myTarget.LeapHand.WristPosition.ToVector3(), Quaternion.identity, myTarget.GizmoSize, EventType.Repaint);
            }

            //Draw the bound Gameobjects
            if(myTarget.DebugModelTransforms) {
                Handles.color = handModelDebugCol;

                for(int i = 0; i < myTarget.BoundGameobjects.Length; i++) {
                    //if(i % 4 == 0 && !myTarget.UseMetaBones && i != myTarget.BoundGameobjects.Length -1)
                    //    continue;

                    if(myTarget.BoundGameobjects[i] != null) {
                        var target = myTarget.BoundGameobjects[i].transform;

                        if(myTarget.DebugModelTransforms) {
                            Handles.DrawWireDisc(target.position, target.right, gizmoSize.floatValue);
                            Handles.DrawWireDisc(target.position, target.up, gizmoSize.floatValue);
                            Handles.DrawWireDisc(target.position, target.forward, gizmoSize.floatValue);
                        }
                    }
                }
            }

            //Draw helpful gizmos to show how much the hand has been offset from its original position
            Handles.color = new Color(43, 43, 43);
            for(int i = 0; i < myTarget.Offsets.Count; i++) {
                var offset = myTarget.Offsets[i];
                var id = HandBinderAutoRigger.TypeToIndex(offset.fingerType, offset.boneType);
                var boundObject = myTarget.BoundGameobjects[id];
                var startTransform = myTarget.StartTransforms[id];

                if(boundObject != null) {
                    var originPosition = boundObject.transform.TransformPoint(startTransform.position - boundObject.localPosition);
                    Handles.SphereHandleCap(-1, originPosition, Quaternion.identity, myTarget.GizmoSize, EventType.Repaint);

                    Handles.DrawDottedLine(originPosition, boundObject.position, 6f);
                }
            }
        }
    }
}