/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Leap.Unity.HandsModule
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(HandBinder))]
    public class HandBinderEditor : Editor
    {
        private HandBinder myTarget;

        private Color handModelDebugCol = Color.green;
        private Color leapHandDebugCol = Color.black;
        private Color previousCol = Color.white;

        private Texture dividerLine;

        private SerializedProperty chirality;
        private SerializedProperty debugLeapHand;
        private SerializedProperty DebugLeapRotationAxis;
        private SerializedProperty gizmoSize;
        private SerializedProperty debugModelTransforms;
        private SerializedProperty DebugModelRotationAxis;
        private SerializedProperty setPositions;
        private SerializedProperty setScale;
        private SerializedProperty scaleSpeed;
        private SerializedProperty setEditorPose;
        private SerializedProperty globalFingerRotationOffset;
        private SerializedProperty wristRotationOffset;
        private SerializedProperty useScaleToPositionElbow;
        private SerializedProperty boundHand;
        private SerializedProperty offsets;
        private SerializedProperty fineTuning;
        private SerializedProperty debugOptions;
        private SerializedProperty leapProvider;
        private SerializedProperty useMetaBones;
        private SerializedProperty scaleOffset;
        private SerializedProperty elbowOffset;

        private Color green = new Color32(140, 234, 40, 255);
        private GUISkin editorSkin;

        /// <summary>
        /// Assign the serialized properties
        /// </summary>
        private void SetSerializedProperties()
        {
            chirality = serializedObject.FindProperty("Chirality");
            debugLeapHand = serializedObject.FindProperty("DebugLeapHand");
            DebugLeapRotationAxis = serializedObject.FindProperty("DebugLeapRotationAxis");
            gizmoSize = serializedObject.FindProperty("GizmoSize");
            debugModelTransforms = serializedObject.FindProperty("DebugModelTransforms");
            DebugModelRotationAxis = serializedObject.FindProperty("DebugModelRotationAxis");
            setPositions = serializedObject.FindProperty("SetPositions");
            setScale = serializedObject.FindProperty("SetModelScale");
            scaleSpeed = serializedObject.FindProperty("ScalingSpeedMultiplier");
            setEditorPose = serializedObject.FindProperty("SetEditorPose");
            globalFingerRotationOffset = serializedObject.FindProperty("GlobalFingerRotationOffset");
            wristRotationOffset = serializedObject.FindProperty("WristRotationOffset");
            useScaleToPositionElbow = serializedObject.FindProperty("UseScaleToPositionElbow");
            fineTuning = serializedObject.FindProperty("FineTuning");
            debugOptions = serializedObject.FindProperty("DebugOptions");
            boundHand = serializedObject.FindProperty("BoundHand");
            offsets = serializedObject.FindProperty("Offsets");
            leapProvider = serializedObject.FindProperty("_leapProvider");
            scaleOffset = boundHand.FindPropertyRelative("scaleOffset");
            elbowOffset = boundHand.FindPropertyRelative("elbowOffset");
            useMetaBones = serializedObject.FindProperty("UseMetaBones");

            dividerLine = Resources.Load<Texture>("EditorDividerLine");
            editorSkin = Resources.Load<GUISkin>("UltraleapEditorStyle");
        }

        private void OnEnable()
        {
            serializedObject.Update();
            myTarget = (HandBinder)target;

            if (myTarget.gameObject.name.ToUpper().Contains("Left".ToUpper()))
            {
                myTarget.Handedness = Unity.Chirality.Left;
            }
            if (myTarget.gameObject.name.ToUpper().Contains("Right".ToUpper()))
            {
                myTarget.Handedness = Unity.Chirality.Right;
            }

            SetSerializedProperties();
        }

        /// <summary>
        /// Draw the inspector GUI
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            previousCol = GUI.color;

            DrawBindHandButton();
            DrawBindingOptions();
            DrawDebugOptions();
            DrawFineTuningOptions();

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Draw the Bind Hand Button
        /// </summary>
        private void DrawBindHandButton()
        {
            //Only draw the Bind hand button if you have 1 object selected
            if (Selection.gameObjects.Length == 1 && GUILayout.Button("Bind Hand", editorSkin.button, GUILayout.Height(40)))
            {
                var window = (BindHandWindow)EditorWindow.GetWindow(typeof(BindHandWindow));
                window.SetUp(ref myTarget);
                window.titleContent = new GUIContent("Binding Window");
                window.autoRepaintOnSceneChange = true;
                window.Show();

                //Set the size of the window equal to the size of the hand texture
                var handTexture = Resources.Load<Texture>("EditorHand");
            }
            EditorGUILayout.Space();
        }

        /// <summary>
        /// Draw the GUI for the binding options
        /// </summary>
        private void DrawBindingOptions()
        {
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(leapProvider, editorSkin);

            EditorGUILayout.PropertyField(chirality, new GUIContent("Hand Type", "Which hand does this binder target?"), editorSkin);

            EditorGUILayout.Space();

            setEditorPose.boolValue = GUILayout.Toggle(setEditorPose.boolValue, new GUIContent("Set Hand Pose In Editor", "Should the Leap Editor Pose be used during Edit mode?"), editorSkin.toggle);

            //If the hand has meta bones display the option to toggle them on and off
            if (myTarget.BoundHand.fingers[1].boundBones[0].boundTransform != null)
            {
                useMetaBones.boolValue = GUILayout.Toggle(useMetaBones.boolValue, new GUIContent("Use Metacarpal bones", "Does this model have weighted metacarpal bones you want to move and rotate?"), editorSkin.toggle);
            }

            setPositions.boolValue = GUILayout.Toggle(setPositions.boolValue, new GUIContent("Match Joint Positions With Tracking Data", "Does this binding require the positional leap data to be applied to the 3D model?"), editorSkin.toggle);

            setScale.boolValue = GUILayout.Toggle(setScale.boolValue, new GUIContent("Scale Model to Tracking Data", "Should the hand binder adjust the models scale?"), editorSkin.toggle);

            if (setScale.boolValue)
            {
                EditorGUILayout.PropertyField(scaleSpeed, editorSkin);
            }

            EditorGUILayout.Space();
            GUILayout.Label(dividerLine);
            EditorGUILayout.Space();
        }

        /// <summary>
        /// Draw the GUI for displaying extra content for debugging
        /// </summary>
        private void DrawDebugOptions()
        {
            //Draw the debugging options toggle
            var buttonName = !debugOptions.boolValue ? "Show Debug Options" : "Hide Debug Options";
            debugOptions.boolValue = GUILayout.Toggle(debugOptions.boolValue, buttonName, editorSkin.button);

            EditorGUILayout.Space();
            if (debugOptions.boolValue)
            {
                EditorGUILayout.Space();

                DrawToggleColorField(debugLeapHand, ref leapHandDebugCol);
                DrawToggleColorField(debugModelTransforms, ref handModelDebugCol);

                DebugLeapRotationAxis.boolValue = GUILayout.Toggle(DebugLeapRotationAxis.boolValue, new GUIContent(DebugLeapRotationAxis.name), editorSkin.toggle);
                DebugModelRotationAxis.boolValue = GUILayout.Toggle(DebugModelRotationAxis.boolValue, new GUIContent(DebugModelRotationAxis.name), editorSkin.toggle);

                EditorGUILayout.PropertyField(gizmoSize, editorSkin);
                EditorGUILayout.Space();
                GUILayout.Label(dividerLine);
                EditorGUILayout.Space();
            }

            /// <summary>
            /// Create a toggle that displays colour when active
            /// </summary>
            void DrawToggleColorField(SerializedProperty toggleProperty, ref Color colorField)
            {
                GUILayout.BeginVertical();
                toggleProperty.boolValue = GUILayout.Toggle(toggleProperty.boolValue, new GUIContent(toggleProperty.name), editorSkin.toggle);
                if (toggleProperty.boolValue)
                {
                    colorField = EditorGUILayout.ColorField(new GUIContent("Color"), colorField, true, false, false);
                }
                GUILayout.EndVertical();
                serializedObject.ApplyModifiedProperties();
            }
        }

        /// <summary>
        /// Draw the options for the user to fine tune the hand binder
        /// </summary>
        private void DrawFineTuningOptions()
        {
            //Draw the fine tuning options
            fineTuning.boolValue = GUILayout.Toggle(fineTuning.boolValue, !fineTuning.boolValue ? "Show Fine Tuning Options" : "Hide Fine Tuning Options", editorSkin.button);
            EditorGUILayout.Space();
            if (fineTuning.boolValue)
            {
                EditorGUILayout.Space();
                GUI.color = Color.white;

                //Draw the Calculated Offsets for the wrist and Fingers
                GUILayout.BeginVertical(editorSkin.box);
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(wristRotationOffset, new GUIContent("Wrist Rotation Offset", "Adjusting this value will modify how the 3D Models wrist is rotated in relation to the tracking data"), editorSkin);
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(globalFingerRotationOffset, new GUIContent("Fingers Rotation Offset", "Adjusting this value will modify how the 3D Models fingers are rotated in relation to the tracking data"), editorSkin);
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(useScaleToPositionElbow, new GUIContent("Use Scale To Position Elbow", "Moves the elbow so that when the forearm scales the bone doesnt clip into the hand model"), editorSkin);
                GUI.color = previousCol;
                GUILayout.EndVertical();

                EditorGUILayout.Space();
                GUILayout.Label(dividerLine);
                EditorGUILayout.Space();

                if (myTarget.BoundHand.baseScale == 0)
                {
                    EditorGUILayout.Space();
                    GUILayout.Label("Rebind the hand to enable scaling");
                    GUI.enabled = false;
                }
                else
                {
                    GUI.enabled = true;
                }
                if (setScale.boolValue && leapProvider.objectReferenceValue != null)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(scaleOffset, new GUIContent("Model Scale Offset", "The hand scale will be modified by this amount"));

                    GUI.enabled = myTarget.BoundHand.elbow.boundTransform != null;

                    EditorGUILayout.PropertyField(elbowOffset, new GUIContent("Elbow Scale Offset", "The Elbow Length will be modified by this amount"));

                    GUI.enabled = true;

                    for (int i = 0; i < myTarget.BoundHand.fingers.Length; i++)
                    {
                        var offset = boundHand.FindPropertyRelative("fingers").GetArrayElementAtIndex(i).FindPropertyRelative("fingerTipScaleOffset");
                        var fingerType = ((Finger.FingerType)i).ToString().Remove(0, 5).ToString();
                        EditorGUILayout.PropertyField(offset, new GUIContent(fingerType + " Tip Offset", "The hand finger tip scale will be modified by this amount"));
                    }
                }

                GUI.enabled = true;


                EditorGUILayout.Space();
                GUILayout.Label(dividerLine);
                EditorGUILayout.Space();


                DrawBoneOffsets();
                DrawAddBoneOffsetButton();

                EditorGUILayout.Space();
                GUILayout.Label(dividerLine);
                EditorGUILayout.Space();
            }
        }

        /// <summary>
        /// Draw any bone offsets that the user want to set up
        /// </summary>
        void DrawBoneOffsets()
        {
            for (int offsetIndex = 0; offsetIndex < offsets.arraySize; offsetIndex++)
            {
                SerializedProperty boundType = offsets.GetArrayElementAtIndex(offsetIndex);
                BoundTypes previousBoundType = myTarget.Offsets[offsetIndex];
                SerializedProperty offsetProperty = BoundTypeToOffsetProperty((BoundTypes)boundType.intValue);
                SerializedProperty offsetRotation = offsetProperty.FindPropertyRelative("rotation");
                SerializedProperty offsetPosition = offsetProperty.FindPropertyRelative("position");

                GUILayout.BeginVertical(editorSkin.box);

                GUILayout.BeginHorizontal(editorSkin.box);
                EditorGUILayout.PropertyField(boundType, GUIContent.none, editorSkin);

                if (GUILayout.Button("-", editorSkin.button))
                {
                    if (boundType.intValue != (int)BoundTypes.WRIST)
                    {
                        offsetRotation.vector3Value = Vector3.zero;
                        offsetPosition.vector3Value = Vector3.zero;
                    }
                    offsets.DeleteArrayElementAtIndex(offsetIndex);
                    break;
                }
                GUILayout.EndHorizontal();

                //Check to see if the user has changed the value
                if ((int)previousBoundType != boundType.intValue)
                {
                    //Check to see if any of the offsets are the same as this one
                    if (myTarget.Offsets.Any(x => (int)x == boundType.intValue))
                    {
                        boundType.intValue = (int)previousBoundType;
                    }
                    else
                    {
                        offsetRotation.vector3Value = Vector3.zero;
                        offsetPosition.vector3Value = Vector3.zero;
                        offsetProperty = BoundTypeToOffsetProperty((BoundTypes)boundType.intValue);
                        offsetRotation = offsetProperty.FindPropertyRelative("rotation");
                        offsetPosition = offsetProperty.FindPropertyRelative("position");
                    }
                }

                EditorGUILayout.PropertyField(offsetPosition, editorSkin);
                EditorGUILayout.PropertyField(offsetRotation, editorSkin);
                GUILayout.EndVertical();
                EditorGUILayout.Space();
                GUILayout.Label(dividerLine);
                EditorGUILayout.Space();
            }
        }

        /// <summary>
        /// Draw the button to allow users to add a bone offset
        /// </summary>
        void DrawAddBoneOffsetButton()
        {
            GUILayout.BeginHorizontal(editorSkin.box);
            GUILayout.Label(new GUIContent("Add Bone Offset", "Add an extra offset for any bone"));
            if (GUILayout.Button("+", editorSkin.button))
            {
                if (offsets.arraySize < 22)
                {
                    offsets.InsertArrayElementAtIndex(offsets.arraySize);
                    var offset = offsets.GetArrayElementAtIndex(offsets.arraySize - 1);

                    var enumList = new List<int>();

                    for (int i = 0; i < 22; i++)
                    {
                        enumList.Add(i);
                    }

                    var result = enumList.Where(typeA => myTarget.Offsets.All(typeB => (int)typeB != typeA)).FirstOrDefault();

                    offset.intValue = result;
                    //Give the UI a chance to update
                    return;
                }
            }
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Convert a BoundType to the offsetProperty for the bound transform
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private SerializedProperty BoundTypeToOffsetProperty(BoundTypes boundType)
        {
            if (boundType == BoundTypes.WRIST)
            {
                return boundHand.FindPropertyRelative("wrist").FindPropertyRelative("offset");
            }
            else if (boundType == BoundTypes.ELBOW)
            {
                return boundHand.FindPropertyRelative("elbow").FindPropertyRelative("offset");
            }
            else if (!HandBinderUtilities.boundTypeMapping.ContainsKey(boundType))
            {
                return null;
            }

            (Finger.FingerType fingerType, Bone.BoneType boneType) fingerBoneType = HandBinderUtilities.boundTypeMapping[boundType];
            return FingerOffsetPropertyFromLeapTypes(fingerBoneType.fingerType, fingerBoneType.boneType);
        }

        private SerializedProperty FingerOffsetPropertyFromLeapTypes(Finger.FingerType fingerType, Bone.BoneType boneType)
        {
            return boundHand.FindPropertyRelative("fingers").GetArrayElementAtIndex((int)fingerType).FindPropertyRelative("boundBones").GetArrayElementAtIndex((int)boneType).FindPropertyRelative("offset");
        }

        /// <summary>
        /// Draw extra gizmos in the scene to help the user while they edit variables
        /// </summary>
        private void OnSceneGUI()
        {
            myTarget = (HandBinder)target;

            if (myTarget.LeapHand == null)
            {
                return;
            }

            DrawLeapHandGizmos();
            DrawLeapBasis();
            DrawModelHandGizmos();
            DrawModelHandBasis();
        }

        /// <summary>
        /// Draw the editor gizmos that will help explain where the Bound hand joints are in the scene
        /// </summary>
        void DrawModelHandGizmos()
        {
            //Draw the bound Gameobjects
            if (debugModelTransforms.boolValue)
            {
                Handles.color = handModelDebugCol;

                foreach (var FINGER in myTarget.BoundHand.fingers)
                {
                    var index = 0;

                    foreach (var BONE in FINGER.boundBones)
                    {
                        var target = BONE.boundTransform;

                        if (target != null)
                        {
                            if ((index + 1) <= 3)
                            {
                                var joint = FINGER.boundBones[index + 1];
                                if (joint.boundTransform != null)
                                {
                                    Handles.DrawAAPolyLine(target.position, joint.boundTransform.position);
                                }
                            }

                            Handles.DrawWireDisc(target.position, target.right, gizmoSize.floatValue);
                            Handles.DrawWireDisc(target.position, target.up, gizmoSize.floatValue);
                            Handles.DrawWireDisc(target.position, target.forward, gizmoSize.floatValue);
                        }
                        index++;
                    }
                }

                //Draw the wrist Gizmo
                if (myTarget.BoundHand.wrist.boundTransform != null)
                {
                    var target = myTarget.BoundHand.wrist.boundTransform;
                    Handles.DrawWireDisc(target.position, target.right, gizmoSize.floatValue);
                    Handles.DrawWireDisc(target.position, target.up, gizmoSize.floatValue);
                    Handles.DrawWireDisc(target.position, target.forward, gizmoSize.floatValue);
                }

                //Draw the wrist Gizmo
                if (myTarget.BoundHand.elbow.boundTransform != null)
                {
                    var target = myTarget.BoundHand.elbow.boundTransform;
                    Handles.DrawWireDisc(target.position, target.right, gizmoSize.floatValue);
                    Handles.DrawWireDisc(target.position, target.up, gizmoSize.floatValue);
                    Handles.DrawWireDisc(target.position, target.forward, gizmoSize.floatValue);
                }

                var wrist = myTarget.BoundHand.wrist.boundTransform;

                if (wrist != null)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        var indexCheck = (int)Bone.BoneType.TYPE_METACARPAL;

                        //The hand binder does not use the METACARPAL bone for the thumb so draw a line to the proximal instead 
                        if ((Leap.Finger.FingerType)i == Finger.FingerType.TYPE_THUMB)
                        {
                            indexCheck = (int)Bone.BoneType.TYPE_PROXIMAL;
                        }

                        var joint = myTarget.BoundHand.fingers[i];
                        var bone = joint.boundBones[indexCheck].boundTransform;

                        if (bone != null)
                        {
                            Handles.DrawAAPolyLine(wrist.position, bone.position);
                        }

                    }
                    Handles.SphereHandleCap(-1, wrist.position, Quaternion.identity, gizmoSize.floatValue, EventType.Repaint);
                    Handles.DrawAAPolyLine(wrist.position, myTarget.LeapHand.Arm.PrevJoint);
                    Handles.SphereHandleCap(-1, myTarget.LeapHand.Arm.PrevJoint, Quaternion.identity, gizmoSize.floatValue, EventType.Repaint);
                }
            }
        }

        /// <summary>
        /// Draw the editor gizmos that will help explain where the leap hand joints are in the scene
        /// </summary>
        void DrawLeapHandGizmos()
        {
            //Draw the leap hand in the scene
            if (debugLeapHand.boolValue)
            {
                Handles.color = leapHandDebugCol;
                foreach (var finger in myTarget.LeapHand.Fingers)
                {
                    var index = 0;

                    foreach (var bone in finger.bones)
                    {
                        Handles.SphereHandleCap(-1, bone.PrevJoint, Quaternion.identity, gizmoSize.floatValue, EventType.Repaint);
                        if ((index + 1) <= finger.bones.Length - 1)
                        {
                            Handles.DrawAAPolyLine(finger.bones[index].PrevJoint, finger.bones[index + 1].PrevJoint);
                        }

                        index++;
                    }

                    Handles.DrawDottedLine(finger.bones.Last().PrevJoint, finger.TipPosition, 5);
                    Handles.SphereHandleCap(-1, finger.TipPosition, Quaternion.identity, gizmoSize.floatValue, EventType.Repaint);
                }

                Handles.SphereHandleCap(-1, myTarget.LeapHand.WristPosition, Quaternion.identity, gizmoSize.floatValue, EventType.Repaint);
                Handles.DrawAAPolyLine(myTarget.LeapHand.WristPosition, myTarget.LeapHand.Fingers[0].bones[0].PrevJoint);
                Handles.DrawAAPolyLine(myTarget.LeapHand.WristPosition, myTarget.LeapHand.Fingers[1].bones[0].PrevJoint);
                Handles.DrawAAPolyLine(myTarget.LeapHand.WristPosition, myTarget.LeapHand.Fingers[2].bones[0].PrevJoint);
                Handles.DrawAAPolyLine(myTarget.LeapHand.WristPosition, myTarget.LeapHand.Fingers[3].bones[0].PrevJoint);
                Handles.DrawAAPolyLine(myTarget.LeapHand.WristPosition, myTarget.LeapHand.Fingers[4].bones[0].PrevJoint);
                Handles.DrawAAPolyLine(myTarget.LeapHand.WristPosition, myTarget.LeapHand.Arm.PrevJoint);
                Handles.SphereHandleCap(-1, myTarget.LeapHand.Arm.PrevJoint, Quaternion.identity, gizmoSize.floatValue, EventType.Repaint);
            }
        }

        /// <summary>
        /// Draw some gizmos to help explain the Leap Rotation Axis for the entire hand
        /// </summary>
        void DrawLeapBasis()
        {
            if (DebugLeapRotationAxis.boolValue)
            {
                foreach (var FINGER in myTarget.LeapHand.Fingers)
                {
                    foreach (var BONE in FINGER.bones)
                    {
                        DrawLeapBoneBasis(BONE, gizmoSize.floatValue * 2);
                    }
                }
            }
        }

        /// <summary>
        ///Draw some gizmos to help explain the Models Rotation Axis for the entire hand
        /// </summary>
        void DrawModelHandBasis()
        {
            if (DebugModelRotationAxis.boolValue)
            {
                foreach (var FINGER in myTarget.BoundHand.fingers)
                {
                    foreach (var BONE in FINGER.boundBones)
                    {
                        if (BONE.boundTransform != null)
                        {
                            DrawTransformBasis(BONE.boundTransform, gizmoSize.floatValue * 2);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Draw some gizmos to help explain the rotation axis that leap uses
        /// </summary>
        /// <param name="bone"></param>
        /// <param name="size"></param>
        private void DrawLeapBoneBasis(Leap.Bone bone, float size)
        {
            Vector3 middle, y, x, z;

            middle = bone.PrevJoint;
            y = bone.Basis.xBasis;
            x = bone.Basis.yBasis;
            z = bone.Basis.zBasis;

            Handles.color = Color.green;
            Handles.DrawLine(middle, middle + y.normalized * size);
            Handles.color = Color.red;
            Handles.DrawLine(middle, middle + x.normalized * size);
            Handles.color = Color.blue;
            Handles.DrawLine(middle, middle + z.normalized * size);
            Handles.color = leapHandDebugCol;
        }

        /// <summary>
        /// Draw some gizmos to help explain the rotation axis for the bound hand
        /// </summary>
        /// <param name="bone"></param>
        /// <param name="size"></param>
        private void DrawTransformBasis(Transform bone, float size)
        {
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

        /// <summary>
        /// An editor window to display information about what transforms are attached to the hand
        /// </summary>
        public class BindHandWindow : EditorWindow
        {
            private Texture dividerLine;
            private HandBinder handBinder;
            private float spaceSize = 30f;
            private GUISkin editorSkin;
            private string message1 = "Reference the GameObjects you wish to use from the scene into the fields below, once assigned the dots above will appear green to show they are bound to tracking data.";
            private string message2 = "Once you have assigned the bones you wish to use, the button below will bind the 3D Model  to the tracking data.";
            private Vector2 scrollPosition;

            /// <summary>
            /// Set up the Editor textures and reference to the hand binder
            /// </summary>
            /// <param name="handBinderRef"></param>
            public void SetUp(ref HandBinder handBinderRef)
            {
                handBinder = handBinderRef;
                dividerLine = Resources.Load<Texture>("EditorDividerline");
                editorSkin = Resources.Load<GUISkin>("UltraleapEditorStyle");
            }

            /// <summary>
            //Close this window if the user selects another gameobject with the Hand Binder on it
            /// </summary>
            private void OnSelectionChange()
            {

                if (Selection.activeTransform != null)
                {
                    var selectedHandBinder = Selection.activeTransform.GetComponent<HandBinder>();
                    if (selectedHandBinder != null && selectedHandBinder != handBinder)
                    {
                        Close();
                    }
                }

                Repaint();
            }

            void OnGUI()
            {
                GUIHandGraphic.DrawHandGraphic(handBinder.Handedness, GUIHandGraphic.FlattenHandBinderTransforms(handBinder), handBinder);
                DrawAutoBindButton();
                DrawObjectFields();
                DrawRotationOffsets();
                EditorUtility.SetDirty(this);
            }

            /// <summary>
            /// Draw a button to allow the user to automatically bind the hand
            /// </summary>
            void DrawAutoBindButton()
            {
                if (GUILayout.Button(new GUIContent("Auto Bind", "Automatically try to search and bind the hand"), editorSkin.button, GUILayout.MaxWidth(EditorGUIUtility.currentViewWidth), GUILayout.MinHeight(spaceSize)))
                {
                    if (EditorUtility.DisplayDialog("Auto Bind",
                        "Are you sure you want to discard all your changes and run the Auto Bind process?", "Yes", "No"))
                    {

                        Undo.RegisterFullObjectHierarchyUndo(handBinder, "AutoBind");
                        Undo.undoRedoPerformed += AutoRigUndo;
                        HandBinderAutoBinder.AutoBind(handBinder);
                        handBinder.UpdateHand();
                    }
                }

                GUILayout.Label(dividerLine);
                GUILayout.Label(message1, editorSkin.label);
            }

            /// <summary>
            /// Undo the previous binding applied to the hand
            /// </summary>
            public void AutoRigUndo()
            {
                Close();
                handBinder.ResetHand(true);
                Undo.undoRedoPerformed -= AutoRigUndo;
            }

            /// <summary>
            /// Draw the fields that will display information regarding which transform in the scene is attached to which leap data point
            /// </summary>
            void DrawObjectFields()
            {
                scrollPosition = GUILayout.BeginScrollView(scrollPosition);
                //Draw a list of all the points of the hand that can be bound too
                GUILayout.Space(spaceSize);
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.BeginVertical();

                //Draw the wrist bone object field
                DrawObjectField("WRIST : ", ref handBinder.BoundHand.wrist);

                GUILayout.BeginHorizontal();
                string length = handBinder.BoundHand.baseScale.ToString();
                GUILayout.Label("HAND LENGTH", editorSkin.label);
                GUILayout.Label(length, editorSkin.label, GUILayout.MaxWidth(EditorGUIUtility.labelWidth * 2));
                GUILayout.EndHorizontal();

                GUILayout.Space(spaceSize);

                for (int fingerID = 0; fingerID < handBinder.BoundHand.fingers.Length; fingerID++)
                {
                    var fingerType = ((Finger.FingerType)fingerID).ToString().Remove(0, 5).ToString();
                    var objectFieldName = "";
                    for (int boneID = 0; boneID < handBinder.BoundHand.fingers[fingerID].boundBones.Length; boneID++)
                    {
                        if ((Finger.FingerType)fingerID == Finger.FingerType.TYPE_THUMB && (Bone.BoneType)boneID == Bone.BoneType.TYPE_METACARPAL)
                        {
                            continue;
                        }

                        var boneType = ((Bone.BoneType)boneID).ToString().Remove(0, 5).ToString();

                        objectFieldName = ((fingerType + " " + boneType + " :").ToString());
                        DrawObjectField(objectFieldName, ref handBinder.BoundHand.fingers[fingerID].boundBones[boneID], true, fingerID, boneID);
                    }


                    GUILayout.BeginHorizontal();
                    string fingerLength = handBinder.BoundHand.fingers[fingerID].fingerTipBaseLength.ToString();
                    GUILayout.Label("FINGER LENGTH", editorSkin.label);
                    GUILayout.Label(fingerLength, editorSkin.label, GUILayout.MaxWidth(EditorGUIUtility.labelWidth * 2));
                    GUILayout.EndHorizontal();
                    GUILayout.Space(spaceSize);
                }

                //Draw the Elbow bone object field
                DrawObjectField("Elbow : ", ref handBinder.BoundHand.elbow);
                GUILayout.Space(spaceSize);

                GUILayout.EndVertical();
                GUILayout.Space(20);
                GUILayout.EndHorizontal();
                GUILayout.EndScrollView();

            }

            /// <summary>
            /// Draw the object field that will allow the user to drag a transform from the scene and attach it to the hand binder data
            /// </summary>
            /// <param name="name"></param>
            /// <param name="boundBone"></param>
            /// <param name="autoAssignChildren"></param>
            /// <param name="fingerID"></param>
            /// <param name="boneID"></param>
            void DrawObjectField(string name, ref BoundBone boundBone, bool autoAssignChildren = false, int fingerID = 0, int boneID = 0)
            {
                GUILayout.BeginHorizontal();
                GUI.color = boundBone.boundTransform != null ? Color.green : Color.white;

                GUILayout.Label(name, editorSkin.label);
                GUI.color = Color.white;
                var newTransform = (Transform)EditorGUILayout.ObjectField(boundBone.boundTransform, typeof(Transform), true, GUILayout.MaxWidth(EditorGUIUtility.labelWidth * 2));
                if (newTransform != boundBone.boundTransform)
                {
                    Undo.RegisterFullObjectHierarchyUndo(handBinder, "Bound Object");
                    boundBone = HandBinderAutoBinder.AssignBoundBone(newTransform);

                    if (boundBone.boundTransform != null)
                    {
                        if (autoAssignChildren)
                        {
                            AutoAssignChildrenBones(newTransform, fingerID, boneID);
                        }
                    }
                }
                GUILayout.EndHorizontal();
            }

            /// <summary>
            /// Automatically assign any children of the selected transform to the hand binder for the user
            /// </summary>
            /// <param name="newT"></param>
            /// <param name="fingerID"></param>
            /// <param name="boneID"></param>
            private void AutoAssignChildrenBones(Transform newT, int fingerID, int boneID)
            {
                var firstChildList = new List<Transform>() { newT };
                firstChildList = GetFirstChildren(newT, ref firstChildList);
                for (int i = 0; i < firstChildList.Count; i++)
                {
                    if (boneID + i <= 3)
                    {
                        handBinder.BoundHand.fingers[fingerID].boundBones[boneID + i] = HandBinderAutoBinder.AssignBoundBone(firstChildList[i]);
                    }
                }
            }

            /// <summary>
            /// Get the first child of this transform, if it has more children continue adding children to the children list
            /// </summary>
            /// <param name="child"></param>
            /// <param name="firstChildren"></param>
            /// <returns></returns>
            List<Transform> GetFirstChildren(Transform child, ref List<Transform> firstChildren)
            {
                if (child.childCount > 0)
                {
                    firstChildren.Add(child.GetChild(0));
                    return GetFirstChildren(child.GetChild(0), ref firstChildren);
                }

                else
                {
                    return firstChildren;
                }
            }

            /// <summary>
            /// Draw any rotation offsets into the window
            /// </summary>
            void DrawRotationOffsets()
            {
                GUILayout.Label(dividerLine);
                GUILayout.Label(message2, editorSkin.label);
                if (GUILayout.Button("Bind Hand", editorSkin.button, GUILayout.MaxWidth(EditorGUIUtility.currentViewWidth), GUILayout.MinHeight(spaceSize)))
                {
                    if (EditorUtility.DisplayDialog("Bind Hand",
                       "Are you sure you want to recalculate the hand binding ?", "Yes", "No"))
                    {
                        handBinder.ResetHand();
                        Undo.RegisterFullObjectHierarchyUndo(handBinder.gameObject, "Bind Hand");
                        HandBinderAutoBinder.BindHand(handBinder);

                        handBinder.UpdateHand();
                    }
                }
            }
        }

        /// <summary>
        /// Contains the set up required to draw a picture of the hand in the GUI
        /// </summary>
        public class GUIHandGraphic
        {
            static public Texture handTexture, dotTexture;
            static public Vector2[] handPoints = new Vector2[]
            {
                //Thumb
                Vector2.Lerp(new Vector2(-20.9F, 51), new Vector2(-94.2f, 146.9f), 0),
                Vector2.Lerp(new Vector2(-20.9F, 51), new Vector2(-94.2f, 146.9f), .2f),
                Vector2.Lerp(new Vector2(-20.9F, 51), new Vector2(-94.2f, 146.9f), .5f),
                Vector2.Lerp(new Vector2(-20.9F, 51), new Vector2(-94.2f, 146.9f), .8f),
                //Vector2.Lerp(new Vector2(-20.9F, 51), new Vector2(-94.2f, 146.9f), 1),
                
                //Index
                Vector2.Lerp(new Vector2(-7.1f, 89.37f), new Vector2(0.9f, 229.8f), 0),
                Vector2.Lerp(new Vector2(-7.1f, 89.37f), new Vector2(0.9f, 229.8f), .45f),
                Vector2.Lerp(new Vector2(-7.1f, 89.37f), new Vector2(0.9f, 229.8f), .65f),
                Vector2.Lerp(new Vector2(-7.1f, 89.37f), new Vector2(0.9f, 229.8f), .85f),
                //Vector2.Lerp(new Vector2(-7.1f, 89.37f), new Vector2(0.9f, 229.8f), 1),

                //Middle
                Vector2.Lerp(new Vector2(17.5f, 99.4f), new Vector2(51.6f, 229.2f), 0),
                Vector2.Lerp(new Vector2(17.5f, 99.4f), new Vector2(51.6f, 229.2f), .4f),
                Vector2.Lerp(new Vector2(17.5f, 99.4f), new Vector2(51.6f, 229.2f), .6f),
                Vector2.Lerp(new Vector2(17.5f, 99.4f), new Vector2(51.6f, 229.2f), .8f),
                //Vector2.Lerp(new Vector2(17.5f, 99.4f), new Vector2(51.6f, 229.2f), 1),

                //Ring
                Vector2.Lerp(new Vector2(33.2f, 82.3f), new Vector2(91.3f, 200f), 0),
                Vector2.Lerp(new Vector2(33.2f, 82.3f), new Vector2(91.3f, 200f), .4f),
                Vector2.Lerp(new Vector2(33.2f, 82.3f), new Vector2(91.3f, 200f), .6f),
                Vector2.Lerp(new Vector2(33.2f, 82.3f), new Vector2(91.3f, 200f), .8f),
                //Vector2.Lerp(new Vector2(33.2f, 82.3f), new Vector2(91.3f, 200f), 1),

                //Pinky
                Vector2.Lerp(new Vector2(39.6f, 53.9f), new Vector2(125, 138.01f), 0),
                Vector2.Lerp(new Vector2(75.4f, 98.6f), new Vector2(125, 138.01f), 0),
                Vector2.Lerp(new Vector2(75.4f, 98.6f), new Vector2(125, 138.01f), .4f),
                Vector2.Lerp(new Vector2(75.4f, 98.6f), new Vector2(125, 138.01f), .7f),
                //Vector2.Lerp(new Vector2(75.4f, 98.6f), new Vector2(125, 138.01f), 1),

                //Wrist
                new Vector2(0, 0),
            };

            /// <summary>
            /// Turn the bound handBinder bones into a flattened array
            /// </summary>
            /// <param name="handBinder"></param>
            /// <returns></returns>
            static public Transform[] FlattenHandBinderTransforms(HandBinder handBinder)
            {
                var bones = new List<Transform>();
                int index = 0;
                for (int FINGERID = 0; FINGERID < handBinder.BoundHand.fingers.Length; FINGERID++)
                {
                    for (int BONEID = 0; BONEID < handBinder.BoundHand.fingers[FINGERID].boundBones.Length; BONEID++)
                    {
                        var BONE = handBinder.BoundHand.fingers[FINGERID].boundBones[BONEID];
                        bones.Add(BONE.boundTransform);
                        index++;
                    }
                    //bones.Add(handBinder.BoundHand.fingers[FINGERID].fingerTip.boundTransform);
                    index++;
                }
                bones.Add(handBinder.BoundHand.wrist.boundTransform);
                return bones.ToArray();
            }

            /// <summary>
            /// Set up the editor textures
            /// </summary>
            static public void SetUp()
            {
                handTexture = Resources.Load<Texture>("EditorHand");
                dotTexture = EditorGUIUtility.IconContent("sv_icon_dot0_pix16_gizmo").image;
            }

            /// <summary>
            /// Draw the hand graphic to the GUI
            /// </summary>
            /// <param name="handedness"></param>
            /// <param name="bones"></param>
            static public void DrawHandGraphic(Chirality handedness, Transform[] bones = null, HandBinder handBinder = null)
            {
                if (handTexture == null || dotTexture == null)
                {
                    SetUp();
                }

                var midPoint = Screen.width / 2f;
                var middleYOffset = 50;

                //Draw the hand texture
                var handTextureRect = new Rect(midPoint, middleYOffset, handTexture.width, handTexture.height);
                if (handedness == Unity.Chirality.Left)
                {
                    handTextureRect.x -= handTexture.width / 2;
                }
                else
                {
                    handTextureRect.x += handTexture.width / 2;
                    handTextureRect.size = new Vector2(-handTextureRect.size.x, handTextureRect.size.y);
                }

                GUI.DrawTextureWithTexCoords(handTextureRect, handTexture, new Rect(0, 0, 1, 1));

                for (int boneID = 0; boneID < bones.Length; boneID++)
                {
                    if (boneID == 0)
                    {
                        continue;
                    }

                    var bone = bones[boneID];
                    var isSelectedOrHovered = Selection.activeTransform == bone;

                    var pointRect = new Rect(midPoint, middleYOffset, handTexture.width, handTexture.height);

                    if (handedness == Unity.Chirality.Left)
                    {
                        pointRect.center -= handPoints[boneID];
                    }
                    else
                    {
                        var offset = handPoints[boneID] + Vector2.left * 25;
                        pointRect.center += new Vector2(offset.x, -offset.y);
                    }

                    GUI.color = bone != null ? Color.green : Color.grey;

                    if (bone != null)
                    {
                        if (isSelectedOrHovered)
                        {
                            GUI.DrawTextureWithTexCoords(pointRect, EditorGUIUtility.IconContent("DotFrameDotted").image, new Rect(0, 0, 11f, 11f));
                        }
                        else
                        {
                            GUI.DrawTextureWithTexCoords(pointRect, dotTexture, new Rect(0, 0, 11f, 11f));
                        }
                    }
                    else
                    {
                        GUI.DrawTextureWithTexCoords(pointRect, EditorGUIUtility.IconContent("DotFrameDotted").image, new Rect(0, 0, 11f, 11f));
                    }


                    GUI.color = Color.white;
                }

                GUILayout.Space(handTexture.height * 1.25f);
            }
        }
    }
}