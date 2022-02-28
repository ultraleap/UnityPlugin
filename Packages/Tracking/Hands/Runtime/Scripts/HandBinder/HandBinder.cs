/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2021.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Leap.Unity.HandsModule
{

    /// <summary>
    /// The HandBinder allows you to use your own hand models so that they follow the leap tracking data.
    /// You can bind your model by specifying transforms for the different joints and use the debug and fine tuning options to test and adjust it.
    /// </summary>
    [DisallowMultipleComponent]
    public class HandBinder : HandModelBase
    {
        #region Inspector
        /// <summary> 
        /// The data structure that contains transforms that get bound to the leap data 
        /// </summary>
        public BoundHand BoundHand = new BoundHand();

        /// <summary> 
        /// The length of the elbow to maintain the correct offset from the wrist 
        /// </summary>
        [Tooltip("The length of the elbow to maintain the correct offset from the wrist")]
        public float ElbowLength;

        /// <summary> 
        /// The rotation offset that will be assigned to all the fingers 
        /// </summary>
        [Tooltip("The rotation offset that will be assigned to all the fingers")]
        public Vector3 GlobalFingerRotationOffset;

        /// <summary> 
        /// The rotation offset that will be assigned to the wrist 
        /// </summary>
        [Tooltip("The rotation offset that will be assigned to the wrist")]
        public Vector3 WristRotationOffset;

        /// <summary> 
        /// Set the assigned transforms to the same position as the Leap Hand 
        /// </summary>
        [Tooltip("Set the assigned transforms to the same position as the Leap Hand")]
        public bool SetPositions = true;

        /// <summary> 
        /// Set the assigned transforms to the same position as the Leap Hand 
        /// </summary>
        [Tooltip("Should binding use metacarpal bones")]
        public bool UseMetaBones = true;

        /// <summary>
        /// Should the hand binder modify the scale of the hand
        /// </summary>
        public bool SetModelScale = true;

        /// <summary> 
        /// User defined offsets in editor script 
        /// </summary>
        public List<BoundTypes> Offsets = new List<BoundTypes>();

        /// <summary> 
        /// Stores all the children's default pose 
        /// </summary>
        public SerializedTransform[] DefaultHandPose;

        #endregion

        #region Hand Model Base

        /// <summary> 
        /// The chirality or handedness of the hand.
        /// Custom editor requires Chirality in a non overridden property, Public Chirality exists for the editor.
        /// </summary>
        public Chirality Chirality;

        /// <summary>
        /// The chirality or handedness of this hand (left or right).
        /// To set, change the public Chirality.
        /// </summary>
        public override Chirality Handedness { get { return Chirality; } set { } }
        /// <summary>
        /// The type of the Hand model (set to Graphics).
        /// </summary>
        public override ModelType HandModelType { get { return ModelType.Graphics; } }

        /// <summary>
        /// The Leap Hand object this hand model represents.
        /// </summary>
        public Hand LeapHand;

        /// <summary>
        /// Returns the Leap Hand object represented by this HandModelBase. 
        /// Note that any physical quantities and directions obtained from the Leap Hand object are 
        /// relative to the Leap Motion coordinate system, which uses a right-handed axes and units 
        /// of millimeters.
        /// </summary>
        /// <returns></returns>
        public override Hand GetLeapHand() { return LeapHand; }

        /// <summary>
        /// Assigns a Leap Hand object to this HandModelBase.
        /// </summary>
        /// <param name="hand"></param>
        public override void SetLeapHand(Hand hand) { LeapHand = hand; }

        #endregion

        #region Hand Binder Logic

        /// <summary>
        /// Called once per frame when the LeapProvider calls the event OnUpdateFrame.
        /// Update the BoundGameobjects so that the positions and rotations match that of the leap hand
        /// </summary>
        public override void UpdateHand()
        {
            if (!SetEditorPose && !Application.isPlaying)
            {
                return;
            }

            if (LeapHand != null && BoundHand != null)
            {
                SetHandScale();
                TransformElbow();
                TransformWrist();
                TransformFingerBones();
                EditPoseNeedsResetting = true;
            }
        }

        /// <summary>
        /// Set the hand model scale based on the CalculatedRatio()
        /// </summary>
        void SetHandScale()
        {
            if (LeapHand == null || (leapProvider == null && Hands.Provider == null)) return;

            if (SetModelScale)
            {
                //Scale the entire model by a ratio of leap middle finger length compared to the models middle finger length
                float middleFingerRatio = (CalculateLeapMiddleFingerLength(LeapHand) / BoundHand.baseScale);
                float scaleRatio = (middleFingerRatio * BoundHand.scaleOffset);

                //Set the object the hand bidner is attached to to scale based on the scale ratio
                transform.localScale = Vector3.Lerp(transform.localScale, BoundHand.startScale * scaleRatio, Time.deltaTime);

                //Scale all the finger tips to match
                for (int i = 0; i < BoundHand.fingers.Length; i++)
                {
                    BoundFinger finger = BoundHand.fingers[i];
                    BoundBone distalBone = finger.boundBones[(int)Bone.BoneType.TYPE_DISTAL];
                    BoundBone intermediateBone = finger.boundBones[(int)Bone.BoneType.TYPE_INTERMEDIATE];

                    Finger leapFinger = LeapHand.Fingers[i];

                    if (intermediateBone.boundTransform == null || distalBone.boundTransform == null || leapFinger == null || finger.fingerTipBaseLength == 0)
                    {
                        return;
                    }

                    //Get the length of the leap finger tip
                    float leapFingerLength = leapFinger.bones.Last().Length;
                    //Get the length of the models finger tip (Calculated when the hand was first bound)
                    float fingerTipLength = finger.fingerTipBaseLength;
                    //Calculate a ratio to use for scaling the finger tip
                    float ratio = leapFingerLength / fingerTipLength;
                    //Adjust the ratio by an offset value exposed in the inspector and the overal scale that has been calculated
                    float adjustedRatio = (ratio * (finger.fingerTipScaleOffset) - BoundHand.scaleOffset);

                    //Calculate the direction that goes up the bone towards the next bone
                    Vector3 direction = (distalBone.boundTransform.position - intermediateBone.boundTransform.position);
                    //Calculate which axis to scale along
                    Vector3 axis = CalculateAxis(distalBone.boundTransform, direction);

                    //Calculate the scale by ensuring all axis are 1 apart from the axis to scale along
                    Vector3 scale = Vector3.one + (axis * adjustedRatio);
                    //Scale the last finger bone 
                    distalBone.boundTransform.localScale = Vector3.Lerp(distalBone.boundTransform.localScale, scale, Time.deltaTime);
                }
            }

            else
            {
                transform.localScale = BoundHand.startScale;

                for (int i = 0; i < BoundHand.fingers.Length; i++)
                {
                    var finger = BoundHand.fingers[i];
                    var lastBone = finger.boundBones.LastOrDefault();

                    if (lastBone.boundTransform == null) continue;

                    lastBone.boundTransform.localScale = lastBone.startTransform.scale;

                }
            }
        }

        /// <summary>
        /// Set the Elbow joint into the correct position and rotation
        /// </summary>
        void TransformElbow()
        {
            if (BoundHand.elbow.boundTransform != null)
            {
                if (SetPositions)
                {
                    BoundHand.elbow.boundTransform.transform.position = LeapHand.Arm.ElbowPosition.ToVector3();
                    BoundHand.elbow.boundTransform.transform.rotation = LeapHand.Arm.Rotation.ToQuaternion() * Quaternion.Euler(BoundHand.elbow.offset.rotation) * Quaternion.Euler(WristRotationOffset);
                }
                //Calculate the elbows position and rotation making sure to maintain the models forearm length
                else if (BoundHand.wrist.boundTransform != null)
                {
                    var dir = (LeapHand.Arm.PrevJoint.ToVector3() - LeapHand.WristPosition.ToVector3()).normalized;
                    var position = LeapHand.WristPosition.ToVector3() + dir * ElbowLength;
                    position += BoundHand.elbow.offset.position;

                    BoundHand.elbow.boundTransform.transform.position = position;
                    BoundHand.elbow.boundTransform.transform.rotation = LeapHand.Arm.Rotation.ToQuaternion()
                        * Quaternion.Euler(BoundHand.elbow.offset.rotation)
                        * Quaternion.Euler(WristRotationOffset);
                }
            }
        }

        /// <summary>
        /// Set the wrist into the correct position and rotation
        /// </summary>
        void TransformWrist()
        {
            //Update the wrist's position and rotation to leap data
            if (BoundHand.wrist.boundTransform != null)
            {
                //Calculate the position of the wrist to the leap position + offset defined by the user
                var wristPosition = LeapHand.WristPosition.ToVector3() + BoundHand.wrist.offset.position;

                //Calculate rotation offset needed to get the wrist into the same rotation as the leap based on the calculated wrist offset
                var leapRotationOffset = ((Quaternion.Inverse(BoundHand.wrist.boundTransform.transform.rotation) * LeapHand.Rotation.ToQuaternion()) * Quaternion.Euler(WristRotationOffset)).eulerAngles;

                //Set the wrist bone to the calculated values
                BoundHand.wrist.boundTransform.transform.position = wristPosition;
                BoundHand.wrist.boundTransform.transform.rotation *= Quaternion.Euler(leapRotationOffset);
            }
        }

        /// <summary>
        /// Set a bone of the hand model into the correct position and rotation
        /// </summary>
        void TransformFingerBones()
        {

            for (int fingerIndex = 0; fingerIndex < BoundHand.fingers.Length; fingerIndex++)
            {
                var finger = BoundHand.fingers[fingerIndex];

                for (int boneIndex = 0; boneIndex < finger.boundBones.Length; boneIndex++)
                {
                    var boundBone = finger.boundBones[boneIndex];
                    var startTransform = boundBone.startTransform;
                    var leapBone = LeapHand.Fingers[fingerIndex].bones[boneIndex];
                    var boneOffset = boundBone.offset;
                    var boundTransform = boundBone.boundTransform;

                    //Continue if the user has not defined a transform for this finger
                    if (boundBone.boundTransform == null)
                    {
                        continue;
                    }

                    //Skip the meta bones if the user does not want to use them
                    if (!UseMetaBones && boneIndex == 0)
                    {
                        boundTransform.transform.localPosition = startTransform.position;
                        boundTransform.transform.localRotation = Quaternion.Euler(startTransform.rotation);
                    }
                    else
                    {
                        //Only update the finger position if the user has defined this behaviour
                        if (SetPositions)
                        {
                            boundTransform.transform.position = leapBone.PrevJoint.ToVector3();
                        }
                        else
                        {
                            boundTransform.transform.localPosition = startTransform.position;
                        }
                    }

                    //Apply any offsets the user has set up in the inspector
                    boundTransform.transform.localPosition += boneOffset.position;

                    //Update the bound transforms rotation to the leap's rotation * global rotation offset * any further offsets the user has defined
                    boundTransform.transform.rotation = leapBone.Rotation.ToQuaternion() * Quaternion.Euler(GlobalFingerRotationOffset) * Quaternion.Euler(boneOffset.rotation);
                }
            }


        }

        #endregion

        #region Scale

        /// <summary>
        /// Calculate the leap hand size
        /// </summary>
        float CalculateLeapMiddleFingerLength(Hand hand)
        {
            var length = 0f;
            for (int i = 0; i < hand.Fingers[(int)Finger.FingerType.TYPE_MIDDLE].bones.Length; i++)
            {
                //If the bound hand does not contain a bone then don't count this in the calculation for leap length
                var boundBone = BoundHand.fingers[(int)Finger.FingerType.TYPE_MIDDLE].boundBones[i];
                if (boundBone.boundTransform != null)
                {
                    var bone = hand.Fingers[(int)Finger.FingerType.TYPE_MIDDLE].bones[i];
                    length += (bone.PrevJoint - bone.NextJoint).Magnitude;
                }
            }
            return length;
        }

        Vector3 CalculateAxis(Transform t, Vector3 dir)
        {
            var boneForward = t.InverseTransformDirection(dir.normalized).normalized;
            boneForward.x = Mathf.Round(boneForward.x);
            boneForward.y = Mathf.Round(boneForward.y);
            boneForward.z = Mathf.Round(boneForward.z);

            return boneForward;
        }
        #endregion

        #region Cleanup

        /// <summary>
        /// When this component is removed from an object, ensure the hand is reset back to how it started
        /// </summary>
        private void OnDestroy()
        {
            ResetHand();
        }

        /// <summary>
        /// Reset is called when the user hits the Reset button in the Inspector's context menu or when adding the component the first time.
        /// </summary>
        private void Reset()
        {
            //Return if we already have assigned base transforms
            if (DefaultHandPose != null)
            {
                ResetHand();
                return;
            }

            else
            {
                SetDefaultHandPose();
            }
        }

        void SetDefaultHandPose()
        {
            //Store all children transforms so the user has the ability to reset back to a default pose
            var allChildren = new List<Transform>();
            allChildren.Add(transform);
            allChildren.AddRange(HandBinderAutoBinder.GetAllChildren(transform));

            var baseTransforms = new List<SerializedTransform>();
            foreach (var child in allChildren)
            {
                var serializedTransform = new SerializedTransform();
                serializedTransform.reference = child.gameObject;
                serializedTransform.transform = new TransformStore();
                serializedTransform.transform.position = child.localPosition;
                serializedTransform.transform.rotation = child.localRotation.eulerAngles;
                serializedTransform.transform.scale = child.localScale;

                baseTransforms.Add(serializedTransform);
            }
            DefaultHandPose = baseTransforms.ToArray();
        }

        /// <summary>
        /// Reset the boundGameobjects back to the default pose given by DefaultHandPose.
        /// </summary>
        public void ResetHand(bool forceReset = false)
        {
            if (this == null) return;

            if (DefaultHandPose == null || EditPoseNeedsResetting == false && forceReset == false)
            {
                return;
            };

            for (int i = 0; i < DefaultHandPose.Length; i++)
            {

                var baseTransform = DefaultHandPose[i];
                if (baseTransform != null && baseTransform.reference != null)
                {
                    baseTransform.reference.transform.localPosition = baseTransform.transform.position;
                    baseTransform.reference.transform.localRotation = Quaternion.Euler(baseTransform.transform.rotation);
                    baseTransform.reference.transform.localScale = baseTransform.transform.scale;

                }
            }

            if (BoundHand.startScale != Vector3.zero)
            {
                transform.localScale = BoundHand.startScale;
            }

            EditPoseNeedsResetting = false;
        }
        #endregion

        #region Editor

        /// <summary> 
        /// Set the assigned transforms to the leap hand during editor 
        /// </summary>
        [Tooltip("Set the assigned transforms to the leap hand during editor")]
        public bool SetEditorPose;

        /// <summary> 
        /// The size of the debug gizmos 
        /// </summary>
        [Tooltip("The size of the debug gizmos")]
        public float GizmoSize = 0.004f;

        /// <summary> 
        /// Show the Leap Hand in the scene 
        /// </summary>
        [Tooltip("Show the Leap Hand in the scene")]
        public bool DebugLeapHand = true;
        /// <summary> 
        /// Show the leap's rotation axis in the scene 
        /// </summary>
        [Tooltip("Show the leap's rotation axis in the scene")]
        public bool DebugLeapRotationAxis = false;
        /// <summary> 
        /// Show the assigned gameobjects as gizmos in the scene 
        /// </summary>
        [Tooltip("Show the assigned gameobjects as gizmos in the scene")]
        public bool DebugModelTransforms = true;
        /// <summary> 
        /// Show the assigned gameobjects rotation axis in the scene 
        /// </summary>
        [Tooltip("Show the assigned gameobjects rotation axis in the scene")]
        public bool DebugModelRotationAxis;

        /// <summary> 
        /// Used by the editor script. Fine tuning allows to specify custom wrist and 
        /// finger rotation offsets. 
        /// </summary>
        public bool FineTuning;
        /// <summary>  
        /// Used by the editor script. The DebugOptions allow to show a debug hand in the scene view
        /// and visualize its rotation and its attached gameobjects
        /// </summary>
        public bool DebugOptions;
        /// <summary> 
        /// Used by the editor script. 
        /// </summary>
        public bool EditPoseNeedsResetting = false;

        /// <summary>
        /// Returns whether or not this hand model supports editor persistence. 
        /// Set by public SetEditorPose.
        /// </summary>
        public override bool SupportsEditorPersistence()
        {
            bool editorPersistance = SetEditorPose;

            if (SetEditorPose == false)
            {
                ResetHand();
            }

            if (DebugLeapHand)
            {
                editorPersistance = true;
            }

            return editorPersistance;
        }

        #endregion
    }
}