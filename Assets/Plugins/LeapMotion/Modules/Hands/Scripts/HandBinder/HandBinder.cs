using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Leap.Unity.HandsModule {

    public class HandBinder : HandModelBase {

        public Hand LeapHand;
        [Tooltip("Custom Bone Name definitions")]
        public HandBinderBoneDefinitions CustomBoneDefinitions;
        [Tooltip("The size of the debug gizmos")]
        public float GizmoSize = 0.004f;
        [Tooltip("Show the Leap Hand in the scene")]     
        public bool DebugLeapHand;
        [Tooltip("Show the assigned gameobjects as gizmos in the scene")]
        public bool DebugModelTransforms;
        [Tooltip("Set the assigned transforms to the leap hand during editor")]
        public bool SetEditorPose;
        [Tooltip("Set the assigned transforms to the same position as the Leap Hand")]
        public bool SetPositions;
        [Tooltip("Use metacarpal bones")]
        public bool UseMetaBones;

        [Tooltip("The Rotation offset that will be assigned to the assigned wrist bone")]
        public Vector3 WristRotationOffset;
        [Tooltip("The Rotation offset that will be assigned to all the Fingers")]
        public Vector3 GlobalFingerRotationOffset;
        [Tooltip("The elbow that will get assigned to the leap elbow position")]
        public GameObject elbow;
        [Tooltip("The shoulder bone that will be oriented to look at the elbow")]
        public GameObject shoulder;
        [Tooltip("The Rotation offset that will get applied to the assigned elbow bone")]
        public Vector3 elbowRotationOffset;
        [Tooltip("The Position offset that will get applied to the assigned elbow bone")]
        public Vector3 elbowPositionOffset;
        [Tooltip("The Rotation offset that will get applied to the assigned Shoulder bone")]
        public Vector3 shoulderRotationOffset;
        public Offset elbowOffset;
        public Offset shoulderOffset;

        /// <summary>
        /// Being used to store position and rotations
        /// </summary>
        [System.Serializable]
        public class Offset {
            public Finger.FingerType fingerType;
            public Bone.BoneType boneType;
            public Vector3 position = Vector3.zero;
            public Vector3 rotation = Vector3.zero;
        }

        public List<Offset> Offsets = new List<Offset>();
        public Transform[] BoundGameobjects = new Transform[21];
        public Offset[] StartTransforms = new Offset[21];

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

        private void OnDisable() {
            ResetHand();
        }

        //Reset is called when the user hits the Reset button in the Inspector's context menu or when adding the component the first time.
        private void Reset() {
            ResetHand();
        }

        private void Start() {
        }
        /// <summary>
        /// Update the BoundGameobjects so that the positions and rotations match that of the leap hand
        /// </summary>
        public override void UpdateHand() {
            Vector3 position;
            Quaternion rotation;
            Transform boundObject = null;

            var index = 0;

            if(shoulder != null && elbow != null) {

                shoulder.transform.LookAt(elbow.transform.position);
                shoulder.transform.rotation *= Quaternion.Euler(shoulderRotationOffset);
            }

            if(elbow != null) {
                elbow.transform.position = LeapHand.Arm.ElbowPosition.ToVector3() + elbowPositionOffset;
                elbow.transform.rotation = LeapHand.Arm.Rotation.ToQuaternion() * Quaternion.Euler(elbowRotationOffset);
            }

            if(LeapHand != null) {
                for(int fingerType = 0; fingerType < LeapHand.Fingers.Count; fingerType++) {
                    var currentFinger = LeapHand.Fingers[fingerType];
                    for(int boneType = 0; boneType < currentFinger.bones.Length; boneType++) {
                        boundObject = BoundGameobjects.Length > index ? BoundGameobjects[index] : null;

                        if(boundObject != null) {
                            if(boneType == 0 && !UseMetaBones) {
                                boundObject.transform.localRotation = Quaternion.Euler(StartTransforms[index].rotation);
                                boundObject.transform.localPosition = StartTransforms[index].position;

                                index++;
                                continue;
                            }

                            var bone = LeapHand.Fingers[fingerType].bones[boneType];

                            //Find an offset that works with this bone
                            Offset offset = Offsets.FirstOrDefault(x => x.fingerType == currentFinger.Type && x.boneType == bone.Type);

                            if(SetPositions) {
                                position = bone.PrevJoint.ToVector3();
                                if(offset != null)
                                    position += offset.position;
                                boundObject.transform.position = position;
                            }
                            else {
                                position = StartTransforms[index].position;
                                if(offset != null)
                                    position += offset.position;
                                boundObject.transform.localPosition = position;
                            }

                            rotation = bone.Rotation.ToQuaternion() * Quaternion.Euler(GlobalFingerRotationOffset);
                            if(offset != null)
                                rotation *= Quaternion.Euler(offset.rotation);
                            boundObject.transform.rotation = rotation;
                        }

                        index++;
                    }
                }

                boundObject = BoundGameobjects[index];
                if(boundObject != null) {
                    //Now set the wrist position
                    position = LeapHand.WristPosition.ToVector3();
                    rotation = LeapHand.Rotation.ToQuaternion() * Quaternion.Euler(WristRotationOffset);

                    boundObject.transform.position = position;
                    boundObject.transform.rotation = rotation;
                }
            }
        }

        /// <summary>
        /// Reset the boundGameobjects back to the default pose
        /// </summary>
        public void ResetHand() {
            for(int i = 0; i < BoundGameobjects.Length; i++) {
                var boundObject = BoundGameobjects[i];
                if(boundObject != null) {
                    boundObject.transform.localPosition = StartTransforms[i].position;
                    boundObject.transform.localRotation = Quaternion.Euler(StartTransforms[i].rotation);
                }
            }

            if(elbow != null) {
                elbow.transform.localPosition = elbowOffset.position;
                elbow.transform.localRotation = Quaternion.Euler(elbowOffset.rotation);
            }

            if(shoulder != null) {
                shoulder.transform.localPosition = shoulderOffset.position;
                shoulder.transform.localRotation = Quaternion.Euler(shoulderOffset.rotation);
            }
        }
    }
}