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
        /// A List of serialized hand poses to detect. Poses can be created using the "Hand Pose Recoder".
        /// Useful for multiple variations of the same pose i.e. left and right thumbs up.
        /// </summary>

        [HideInInspector]
        public bool CheckBothHands = true;
        [HideInInspector]
        public Chirality ChiralityToCheck;

        [SerializeField]
        private List<HandPoseScriptableObject> _posesToDetect;
        [SerializeField]
        private HandPoseValidator _handPoseValidator = null;
        [SerializeField]
        private LeapProvider _leapProvider = null;
        [SerializeField]
        private float _hysteresisThreshold = 5;



        public static event Action<HandPoseScriptableObject> PoseHasBeenDetected;
        public static event Action PoseHasNotBeenDetected;

        private void Start()
        {
            PoseHasBeenDetected += PoseDetected;
            PoseHasNotBeenDetected += PoseNotDetected;
            if(_leapProvider == null)
            {
                _leapProvider = FindObjectOfType<LeapProvider>();
            }
        }
        private void PoseDetected(HandPoseScriptableObject poseScriptableObject) { }
        private void PoseNotDetected() { }

        bool poseAlreadyDetected = false;
        // Update is called once per frame
        void Update()
        {
            bool anyHandMatched = CompareAllHandsAndPoses();
            if (anyHandMatched && !poseAlreadyDetected)
            {
                poseAlreadyDetected = true;
                PoseHasBeenDetected(poseAndHandDetected.Item1);
                Debug.Log("pose Detected");
            }
            else if (!anyHandMatched && poseAlreadyDetected)
            {
                poseAlreadyDetected = false;
                PoseHasNotBeenDetected();
                Debug.Log("pose Un Detected");
            }
        }


        Tuple<HandPoseScriptableObject, Chirality> poseAndHandDetected = null;

        private bool CompareAllHandsAndPoses()
        {
            // If the user hasnt specified the hands to detect, check all Hand Model Bases.
            // This will only do this once unless manually cleared.
            poseAndHandDetected = null;

            foreach (var activePlayerHand in _leapProvider.CurrentFrame.Hands)
            {
                if ((!CheckBothHands && activePlayerHand.GetChirality() == ChiralityToCheck) || CheckBothHands)
                {
                    foreach (HandPoseScriptableObject pose in _posesToDetect)
                    {
                        bool poseDetectedThisFrame = ComparePoseToHand(pose, activePlayerHand);
                        if(poseDetectedThisFrame) 
                        {
                            poseAndHandDetected = new Tuple<HandPoseScriptableObject, Chirality>(pose, ChiralityToCheck);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private bool ComparePoseToHand(HandPoseScriptableObject pose, Hand activePlayerHand)
        {
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

                    Quaternion activeBoneRotation = activeHandBone.Rotation;
                    Quaternion serializedBoneRotation = serializedHandBone.Rotation;

                    Vector3 activeRotEuler = (Quaternion.Inverse(lastBoneRotation) * activeBoneRotation).eulerAngles;
                    Vector3 serializedRotEuler = (Quaternion.Inverse(lastSerializedBoneRotation) * serializedBoneRotation).eulerAngles;

                    float boneDifference = GetDegreeAngleDifferenceXY(serializedRotEuler, activeRotEuler);

                    lastBoneRotation = activeBoneRotation;
                    lastSerializedBoneRotation = serializedBoneRotation;

                    if(_handPoseValidator != null)
                    {
                        _handPoseValidator.ShowJointColour(fingerNum, boneNum, boneDifference, jointRotationThreshold);
                    }

                    if (poseAlreadyDetected)
                    {
                        if (boneDifference <= (jointRotationThreshold + _hysteresisThreshold) && boneDifference >= (-jointRotationThreshold - _hysteresisThreshold))
                        {
                            numMatchedBones++;
                        }
                    }
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

        [SerializeField]
        Transform poseTarget;
        
        


        private bool CheckPoseOrientation(HandPoseScriptableObject pose, Hand activePlayerHand)
        {
            var FaceTowardDirection = activePlayerHand.GetIndex().Bone(Bone.BoneType.TYPE_PROXIMAL).Direction;

            if (GetIsFacingDirection(activePlayerHand.PalmPosition, poseTarget.position, activePlayerHand.PalmNormal))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool GetIsFacingDirection(Vector3 ObjectPosition, Vector3 ComparisonPosition, Vector3 ObjectOrientation, float minAllowedDotProduct = 0.8F)
        {
            return Vector3.Dot((ComparisonPosition - ObjectPosition).normalized, ObjectOrientation) > minAllowedDotProduct;
        }

        Vector3 GetEulerAngleDifference(Vector3 a, Vector3 b)
        {
            return new Vector3(Mathf.DeltaAngle(a.x, b.x), Mathf.DeltaAngle(a.y, b.y), Mathf.DeltaAngle(a.z, b.z));
        }

        float GetDegreeAngleDifferenceXY(Vector3 a, Vector3 b)
        {
            var averageAngle = (Mathf.DeltaAngle(a.x, b.x) + Mathf.DeltaAngle(a.y, b.y)/* + Mathf.DeltaAngle(a.z, b.z)*/)/2;
            return averageAngle;
        }

        float GetDegreeAngleDifferenceXYZ(Vector3 a, Vector3 b)
        {
            var averageAngle = Vector3.Angle(b, a);

            //var averageAngle = (Mathf.DeltaAngle(a.x, b.x) + Mathf.DeltaAngle(a.y, b.y)+ Mathf.DeltaAngle(a.z, b.z)) / 3;
            return averageAngle;
        }

        private float GetBoneRotationThreshold(HandPoseScriptableObject pose, int fingerNum, int boneNum)
        {
            return pose.GetBoneRotationthreshold(fingerNum, boneNum);
        }
    }
}
