/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.HandsModule {

    [DisallowMultipleComponent]
    public class HandBinder : HandModelBase {
        public Hand LeapHand;

        [Tooltip("The size of the debug gizmos")]
        public float GizmoSize = 0.004f;
        [Tooltip("The length of the elbow to maintain the correct offset from the wrist")]
        public float ElbowLength;
        [Tooltip("The rotation offset that will be assigned to all the fingers")]
        public Vector3 GlobalFingerRotationOffset;
        [Tooltip("The rotation offset that will be assigned to the wrist")]
        public Vector3 WristRotationOffset;
        [Tooltip("Set the assigned transforms to the leap hand during editor")]
        public bool SetEditorPose;
        [Tooltip("Set the assigned transforms to the same position as the Leap Hand")]
        public bool SetPositions;
        [Tooltip("Use metacarpal bones")]
        public bool UseMetaBones;
        [Tooltip("Show the Leap Hand in the scene")]
        public bool DebugLeapHand = true;
        [Tooltip("Show the leap's rotation axis in the scene")]
        public bool DebugLeapRotationAxis = false;
        [Tooltip("Show the assigned gameobjects as gizmos in the scene")]
        public bool DebugModelTransforms = true;
        [Tooltip("Show the assigned gameobjects rotation axis in the scene")]
        public bool DebugModelRotationAxis;

        //Used by editor script
        public bool FineTuning;
        public bool DebugOptions;
        public bool EditPoseNeedsResetting = false;
        public Chirality Chirality;

        //The data structure that contains transforms that get bound to the leap data
        public BoundHand BoundHand = new BoundHand();
        //User defines offsets in editor script
        public List<BoundTypes> Offsets = new List<BoundTypes>();
        //Stores all the childrens default pose
        public SerializedTransform[] DefaultHandPose;

        //Custom editor requires Chirality in a non overridden property, Public Chirality exists for the editor.
        public override Chirality Handedness { get { return Chirality; } set { } }
        public override ModelType HandModelType { get { return ModelType.Graphics; } }
        public override Hand GetLeapHand() { return LeapHand;}
        public override void SetLeapHand(Hand hand) { LeapHand = hand; }

        public override bool SupportsEditorPersistence() {

            bool editorPersistance = SetEditorPose;

            if(SetEditorPose == false) {
                ResetHand();
            }

            if(DebugLeapHand) {
                editorPersistance = true;
            }

            return editorPersistance;
        }

        private void OnDestroy() {
            ResetHand();
        }

        //Reset is called when the user hits the Reset button in the Inspector's context menu or when adding the component the first time.
        private void Reset() {

            //Return if we already have assigned base transforms
            if(DefaultHandPose != null) {
                ResetHand();
                return;
            }

            else {
                //Store all children transforms so the user has the ability to reset back to a default pose
                var allChildren = new List<Transform>();
                allChildren.Add(transform);
                allChildren.AddRange(HandBinderAutoBinder.GetAllChildren(transform));

                var baseTransforms = new List<SerializedTransform>();
                foreach(var child in allChildren) {
                    var serializedTransform = new SerializedTransform();
                    serializedTransform.reference = child.gameObject;
                    serializedTransform.transform = new TransformStore();
                    serializedTransform.transform.position = child.localPosition;
                    serializedTransform.transform.rotation = child.localRotation.eulerAngles;
                    baseTransforms.Add(serializedTransform);
                }
                DefaultHandPose = baseTransforms.ToArray();
            }
        }

        /// <summary>
        /// Update the BoundGameobjects so that the positions and rotations match that of the leap hand
        /// </summary>
        public override void UpdateHand() {

            if(!SetEditorPose && !Application.isPlaying) {
                return;
            }

            if(LeapHand == null) {
                ResetHand();
                return;
            }

            //Calculate the elbows position and rotation making sure to maintain the models forearm length
            if(BoundHand.elbow.boundTransform != null && BoundHand.wrist.boundTransform != null && ElbowLength > 0) {
                //Calculate the position of the elbow based on the calcualted elbow length
                var elbowPosition = LeapHand.WristPosition.ToVector3() -
                                        ((LeapHand.Arm.Basis.zBasis.ToVector3() * ElbowLength) + BoundHand.elbow.offset.position);
                if(!elbowPosition.ContainsNaN()) {
                    BoundHand.elbow.boundTransform.transform.position = elbowPosition;
                    BoundHand.elbow.boundTransform.transform.rotation = LeapHand.Arm.Rotation.ToQuaternion() * Quaternion.Euler(BoundHand.elbow.offset.rotation);
                }
            }

            //Update the wrist's position and rotation to leap data
            if(BoundHand.wrist.boundTransform != null) {

                //Calculate the position of the wrist to the leap position + offset defined by the user
                var wristPosition = LeapHand.WristPosition.ToVector3() + BoundHand.wrist.offset.position;

                //Calculate rotation offset needed to get the wrist into the same rotation as the leap based on the calculated wrist offset
                var leapRotationOffset = ((Quaternion.Inverse(BoundHand.wrist.boundTransform.transform.rotation) * LeapHand.Rotation.ToQuaternion()) * Quaternion.Euler(WristRotationOffset)).eulerAngles;

                //Set the wrist bone to the calculated values
                BoundHand.wrist.boundTransform.transform.position = wristPosition;
                BoundHand.wrist.boundTransform.transform.rotation *= Quaternion.Euler(leapRotationOffset);
            }

            //Loop through all the leap fingers and update the bound fingers to the leap data
            if(LeapHand != null) {
                for(int fingerIndex = 0; fingerIndex < LeapHand.Fingers.Count; fingerIndex++) {
                    for(int boneIndex = 0; boneIndex < LeapHand.Fingers[fingerIndex].bones.Length; boneIndex++) {

                        //The transform that the user has defined
                        var boundTransform = BoundHand.fingers[fingerIndex].boundBones[boneIndex].boundTransform;

                        //Continue if the user has not defined a transform for this finger
                        if(boundTransform == null) {
                            continue;
                        }

                        //Get the start transform that was stored for each assigned transform
                        var startTransform = BoundHand.fingers[fingerIndex].boundBones[boneIndex].startTransform;

                        if(boneIndex == 0 && !UseMetaBones) {
                            boundTransform.transform.localRotation = Quaternion.Euler(startTransform.rotation);
                            boundTransform.transform.localPosition = startTransform.position;
                            continue;
                        }

                        //Get the leap bone to extract the position and rotation values
                        var leapBone = LeapHand.Fingers[fingerIndex].bones[boneIndex];
                        //Get any offsets the user has set up
                        var boneOffset = BoundHand.fingers[fingerIndex].boundBones[boneIndex].offset;

                        //Only update the finger position if the user has defined this behaviour
                        if(SetPositions) {
                            boundTransform.transform.position = leapBone.PrevJoint.ToVector3();
                            boundTransform.transform.localPosition += boneOffset.position;
                        }

                        else {
                            boundTransform.transform.localPosition = startTransform.position + boneOffset.position;
                        }

                        //Update the bound transforms rotation to the leap's rotation * global rotation offset * any further offsets the user has defined
                        boundTransform.transform.rotation = leapBone.Rotation.ToQuaternion() * Quaternion.Euler(GlobalFingerRotationOffset) * Quaternion.Euler(boneOffset.rotation);
                    }
                }
            }

            EditPoseNeedsResetting = true;
        }

        /// <summary>
        /// Reset the boundGameobjects back to the default pose
        /// </summary>
        public void ResetHand(bool forceReset = false) {

            if(DefaultHandPose == null || EditPoseNeedsResetting == false && forceReset == false) {
                return;
            };

            for(int i = 0; i < DefaultHandPose.Length; i++) {

                var baseTransform = DefaultHandPose[i];
                if(baseTransform != null && baseTransform.reference != null) {
                    baseTransform.reference.transform.localPosition = baseTransform.transform.position;
                    baseTransform.reference.transform.localRotation = Quaternion.Euler(baseTransform.transform.rotation);
                }
            }
            EditPoseNeedsResetting = false;
        }
    }
}
