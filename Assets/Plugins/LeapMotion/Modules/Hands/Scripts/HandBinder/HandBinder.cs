/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.HandsModule {

    public class HandBinder : HandModelBase {
        public Hand LeapHand;

        [Tooltip("Custom Bone Name definitions")]
        public HandBinderBoneDefinitions CustomBoneDefinitions;

        [Tooltip("The size of the debug gizmos")]
        public float GizmoSize = 0.004f;
        [Tooltip("The length of the elbow to maintain the correct offset from the wrist")]
        public float elbowLength;

        [Tooltip("Set the assigned transforms to the leap hand during editor")]
        public bool SetEditorPose;
        [Tooltip("Set the assigned transforms to the same position as the Leap Hand")]
        public bool SetPositions;
        [Tooltip("Use metacarpal bones")]
        public bool UseMetaBones;
        [Tooltip("The Rotation offset that will be assigned to all the Fingers")]
        public Vector3 GlobalFingerRotationOffset;

        //Used by Editor Script
        public bool fineTuning;

        public bool debugOptions;
        public bool riggingOptions;
        public bool armRigging;

        [Tooltip("Show the Leap Hand in the scene")]
        public bool DebugLeapHand = true;

        [Tooltip("Show the Leaps rotation axis in the scene")]
        public bool DebugLeapRotationAxis = false;

        [Tooltip("Show the assigned gameobjects as gizmos in the scene")]
        public bool DebugModelTransforms = true;

        [Tooltip("Show the assigned gameobjects rotation axis in the scene")]
        public bool DebugModelRotationAxis;

        //The data structure that contains transforms that get bound to the leap data
        public BoundHand boundHand = new BoundHand();

        //User defines offsets in editor script
        public List<BoundTypes> offsets = new List<BoundTypes>();

        public override Chirality Handedness { get { return handedness; } set { } }
        public Chirality handedness;
        public override ModelType HandModelType { get { return ModelType.Graphics; } }

        public override Hand GetLeapHand() {
            return LeapHand;
        }

        public override void SetLeapHand(Hand hand) {
            LeapHand = hand;
        }

        public override bool SupportsEditorPersistence() {
            return false;
        }

        private void OnDestroy() {
            ResetHand();
        }

        //Reset is called when the user hits the Reset button in the Inspector's context menu or when adding the component the first time.
        private void Reset() {
            ResetHand();
        }

        /// <summary>
        /// Update the BoundGameobjects so that the positions and rotations match that of the leap hand
        /// </summary>
        public override void UpdateHand() {

            //Calculate the elbows position and rotation making sure to maintatin the models forearm length
            if(boundHand.elbow.boundTransform != null) {
                var elbowPosition = LeapHand.WristPosition.ToVector3() -
                                        ((LeapHand.Arm.Basis.zBasis.ToVector3() * elbowLength) + boundHand.elbow.offset.position);
                boundHand.elbow.boundTransform.transform.position = elbowPosition;
                boundHand.elbow.boundTransform.transform.rotation = LeapHand.Arm.Rotation.ToQuaternion() * Quaternion.Euler(boundHand.elbow.offset.rotation);
            }

            //Update the bound wrist transform to leap data
            if(boundHand.wrist.boundTransform != null) {
                //Now set the wrist position
                var wristPosition = LeapHand.WristPosition.ToVector3() + boundHand.wrist.offset.position;
                var wristRotation = LeapHand.Rotation.ToQuaternion() * Quaternion.Euler(boundHand.wrist.offset.rotation);

                boundHand.wrist.boundTransform.transform.position = wristPosition;
                boundHand.wrist.boundTransform.transform.rotation = wristRotation;
            }

            //Loop through all the leap fingers and update an bound fingers to the leap data
            if(LeapHand != null) {
                for(int fingerIndex = 0; fingerIndex < LeapHand.Fingers.Count; fingerIndex++) {
                    for(int boneIndex = 0; boneIndex < LeapHand.Fingers[fingerIndex].bones.Length; boneIndex++) {

                        var boundTransform = boundHand.fingers[fingerIndex].boundBones[boneIndex].boundTransform;
                        //Continue if the user has not defined a transform for this finger
                        if(boundTransform == null) {
                            continue;
                        }

                        //Get the start transform that was stored for each assigned transform
                        var startTransform = boundHand.fingers[fingerIndex].boundBones[boneIndex].startTransform;

                        if(boneIndex == 0 && !UseMetaBones) {
                            boundTransform.transform.localRotation = Quaternion.Euler(startTransform.rotation);
                            boundTransform.transform.localPosition = startTransform.position;
                            continue;
                        }

                        //Get the leap bone to extract the position and rotation values
                        var bone = LeapHand.Fingers[fingerIndex].bones[boneIndex];
                        //Get any offsets the user has set up
                        var offset = boundHand.fingers[fingerIndex].boundBones[boneIndex].offset;

                        //Only update the finger position if the user has defined this behaviour
                        if(SetPositions) {
                            boundTransform.transform.position = bone.PrevJoint.ToVector3() + offset.position;
                        }
                        else {
                            //User can still add offsets to the start position even if they are not setting leap positional data
                            boundTransform.transform.localPosition = startTransform.position + offset.position;
                        }

                        //Update the bound transforms rotation to the leaps rotation * global rotation offset * any further offsets the user has defined
                        boundTransform.transform.rotation = bone.Rotation.ToQuaternion() * Quaternion.Euler(GlobalFingerRotationOffset) * Quaternion.Euler(offset.rotation);
                    }
                }
            }
        }

        /// <summary>
        /// Reset the boundGameobjects back to the default pose
        /// </summary>
        public void ResetHand() {

            //Reset all of the boundTransforms back to position and rotation they were stored to.
            SetEditorPose = false;
            foreach(var finger in boundHand.fingers) {
                if(finger == null) {
                    continue;
                }

                foreach(var bone in finger.boundBones) {
                    if(bone.boundTransform == null) {
                        continue;
                    }

                    bone.boundTransform.localPosition = bone.startTransform.position;
                    bone.boundTransform.localRotation = Quaternion.Euler(bone.startTransform.rotation);
                }
            }

            if(boundHand.elbow.boundTransform != null) {
                boundHand.elbow.boundTransform.localPosition = boundHand.elbow.startTransform.position;
                boundHand.elbow.boundTransform.localRotation = Quaternion.Euler(boundHand.elbow.startTransform.rotation);
            }

            if(boundHand.wrist.boundTransform != null) {
                boundHand.wrist.boundTransform.localPosition = boundHand.wrist.startTransform.position;
                boundHand.wrist.boundTransform.localRotation = Quaternion.Euler(boundHand.wrist.startTransform.rotation);
            }
        }
    }
}