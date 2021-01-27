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
            if(boundHand.elbow.boundTransform != null) {
                var elbowPosition = LeapHand.WristPosition.ToVector3() -
                                        ((LeapHand.Arm.Basis.zBasis.ToVector3() * elbowLength) + boundHand.elbow.offset.position);
                boundHand.elbow.boundTransform.transform.position = elbowPosition;
                boundHand.elbow.boundTransform.transform.rotation = LeapHand.Arm.Rotation.ToQuaternion() * Quaternion.Euler(boundHand.elbow.offset.rotation);
            }

            if(boundHand.wrist.boundTransform != null) {
                //Now set the wrist position
                var position = LeapHand.WristPosition.ToVector3() + boundHand.wrist.offset.position;
                var rotation = LeapHand.Rotation.ToQuaternion() * Quaternion.Euler(boundHand.wrist.offset.rotation);

                boundHand.wrist.boundTransform.transform.position = position;
                boundHand.wrist.boundTransform.transform.rotation = rotation;
            }

            if(LeapHand != null) {
                for(int fingerIndex = 0; fingerIndex < LeapHand.Fingers.Count; fingerIndex++) {
                    for(int boneIndex = 0; boneIndex < LeapHand.Fingers[fingerIndex].bones.Length; boneIndex++) {
                        var boundTransform = boundHand.fingers[fingerIndex].boundBones[boneIndex].boundTransform;

                        if(boundTransform == null) {
                            continue;
                        }
                        var startTransform = boundHand.fingers[fingerIndex].boundBones[boneIndex].startTransform;

                        if(boneIndex == 0 && !UseMetaBones) {
                            boundTransform.transform.localRotation = Quaternion.Euler(startTransform.rotation);
                            boundTransform.transform.localPosition = startTransform.position;
                            continue;
                        }

                        var bone = LeapHand.Fingers[fingerIndex].bones[boneIndex];
                        var offset = boundHand.fingers[fingerIndex].boundBones[boneIndex].offset;

                        Vector3 position = Vector3.zero;
                        Quaternion rotation = Quaternion.identity;

                        if(SetPositions) {
                            position = bone.PrevJoint.ToVector3() + offset.position;
                            boundTransform.transform.position = position;
                        }
                        else {
                            position = startTransform.position + offset.position;
                            boundTransform.transform.localPosition = position;
                        }

                        rotation = bone.Rotation.ToQuaternion() * Quaternion.Euler(GlobalFingerRotationOffset) * Quaternion.Euler(offset.rotation);
                        boundTransform.transform.rotation = rotation;
                    }
                }
            }
        }

        /// <summary>
        /// Reset the boundGameobjects back to the default pose
        /// </summary>
        public void ResetHand() {
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