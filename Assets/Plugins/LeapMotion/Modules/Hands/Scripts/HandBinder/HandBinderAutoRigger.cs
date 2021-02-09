/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Leap.Unity.HandsModule {

    public static class HandBinderAutoRigger {

        /// <summary>
        /// This function is used to search the HandBinder scipts children transforms to auto assign them for the user
        /// </summary>
        /// <param name="handBinder">The binder that the found transforms will get assigned too</param>
        public static void AutoRig(HandBinder handBinder) {
            handBinder.ResetHand();
            BoneDefinitions boneDefinitions = null;

            //Check to see if we have an autorigger Definitions scriptable object
            if(handBinder.CustomBoneDefinitions == null) {
                boneDefinitions = new BoneDefinitions();
            }
            else {
                boneDefinitions = handBinder.CustomBoneDefinitions.BoneDefinitions;
            }

            //Get all children of the hand
            var children = new List<Transform>();
            children.Add(handBinder.transform); 
            children.AddRange(GetAllChildren(handBinder.transform));

            var thumbBones = SelectBones(children, boneDefinitions.DefinitionThumb, true);
            var indexBones = SelectBones(children, boneDefinitions.DefinitionIndex);
            var middleBones = SelectBones(children, boneDefinitions.DefinitionMiddle);
            var ringBones = SelectBones(children, boneDefinitions.DefinitionRing);
            var pinkyBones = SelectBones(children, boneDefinitions.DefinitionPinky);
            var wrist = SelectBones(children, boneDefinitions.DefinitionWrist).FirstOrDefault();
            var Elbow = SelectBones(children, boneDefinitions.DefinitionElbow).FirstOrDefault();

            handBinder.boundHand.fingers[0].boundBones = AssignUnityBone(thumbBones);
            handBinder.boundHand.fingers[1].boundBones = AssignUnityBone(indexBones);
            handBinder.boundHand.fingers[2].boundBones = AssignUnityBone(middleBones);
            handBinder.boundHand.fingers[3].boundBones = AssignUnityBone(ringBones);
            handBinder.boundHand.fingers[4].boundBones = AssignUnityBone(pinkyBones);
            handBinder.boundHand.wrist = AssignBoundBone(wrist);
            handBinder.boundHand.elbow = AssignBoundBone(Elbow);
            if(wrist != null && Elbow != null) {
                handBinder.elbowLength = (wrist.position - Elbow.position).magnitude;
            }

            EstimateWristRotationOffset(handBinder);

            handBinder.UpdateHand();
            handBinder.DebugModelTransforms = true;
            handBinder.SetEditorPose = true;
        }

        /// <summary>
        /// Get all the children of a transform
        /// </summary>
        /// <param name="_t"></param>
        /// <returns></returns>
        public static List<Transform> GetAllChildren(Transform _t) {
            List<Transform> ts = new List<Transform>();
            foreach(Transform t in _t) {
                ts.Add(t);
                if(t.childCount > 0) {
                    ts.AddRange(GetAllChildren(t));
                }
            }
            return ts;
        }

        /// <summary>
        /// The Autorigger uses this to select the children that match the finger definitions
        /// </summary>
        /// <param name="children">The found Children</param>
        /// <param name="definitions">The definition to sort through the children</param>
        /// <param name="isThumb">is this a thumb?</param>
        /// <returns></returns>
        private static Transform[] SelectBones(List<Transform> children, string[] definitions, bool isThumb = false) {
            //Can only ever be 4 bones per hand
            var bones = new Transform[4];
            int foundBoneIndex = 0;
            for(int definitionIndex = 0; definitionIndex < definitions.Length; definitionIndex++) {
                foreach(var child in children) {
                    //We have found all the bones we need
                    if(foundBoneIndex == 4) {
                        break;
                    }

                    var definition = definitions[definitionIndex];
                    if(child.name.ToUpper().Contains(definition.ToUpper())) {
                        bones[foundBoneIndex] = child;
                        foundBoneIndex++;
                    }
                }
            }
            return SortBones(bones, isThumb);
        }

        /// <summary>
        /// Sort through the bones to identify which BoneType they all belong to
        /// </summary>
        /// <param name="bones">The bones you want to sort through</param>
        /// <param name="isThumb">Is it a thumb</param>
        /// <returns></returns>
        private static Transform[] SortBones(Transform[] bones, bool isThumb = false) {
            Transform meta = null;
            Transform proximal = null;
            Transform middle = null;
            Transform distal = null;

            if(isThumb || bones.Length == 3) {
                meta = null;
                proximal = bones[0];
                middle = bones[1];
                distal = bones[2];
            }
            //We assume the 4th child is the distal bone
            else if(bones.Length >= 4) {
                meta = bones[0];
                proximal = bones[1];
                middle = bones[2];
                distal = bones[3];
            }

            var boundObjects = new Transform[]
            {
                meta,
                proximal,
                middle,
                distal
            };

            return boundObjects;
        }

        /// <summary>
        /// Bind a transform in the scene to the Hand Binder
        /// </summary>
        /// <param name="boneTransform">The transform you want to assign </param>
        /// <param name="fingerIndex"> The index of the finger you want to assign</param>
        /// <param name="boneIndex">The index of the bone you want to assign</param>
        /// <param name="handBinder">The Hand Binder this information will be added to</param>
        /// <returns></returns>
        public static BoundBone[] AssignUnityBone(Transform[] boneTransform) {
            var boundFingers = new BoundBone[]
                {
                    AssignBoundBone(boneTransform[0]),
                    AssignBoundBone(boneTransform[1]),
                    AssignBoundBone(boneTransform[2]),
                    AssignBoundBone(boneTransform[3]),
                };

            return boundFingers;
        }

        public static BoundBone AssignBoundBone(Transform transform) {
            var newBone = new BoundBone();
            if(transform != null) {
                newBone.boundTransform = transform;
                newBone.startTransform = new TransformStore();
                newBone.startTransform.position = transform.localPosition;
                newBone.startTransform.rotation = transform.localRotation.eulerAngles;
            }
            return newBone;
        }

        /// <summary>
        /// Estimate the rotation offset needed to get the rigged hand into the same orientation as the leap hand
        /// </summary>
        public static void EstimateWristRotationOffset(HandBinder handBinder) {

            //Try to using the meta first
            Transform indexBone = handBinder.boundHand.fingers[(int)Finger.FingerType.TYPE_INDEX].boundBones[(int)Bone.BoneType.TYPE_METACARPAL].boundTransform;
            Transform middleBone = handBinder.boundHand.fingers[(int)Finger.FingerType.TYPE_MIDDLE].boundBones[(int)Bone.BoneType.TYPE_METACARPAL].boundTransform;
            Transform pinkyBone = handBinder.boundHand.fingers[(int)Finger.FingerType.TYPE_PINKY].boundBones[(int)Bone.BoneType.TYPE_METACARPAL].boundTransform;

            //If the meta does not exist, use the proximal bones
            if(indexBone == null || middleBone == null || pinkyBone == null) {
                indexBone = handBinder.boundHand.fingers[(int)Finger.FingerType.TYPE_INDEX].boundBones[(int)Bone.BoneType.TYPE_PROXIMAL].boundTransform;
                middleBone = handBinder.boundHand.fingers[(int)Finger.FingerType.TYPE_MIDDLE].boundBones[(int)Bone.BoneType.TYPE_PROXIMAL].boundTransform;
                pinkyBone = handBinder.boundHand.fingers[(int)Finger.FingerType.TYPE_PINKY].boundBones[(int)Bone.BoneType.TYPE_PROXIMAL].boundTransform;
            }

            Transform wrist = handBinder.boundHand.wrist.boundTransform;

            if(middleBone != null && indexBone != null && pinkyBone != null && wrist != null) {
                //Get the Direction from the middle finger to the wrist
                Vector3 wristForward = middleBone.position - wrist.position;
                //Get the Direction from the Proximal pinky finger to the Proximal Index finger
                Vector3 wristRight = indexBone.position - pinkyBone.position;

                //Swap the direction based on left and right hands
                if(handBinder.Handedness == Chirality.Right) {
                    wristRight = -wristRight;
                }

                //Get the direciton that goes outwards from the back of the hand
                Vector3 wristUp = Vector3.Cross(wristForward, wristRight);

                //Make the vectors orthoginal to eacother, this is the basis for the model hand
                Vector3.OrthoNormalize(ref wristRight, ref wristUp, ref wristForward);

                //Create a new leap hand based off the Desktop hand pose
                Hand hand = TestHandFactory.MakeTestHand(handBinder.Handedness == Chirality.Left, unitType: TestHandFactory.UnitType.LeapUnits);
                hand.Transform(TestHandFactory.GetTestPoseLeftHandTransform(TestHandFactory.TestHandPose.DesktopModeA));
                Quaternion leapRotation = hand.Rotation.ToQuaternion();

                //Get the rotation of the calculated hand Basis
                Quaternion modelRotation = Quaternion.LookRotation(wristForward, wristUp);

                //Now calculate the difference between the models rotation and the leaps rotation
                Quaternion wristRotationDifference = Quaternion.Inverse(modelRotation) * leapRotation;
                Vector3 wristRelativeDifference = (Quaternion.Inverse(wrist.rotation) * wristRotationDifference).eulerAngles;

                //Round to the nearest 90 degrees
                wristRelativeDifference.x = Mathf.Round(wristRelativeDifference.x / 90) * 90;
                wristRelativeDifference.y = Mathf.Round(wristRelativeDifference.y / 90) * 90;
                wristRelativeDifference.z = Mathf.Round(wristRelativeDifference.z / 90) * 90;

                //Assign these values to the hand binder
                handBinder.GlobalFingerRotationOffset = wristRelativeDifference;
                handBinder.boundHand.wrist.offset.rotation = wristRelativeDifference;
            }
        }
    }
}