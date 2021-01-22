using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Leap.Unity.HandsModule {

    public static class HandBinderAutoRigger {

        /// <summary>
        /// This function is used to search the HandBinder scipts children transforms to auto assign them for the user
        /// </summary>
        /// <param name="handBinder">The binder that the found transforms will get assigned too</param>
        public static void AutoRig(HandBinder handBinder, ref string warnings) {
            handBinder.SetEditorPose = false;
            handBinder.ResetHand();
            warnings = "";
            BoneDefinitions boneDefinitions = null;

            //Check to see if we have an autorigger Definitions scriptable object
            if(handBinder.CustomBoneDefinitions == null) {
                boneDefinitions = new BoneDefinitions();
            }
            else {
                boneDefinitions = handBinder.CustomBoneDefinitions.BoneDefinitions;
            }

            //Get all children of the hand
            var children = GetAllChildren(handBinder.transform);

            var foundBones = new List<Transform>();
            foundBones.AddRange(SelectBones(children, boneDefinitions.DefinitionThumb, true));
            foundBones.AddRange(SelectBones(children, boneDefinitions.DefinitionIndex));
            foundBones.AddRange(SelectBones(children, boneDefinitions.DefinitionMiddle));
            foundBones.AddRange(SelectBones(children, boneDefinitions.DefinitionRing));
            foundBones.AddRange(SelectBones(children, boneDefinitions.DefinitionPinky));
            var wrist = SelectBones(children, boneDefinitions.DefinitionWrist).FirstOrDefault();
            foundBones.Add(wrist);

            for(int i = 0; i < foundBones.Count; i++) {
                AssignUnityBone(foundBones[i], i, handBinder, ref warnings);
            }

            CalculateWristRotationOffset(handBinder);

            if(wrist != null) {

                foreach(var def in boneDefinitions.DefinitionElbow) {
                    foreach(var child in children) {
                        if(child.name.ToUpper().Contains(def.ToUpper())) {

                            var elbow = child.gameObject;

                            handBinder.elbowLength = (wrist.position - elbow.transform.position).magnitude;

                            var startTransform = new HandBinder.TransformStore();
                            startTransform.position = elbow.transform.localPosition;
                            startTransform.rotation = elbow.transform.localRotation.eulerAngles;

                            handBinder.elbowStartPosition = startTransform;

                            handBinder.elbow = elbow.gameObject;
                            break;
                        }
                    }

                    if(handBinder.elbow != null) {
                        break;
                    }
                }
            }

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
                if(t.childCount > 0)
                    ts.AddRange(GetAllChildren(t));
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
            int foundBonesIndex = 0;
            for(int i = 0; i < definitions.Length; i++) {
                foreach(var child in children) {
                    //We have found all the bones we need
                    if(foundBonesIndex == 4)
                        break;

                    var definition = definitions[i];
                    if(child.name.ToUpper().Contains(definition.ToUpper())) {
                        bones[foundBonesIndex] = child;
                        foundBonesIndex++;
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
        public static void AssignUnityBone(Transform boneTransform, int index, HandBinder handBinder, ref string warning) {
            Finger.FingerType fingerType;
            Bone.BoneType boneType;
            IndexToType(index, out fingerType, out boneType);

            var startTransform = handBinder.StartTransforms[index];
            startTransform.fingerType = fingerType;
            startTransform.boneType = boneType;

            if(boneTransform != null) {
                startTransform.position = boneTransform.localPosition;
                startTransform.rotation = boneTransform.localRotation.eulerAngles;

                handBinder.BoundGameobjects[index] = boneTransform;

                //If we found meta bones then we can turn on this option for the user
                if(boneType == Bone.BoneType.TYPE_METACARPAL) {
                    handBinder.UseMetaBones = true;
                }
            }

            else {
                warning += "\n" + fingerType.ToString() + " " + boneType.ToString() + " : Not Assigned";
            }
        }

        /// <summary>
        /// Calculate the rotation offset needed to get the rigged hand into the same orientation as the leap hand
        /// </summary>
        public static void CalculateWristRotationOffset(HandBinder handBinder) {
            //This function needs the following information
            //handBinder.boundGameobjects[9] = Middle Proximal
            //handBinder.boundGameobjects[5] = Index Proximal
            //handBinder.boundGameobjects[17] = Pinky Proximal
            //handBinder.boundGameobjects[20] = Wrist

            if(handBinder.BoundGameobjects[9] != null && handBinder.BoundGameobjects[5] != null && handBinder.BoundGameobjects[17] != null && handBinder.BoundGameobjects[20] != null) {
                //Get the Direction from the middle finger to the wrist
                var wristForward = handBinder.BoundGameobjects[9].transform.position - handBinder.BoundGameobjects[20].transform.position;
                //Get the Direction from the Proximal pinky finger to the Proximal Index finger
                var wristRight = handBinder.BoundGameobjects[5].transform.position - handBinder.BoundGameobjects[17].transform.position;

                //Swap the direction based on left and right hands
                if(handBinder.Handedness == Chirality.Right)
                    wristRight = -wristRight;

                //Get the direciton that goes outwards from the back of the hand
                var wristUp = Vector3.Cross(wristForward, wristRight);

                //Make the vectors orthoginal to eacother, this is the basis for the model hand
                Vector3.OrthoNormalize(ref wristRight, ref wristUp, ref wristForward);

                //Create a new leap hand based off the Desktop hand pose
                var hand = TestHandFactory.MakeTestHand(handBinder.Handedness == Chirality.Left, unitType: TestHandFactory.UnitType.LeapUnits);
                hand.Transform(TestHandFactory.GetTestPoseLeftHandTransform(TestHandFactory.TestHandPose.DesktopModeA));
                var leapRotation = hand.Rotation.ToQuaternion();

                //Get the rotation of the calculated hand Basis
                var modelRotation = Quaternion.LookRotation(wristForward, wristUp);

                //Now calculate the difference between the models rotation and the leaps rotation
                var wristRotationDifference = Quaternion.Inverse(modelRotation) * leapRotation;
                var wristRelativeDifference = (Quaternion.Inverse(handBinder.BoundGameobjects[20].transform.rotation) * wristRotationDifference).eulerAngles;

                //Assign these values to the hand binder
                handBinder.GlobalFingerRotationOffset = wristRelativeDifference;
                handBinder.WristRotationOffset = wristRelativeDifference;
            }
        }

        /// <summary>
        /// This is being used to return a specific index between 0 - 20 from a FingerType and a BoneType
        /// </summary>
        /// <param name="finger">The FingerType we use to find the index</param>
        /// <param name="bone">The BoneType we use to find the index</param>
        /// <returns></returns>
        public static int TypeToIndex(Finger.FingerType finger, Bone.BoneType bone) {
            var boneValue = (int)bone;
            var fingerValue = (int)finger * 4;

            return fingerValue + boneValue;
        }

        /// <summary>
        /// This is being used to return a specific FingerType and BoneType from an index between 0 - 20
        /// </summary>
        /// <param name="index">The index between 0 - 20</param>
        /// <param name="fingerType">The FingerType associated with this Index</param>
        /// <param name="boneType">The BoneType associated with this Index</param>
        /// <returns></returns>
        public static void IndexToType(int index, out Finger.FingerType fingerType, out Bone.BoneType boneType) {
            //The FingerType is the whole value and the BoneType we calculate from the decimal value
            //Example:
            //combinedIndexWithDecimal = 19 / 4 = 4.75
            //fingerIndex = 4
            //decimalValue = 4.75 - 4 = 0.75
            //boneIndex = 0.75 * 4 = 3

            var combinedIndexWithDecimal = (index / 4.0f);
            var fingerIndex = Mathf.FloorToInt(combinedIndexWithDecimal);
            var decimalValue = combinedIndexWithDecimal - fingerIndex;
            var boneIndex = (int)(decimalValue * 4.0f);

            fingerType = (Finger.FingerType)fingerIndex;
            boneType = (Bone.BoneType)boneIndex;
        }
    }
}