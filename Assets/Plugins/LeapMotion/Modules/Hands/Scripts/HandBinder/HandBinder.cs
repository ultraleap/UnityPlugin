using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Leap.Unity.HandsModule {

    public class HandBinder : HandModelBase {
        public Hand LeapHand;
        public HandBinderBoneDefinitions CustomBoneDefinitions;
        public float GizmoSize = 0.004f;
        public bool DebugLeapHand;
        public bool DebugModelTransforms;
        public bool SetEditorPose;
        public bool SetPositions;
        public bool UseMetaBones;

        public Vector3 WristRotationOffset;
        public Vector3 GlobalFingerRotationOffset;

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

        private void Reset() {
            ResetHand();
        }

        /// <summary>
        /// Update the BoundGameobjects so that the positions and rotations match that of the leap hand
        /// </summary>
        public override void UpdateHand() {
            Vector3 position;
            Quaternion rotation;
            Transform boundObject = null;

            var index = 0;

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
        }
    }
}