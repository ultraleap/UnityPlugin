using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Leap.Unity.HandsModule {

    public class HandBinder : HandModelBase {
        public Hand _leapHand;
        public Chirality handedness;
        public HandBinderBoneDefinitions customBoneDefinitions;
        public float debugHand_Size = 0.004f;
        public bool debugLeapHand, debugModelTransforms, setEditorPose, setPositions, useMetaBones;
        public Vector3 wristRotationOffset, GlobalFingerRotationOffset;

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

        public List<Offset> offsets = new List<Offset>();
        public Transform[] boundGameobjects = new Transform[21];
        public Offset[] startTransforms = new Offset[21];

        public override Chirality Handedness { get { return handedness; } set { } }
        public override ModelType HandModelType { get { return ModelType.Graphics; } }

        public override void BeginHand() {
            base.BeginHand();
        }

        public override Hand GetLeapHand() {
            return _leapHand;
        }

        public override void InitHand() {
        }

        public override void SetLeapHand(Hand hand) {
            _leapHand = hand;
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

        /// <summary>
        /// Update the BoundGameobjects so that the positions and rotations match that of the leap hand
        /// </summary>
        public override void UpdateHand() {
            Vector3 position;
            Quaternion rotation;
            Transform boundObject = null;

            var index = 0;

            if(_leapHand != null) {
                for(int fingerType = 0; fingerType < _leapHand.Fingers.Count; fingerType++) {
                    var currentFinger = _leapHand.Fingers[fingerType];
                    for(int boneType = 0; boneType < currentFinger.bones.Length; boneType++) {
                        boundObject = boundGameobjects.Length > index ? boundGameobjects[index] : null;

                        if(boundObject != null) {
                            if(boneType == 0 && !useMetaBones) {
                                boundObject.transform.localRotation = Quaternion.Euler(startTransforms[index].rotation);
                                boundObject.transform.localPosition = startTransforms[index].position;

                                index++;
                                continue;
                            }

                            var bone = _leapHand.Fingers[fingerType].bones[boneType];

                            //Find an offset that works with this bone
                            Offset offset = offsets.FirstOrDefault(x => x.fingerType == currentFinger.Type && x.boneType == bone.Type);

                            if(setPositions) {
                                position = bone.PrevJoint.ToVector3();
                                if(offset != null)
                                    position += offset.position;
                                boundObject.transform.position = position;
                            }
                            else {
                                position = startTransforms[index].position;
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

                boundObject = boundGameobjects[index];
                if(boundObject != null) {
                    //Now set the wrist position
                    position = _leapHand.WristPosition.ToVector3();
                    rotation = _leapHand.Rotation.ToQuaternion() * Quaternion.Euler(wristRotationOffset);

                    boundObject.transform.position = position;
                    boundObject.transform.rotation = rotation;
                }
            }
        }

        /// <summary>
        /// Reset the boundGameobjects back to the default pose
        /// </summary>
        public void ResetHand() {
            for(int i = 0; i < boundGameobjects.Length; i++) {
                var boundObject = boundGameobjects[i];
                if(boundObject != null) {
                    boundObject.transform.localPosition = startTransforms[i].position;
                    boundObject.transform.localRotation = Quaternion.Euler(startTransforms[i].rotation);
                }
            }
        }
    }
}