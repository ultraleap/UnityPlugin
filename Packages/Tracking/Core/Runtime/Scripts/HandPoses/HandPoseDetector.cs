using LeapInternal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Leap.Unity
{
    public class HandPoseDetector : MonoBehaviour
    {
        /// <summary>
        /// Should the detector try to match this pose with both hands?
        /// Untick to enable single hand poses. E.g. only allowing left hand to activate pose.
        /// </summary>
        [HideInInspector]
        public bool CheckBothHands = true;
        /// <summary>
        /// Which hand should the detector check?
        /// </summary>
        [HideInInspector]
        public Chirality ChiralityToCheck;
        /// <summary>
        /// List of poses that the detector will check through. This list can be filled with different poses or variations of the same pose.
        /// Once one of these poses is found, the class will call the "Pose Detected event"
        /// </summary>
        [SerializeField]
        private List<HandPoseScriptableObject> _posesToDetect;
        /// <summary>
        /// OPTIONAL. The object which has the "HandPoseValidator" script on it. This will allow the capsule hands to act as validators
        /// with the joint spheres changing colour the closer to the pose they get.
        /// </summary>
        [SerializeField]
        private HandPoseValidator _handPoseValidator = null;
        /// <summary>
        /// OPTIONAL. Specify a particular leap provider. If none is selected, the script will automatically find one in the scene.
        /// </summary>
        [SerializeField]
        private LeapProvider _leapProvider = null;
        /// <summary>
        /// The distance a bone must move away from being detected before the pose is no longer enabled.
        /// This means that users cannot hover just on the edge of a detection and cause it to send rapid detections while straying still.
        /// E.g. Detection threshold is 15 degrees, so when the user gets within 15 degrees, detection will occur.
        /// Hysteresis threshold is 5 so the user need to move 20 degrees from the pose before the detection will drop.
        /// </summary>
        [SerializeField]
        private float _hysteresisThreshold = 5;
        /// <summary>
        /// Should the pose use the palm direction to orient the pose?
        /// </summary>
        [SerializeField]
        private bool _usePalmDirectionForPoseOrientation = false;

        /// <summary>
        /// Add more here to put constraints on when the pose is detected. 
        /// E.g. if the user needs to point at an object with their index finger for the pose to be considered detected"
        /// </summary>
        [SerializeField]
        public List<FingerDirection> BoneDirectionTargets;

        /// <summary>
        /// Holds information about different fingers, bones and their directional targets.
        /// </summary>
        [Serializable]
        public struct FingerDirection
        {
            public Transform poseTarget;
            public Finger.FingerType fingerTypeForPoint;
            public Bone.BoneType boneForPoint;
        }

        /// <summary>
        /// Has a pose been detected since last time there was no pose was detected? 
        /// </summary>
        private bool _poseAlreadyDetected = false;
        /// <summary>
        /// Gives us the pose which has been detected.
        /// </summary>
        private HandPoseScriptableObject detectedPose = null;


        public static event Action<HandPoseScriptableObject> PoseHasBeenDetected;
        public static event Action PoseHasNotBeenDetected;

        private void Start()
        {
            //PoseHasBeenDetected += PoseDetected;
            //PoseHasNotBeenDetected += PoseNotDetected;
            if(_leapProvider == null)
            {
                _leapProvider = FindObjectOfType<LeapProvider>();
            }
        }
        //private void PoseDetected(HandPoseScriptableObject poseScriptableObject) { }
        //private void PoseNotDetected() { }


        // Update is called once per frame
        private void Update()
        {
            bool anyHandMatched = CompareAllHandsAndPoses();
            if (anyHandMatched && !_poseAlreadyDetected)
            {
                _poseAlreadyDetected = true;
                PoseHasBeenDetected(detectedPose);
                Debug.Log("pose Detected");
            }
            else if (!anyHandMatched && _poseAlreadyDetected)
            {
                _poseAlreadyDetected = false;
                PoseHasNotBeenDetected();
                Debug.Log("pose Un Detected");
            }
        }




        private bool CompareAllHandsAndPoses()
        {
            // If the user hasnt specified the hands to detect, check all Hand Model Bases.
            // This will only do this once unless manually cleared.
            detectedPose = null;

            foreach (var activePlayerHand in _leapProvider.CurrentFrame.Hands)
            {
                if ((!CheckBothHands && activePlayerHand.GetChirality() == ChiralityToCheck) || CheckBothHands)
                {
                    foreach (HandPoseScriptableObject pose in _posesToDetect)
                    {
                        bool poseDetectedThisFrame = ComparePoseToHand(pose, activePlayerHand);
                        if(poseDetectedThisFrame) 
                        {
                            detectedPose = pose;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private bool ComparePoseToHand(HandPoseScriptableObject pose, Hand activePlayerHand)
        {
            // Do we care about the orientation of the hand or fingers?
            if (pose._careAboutOrientation)
            {
                if (CheckPoseOrientation(pose, activePlayerHand) == false) { return false; }
            }
            
            Hand serializedHand = pose.GetSerializedHand();
            Hand playerHand = activePlayerHand;
            int numMatchedFingers = 0;

            if (serializedHand == null || playerHand == null)
            {
                return false;
            }

            List<int> fingerIndexesToCheck = pose.GetFingerIndexesToCheck();

            foreach (int fingerNum in fingerIndexesToCheck)
            {
                int numMatchedBones = 0;
                Quaternion lastBoneRotation = playerHand.Rotation;
                Quaternion lastSerializedBoneRotation = serializedHand.Rotation;

                // Each bone in the finger 
                for (int boneNum = 0; boneNum < serializedHand.Fingers[fingerNum].bones.Length; boneNum++)
                {
                    // Get the same bone for both comparison hand and player hand
                    Bone activeHandBone = playerHand.Fingers[fingerNum].bones[boneNum];
                    Bone serializedHandBone = serializedHand.Fingers[fingerNum].bones[boneNum];

                    // Get the user defined rotation threshold for the current bone (threshold is defined in the pose scriptable object)
                    float jointRotationThreshold = GetBoneRotationThreshold(pose, fingerNum, boneNum);

                    // Get hand rotation for both hands
                    Quaternion activeBoneRotation = activeHandBone.Rotation;
                    Quaternion serializedBoneRotation = serializedHandBone.Rotation;

                    // Calculate the vector3 rotation between the current bone and the previous one.
                    Vector3 activeRotEuler = (Quaternion.Inverse(lastBoneRotation) * activeBoneRotation).eulerAngles;
                    Vector3 serializedRotEuler = (Quaternion.Inverse(lastSerializedBoneRotation) * serializedBoneRotation).eulerAngles;

                    // Calculate angle difference between the active hand and the serialized hand.
                    float boneDifference = GetDegreeAngleDifferenceXY(serializedRotEuler, activeRotEuler);

                    lastBoneRotation = activeBoneRotation;
                    lastSerializedBoneRotation = serializedBoneRotation;

                    // If there is a pose validator present, show the joint colours.
                    if(_handPoseValidator != null)
                    {
                        _handPoseValidator.ShowJointColour(fingerNum, boneNum, boneDifference, jointRotationThreshold);
                    }

                    // If the pose has been detected, use the Hysteresis value to check if it should be undetected
                    if (_poseAlreadyDetected)
                    {
                        if (boneDifference <= (jointRotationThreshold + _hysteresisThreshold) && boneDifference >= (-jointRotationThreshold - _hysteresisThreshold))
                        {
                            numMatchedBones++;
                        }
                    }
                    // Otherwise, check if the difference between current hand and serialized hand is within the threshold.
                    else
                    {
                        if (boneDifference <= jointRotationThreshold && boneDifference >= -jointRotationThreshold)
                        {
                            numMatchedBones++;
                        }
                    }
                }

                if(numMatchedBones >= pose.GetNumBonesForMatch(fingerNum))
                {
                    ++numMatchedFingers;
                }
            }
            if (numMatchedFingers >= fingerIndexesToCheck.Count)
            {
                return true;
            }
            else
            {
                return false;
            }
        }



        private bool CheckPoseOrientation(HandPoseScriptableObject pose, Hand activePlayerHand)
        {
            bool allBonesInCorrectDirection = true;

            if (BoneDirectionTargets.Count > 0)
            {
                foreach (var item in BoneDirectionTargets)
                {
                    Vector3 pointDirection;
                    Vector3 pointPosition;

                    if (_usePalmDirectionForPoseOrientation)
                    {
                        pointDirection = activePlayerHand.PalmNormal.normalized;
                        pointPosition = activePlayerHand.PalmPosition;
                    }
                    else
                    {
                        pointDirection = activePlayerHand.Fingers[(int)item.fingerTypeForPoint].Bone(item.boneForPoint).Direction.normalized;
                        pointPosition = activePlayerHand.Fingers[(int)item.fingerTypeForPoint].Bone(item.boneForPoint).NextJoint;
                    }

                    if (!GetIsFacingDirection(pointPosition, item.poseTarget.position, pointDirection))
                    {
                        allBonesInCorrectDirection = false;
                    }
                }

                return allBonesInCorrectDirection;
            }
            else
            {
                Debug.Log("Ignoring Orientation, please assign some finger directions from the inspector.");
                return true;
            }
        }

        private static bool GetIsFacingDirection(Vector3 bonePosition, Vector3 comparisonPosition, Vector3 boneDirection, float minAllowedDotProduct = 0.8F)
        {
            return Vector3.Dot((comparisonPosition - bonePosition).normalized, boneDirection.normalized) > minAllowedDotProduct;
        }

        private float GetDegreeAngleDifferenceXY(Vector3 a, Vector3 b)
        {
            var averageAngle = (Mathf.DeltaAngle(a.x, b.x) + Mathf.DeltaAngle(a.y, b.y))/2;
            return averageAngle;
        }

        private float GetBoneRotationThreshold(HandPoseScriptableObject pose, int fingerNum, int boneNum)
        {
            return pose.GetBoneRotationthreshold(fingerNum, boneNum);
        }
    }
}
