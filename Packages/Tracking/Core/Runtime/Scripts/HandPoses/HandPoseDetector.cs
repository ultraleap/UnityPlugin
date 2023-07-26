using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.Events;

namespace Leap.Unity
{
    public class HandPoseDetector : MonoBehaviour
    {
        /// <summary>
        /// Should the detector try to match this pose with both hands?
        /// Untick to enable single hand poses. E.g. only allowing left hand to activate pose.
        /// </summary>
        [HideInInspector]
        public bool checkBothHands = true;

        /// <summary>
        /// Which hand should the detector check?
        /// </summary>
        [HideInInspector]
        public Chirality chiralityToCheck;

        [SerializeField]
        private HandPoseScriptableObject poseToDetect;

        /// <summary>
        /// List of poses that the detector will check through. This list can be filled with different variations of the same pose.
        /// Once one of these poses is found, the class will call the "Pose Detected event"
        /// </summary>
        [SerializeField]
        private List<HandPoseScriptableObject> posesToDetect;
        /// <summary>
        /// OPTIONAL. Specify a particular leap provider. If none is selected, the script will automatically find one in the scene.
        /// </summary>
        [SerializeField]
        private LeapProvider leapProvider = null;

        /// <summary>
        /// Has a pose been detected since last time there was no pose was detected? 
        /// </summary>
        private bool poseAlreadyDetected = false;

        /// <summary>
        /// Gives us the pose which has been detected.
        /// </summary>
        private HandPoseScriptableObject detectedPose = null;

        /// <summary>
        /// did all of the bone directions match last frame? (For hysteresis)
        /// </summary>
        private bool allRulesMatchedLastFrame = false;

        private bool cacheValidationData = false;

        public UnityEvent OnPoseDetected;
        public UnityEvent WhilePoseDetected;
        public UnityEvent OnPoseLost;

        public struct ValidationData
        {
            public int fingerNum;
            public int boneNum;
            public bool withinThreshold;
            public Chirality chirality;

            public ValidationData(Chirality chirality, int fingerNum, int boneNum, bool withinThreshold)
            {
                this.chirality = chirality;
                this.fingerNum = fingerNum;
                this.boneNum = boneNum;
                this.withinThreshold = withinThreshold;
            }
        }

        private List<ValidationData> validationDatas = new List<ValidationData>();

        public List<ValidationData> GetValidationData()
        {
            return validationDatas;
        }

        public List<HandPoseScriptableObject> GetPosesToDetect()
        {
            return posesToDetect;
        }

        public void SetPosesToDetect(List<HandPoseScriptableObject> posesToDetect)
        {
            this.posesToDetect = posesToDetect;
        }

        public void AddPoseToDetect(HandPoseScriptableObject poseToAdd)
        {
            posesToDetect.Add(poseToAdd);
        }

        public void EnablePoseCaching()
        {
            cacheValidationData = true;
        }

        #region poseDirectionVariables
        /// <summary>
        /// What type of directionality is this check? e.g. pointing towards and object or a world direction.
        /// </summary>
        public enum TypeOfDirectionCheck
        {
            TowardsObject = 0,
            WorldDirection = 1,
            CameraDirection = 2
        };

        /// <summary>
        /// Adding the ablility of enum style direction selection in the Inspector.
        /// </summary>
        public enum AxisToFace
        {
            Back,
            Down,
            Forward,
            Left,
            Right,
            Up
        };

        private static readonly Vector3[] vectorAxes = new Vector3[]
        {
            Vector3.back,
            Vector3.down,
            Vector3.forward,
            Vector3.left,
            Vector3.right,
            Vector3.up
        };

        public Vector3 GetAxis(AxisToFace axis)
        {
            return vectorAxes[(int)axis];
        }

        /// <summary>
        /// Add more here to put constraints on when the pose is detected. 
        /// E.g. if the user needs to point at an object with their index finger for the pose to be considered detected"
        /// </summary>
        [SerializeField]
        public List<PoseRule> poseRules;

        [Serializable]
        public struct PoseRule
        {
            public bool enabled;
            public int finger;
            public int bone;
            public List<RuleDirection> directions;
        }
        /// <summary>
        /// Holds information about different fingers, bones and their directional targets.
        /// </summary>
        [Serializable]
        public struct RuleDirection
        {
            public bool enabled;
            public TypeOfDirectionCheck typeOfDirectionCheck;
            public bool isPalmDirection;
            public Transform poseTarget;
            public AxisToFace axisToFace;
            public float rotationThreshold;
        }

        public void CreatePoseRule()
        {
            PoseRule poseRule = new PoseRule();
            poseRule.enabled = true;
            poseRule.finger = (int)Finger.FingerType.TYPE_INDEX;
            poseRule.bone = (int)Bone.BoneType.TYPE_INTERMEDIATE;
            poseRule.directions = new List<RuleDirection>();

            poseRules.Add(poseRule);

            CreateRuleDirection(poseRules.Count - 1);
        }

        public void CreateRuleDirection(int sourceIndex)
        {
            RuleDirection fingerDirection = new RuleDirection();
            fingerDirection.typeOfDirectionCheck = TypeOfDirectionCheck.CameraDirection;
            fingerDirection.isPalmDirection = false;
            fingerDirection.poseTarget = null;
            fingerDirection.axisToFace = AxisToFace.Forward;
            fingerDirection.rotationThreshold = 45;
            fingerDirection.enabled = true;

            poseRules.ElementAt(sourceIndex).directions.Add(fingerDirection);
        }

        public void RemoveRule(int index)
        {
            poseRules.RemoveAt(index);
        }

        public void RemoveRuleDirection(int ruleIndex, int directionIndex)
        {
            poseRules[ruleIndex].directions.RemoveAt(directionIndex);
        }

        public List<PoseRuleCache> poseRulesForValidator = new List<PoseRuleCache>();
        public struct PoseRuleCache
        {
            public Chirality chirality;
            public Tuple<PoseRule, bool> poseRuleAndStatus;
            public PoseRuleCache(Chirality chirality, Tuple<PoseRule, bool> poseRuleAndStatus)
            {
                this.chirality = chirality;
                this.poseRuleAndStatus = poseRuleAndStatus;
            }
        }

        #endregion

        /// <summary>
        /// This function determines whether a pose is currently detected or not.
        /// If a pose is currently detected, it will return the pose
        /// If a pose is not detected then this will return null
        /// </summary>
        /// <returns> currently detected pose (will be null if no pose is currently detected)</returns>
        public HandPoseScriptableObject GetCurrentlyDetectedPose()
        {
            return detectedPose;
        }

        private void Start()
        {
            if (leapProvider == null)
            {
                leapProvider = Hands.Provider;
            }

            //add the primary pose to the list of poses to be used
            if (poseToDetect != null)
            {
                posesToDetect.Add(poseToDetect);
            }
        }

        private void Update()
        {
            bool anyHandMatched = CompareAllHandsAndPoses();
            if (anyHandMatched && !poseAlreadyDetected)
            {
                poseAlreadyDetected = true;
                OnPoseDetected.Invoke();
            }
            else if (!anyHandMatched && poseAlreadyDetected)
            {
                poseAlreadyDetected = false;
                OnPoseLost.Invoke();
                detectedPose = null;
            }

            if (detectedPose != null)
            {
                WhilePoseDetected.Invoke();
            }
        }

        private bool CompareAllHandsAndPoses()
        {
            bool poseDetectedOnEitherHand = false;
            validationDatas.Clear();
            poseRulesForValidator.Clear();

            if (leapProvider != null && leapProvider.CurrentFrame != null)
            {
                foreach (var activePlayerHand in leapProvider.CurrentFrame.Hands)
                {
                    if ((checkBothHands || activePlayerHand.GetChirality() == chiralityToCheck))
                    {
                        foreach (HandPoseScriptableObject pose in posesToDetect)
                        {
                            bool poseDetectedThisFrame = ComparePoseToHand(pose, activePlayerHand);
                            if (poseDetectedThisFrame)
                            {
                                detectedPose = pose;
                                poseDetectedOnEitherHand = true;
                            }
                        }
                    }
                }
            }
            return poseDetectedOnEitherHand;
        }

        private bool ComparePoseToHand(HandPoseScriptableObject pose, Hand playerHand)
        {

            // Check any finger directions set up in the pose detector
            if (poseRules.Count > 0)
            {
                if (CheckPoseDirection(playerHand) == false)
                {
                    return false;
                }
            }

            Hand serializedHand = pose.GetSerializedHand();

            if (serializedHand == null || playerHand == null)
            {
                return false;
            }

            // Swap to mirrored hand depending on chirality of playerHand
            if (serializedHand.GetChirality() != playerHand.GetChirality())
            {
                serializedHand = pose.GetMirroredHand();
            }

            bool allFingersMatched = true;
            List<int> fingerIndexesToCheck = pose.GetFingerIndexesToCheck();

            foreach (int fingerNum in fingerIndexesToCheck)
            {
                Quaternion lastBoneRotation = playerHand.Rotation;
                Quaternion lastSerializedBoneRotation = serializedHand.Rotation;

                // Each bone in the finger 
                for (int boneNum = 0; boneNum < serializedHand.Fingers[fingerNum].bones.Length; boneNum++)
                {
                    // Get the same bone for both comparison hand and player hand
                    Bone activeHandBone = playerHand.Fingers[fingerNum].bones[boneNum];
                    Bone serializedHandBone = serializedHand.Fingers[fingerNum].bones[boneNum];

                    //Ignore the metacarpal as it will never really change.
                    if (serializedHandBone.Type == Bone.BoneType.TYPE_METACARPAL)
                    {
                        lastBoneRotation = activeHandBone.Rotation;
                        lastSerializedBoneRotation = serializedHandBone.Rotation;
                        continue;
                    }

                    // Do not check distal for fingers, only for thumb
                    if (fingerNum != 0 &&
                        serializedHandBone.Type == Bone.BoneType.TYPE_DISTAL)
                    {
                        continue;
                    }

                    bool boneMatched = false;

                    // Get the user defined rotation threshold for the current bone (threshold is defined in the pose scriptable object)
                    Vector2 jointRotationThresholds = GetBoneRotationThreshold(pose, fingerNum, boneNum - 1); //i - 1 to ignore metacarpal

                    // Get hand rotation for both hands
                    Quaternion activeBoneRotation = activeHandBone.Rotation;
                    Quaternion serializedBoneRotation = serializedHandBone.Rotation;

                    // Calculate the vector3 rotation between the current bone and the previous one.
                    Vector3 activeRotEuler = (Quaternion.Inverse(lastBoneRotation) * activeBoneRotation).eulerAngles;
                    Vector3 serializedRotEuler = (Quaternion.Inverse(lastSerializedBoneRotation) * serializedBoneRotation).eulerAngles;

                    // Calculate angle difference between the active hand and the serialized hand.
                    Vector2 boneDifference = GetDegreeAngleDifferenceXY(serializedRotEuler, activeRotEuler);

                    lastBoneRotation = activeBoneRotation;
                    lastSerializedBoneRotation = serializedBoneRotation;

                    // If the pose has been detected, use the Hysteresis value to check if it should be undetected
                    if (poseAlreadyDetected)
                    {
                        if (boneDifference.x <= (jointRotationThresholds.x + pose.GetHysteresisThreshold()) && boneDifference.x >= (-jointRotationThresholds.x - pose.GetHysteresisThreshold()))
                        {
                            if (serializedHandBone.Type == Bone.BoneType.TYPE_PROXIMAL) // Proximal also uses Y rotation for Abduction/Splay
                            {
                                if (boneDifference.y <= (jointRotationThresholds.y + pose.GetHysteresisThreshold()) && boneDifference.y >= (-jointRotationThresholds.y - pose.GetHysteresisThreshold()))
                                {
                                    boneMatched = true;
                                }
                            }
                            else
                            {
                                boneMatched = true;
                            }
                        }
                    }
                    // Otherwise, check if the difference between current hand and serialized hand is within the threshold.
                    else
                    {
                        if (boneDifference.x <= jointRotationThresholds.x && boneDifference.x >= -jointRotationThresholds.x)
                        {
                            if (serializedHandBone.Type == Bone.BoneType.TYPE_PROXIMAL) // Proximal also uses Y rotation for Abduction/Splay
                            {
                                if (boneDifference.y <= jointRotationThresholds.y && boneDifference.y >= -jointRotationThresholds.y)
                                {
                                    boneMatched = true;
                                }
                            }
                            else
                            {
                                boneMatched = true;
                            }
                        }
                    }

                    if (cacheValidationData)
                    {
                        validationDatas.Add(new ValidationData(serializedHand.GetChirality(), fingerNum, boneNum, boneMatched));
                    }

                    if (!boneMatched)
                    {
                        allFingersMatched = false;
                    }
                }
            }

            if (allFingersMatched)
            {
                return true;
            }

            return false;
        }

        private bool CheckPoseDirection(Hand playerHand)
        {
            bool allRulesPassed = true;
            foreach (var rule in poseRules)
            {
                bool directionMatchInRule = false;
                if (rule.directions.Count <= 0) // If there are no directions for this rule, skip this rule
                {
                    directionMatchInRule = true;
                    continue;
                }

                foreach (var direction in rule.directions)
                {
                    if (direction.enabled == false)
                    {
                        continue;
                    }
                    else // This is an active direction, check that it is correct
                    {
                        Vector3 pointDirection;
                        Vector3 pointPosition;

                        if ((int)rule.finger == 5) // This represents the palm
                        {
                            pointDirection = playerHand.PalmNormal.normalized;
                            pointPosition = playerHand.PalmPosition;
                        }
                        else
                        {
                            pointDirection = playerHand.Fingers[(int)rule.finger].bones[rule.bone].Direction.normalized;
                            pointPosition = playerHand.Fingers[(int)rule.finger].bones[rule.bone].NextJoint;
                        }

                        float hysteresisToAdd = 0;

                        if (allRulesMatchedLastFrame)
                        {
                            hysteresisToAdd = 5f;
                        }

                        switch (direction.typeOfDirectionCheck)
                        {
                            case TypeOfDirectionCheck.TowardsObject:
                                {
                                    float angleToDot = direction.rotationThreshold + hysteresisToAdd;
                                    angleToDot = 1 - (Mathf.Clamp01(angleToDot / 180) * 2);

                                    if (GetIsFacingObject(pointPosition, direction.poseTarget.position, pointDirection, angleToDot))
                                    {
                                        directionMatchInRule = true;
                                    }
                                    break;
                                }
                            case TypeOfDirectionCheck.WorldDirection:
                                {
                                    if (GetIsFacingDirection(pointDirection, GetAxis(direction.axisToFace), direction.rotationThreshold + hysteresisToAdd))
                                    {
                                        directionMatchInRule = true;
                                    }
                                    break;
                                }
                            case TypeOfDirectionCheck.CameraDirection:
                                {
                                    if (GetIsFacingDirection(pointDirection,
                                        (Camera.main.transform.rotation.normalized * GetAxis(direction.axisToFace).normalized).normalized, direction.rotationThreshold + hysteresisToAdd))
                                    {
                                        directionMatchInRule = true;
                                    }
                                    break;
                                }
                        }
                        poseRulesForValidator.Add(new PoseRuleCache(playerHand.GetChirality(),
                            new Tuple<PoseRule, bool>(rule, directionMatchInRule)));
                    }
                }

                if (directionMatchInRule == false)
                {
                    allRulesPassed = false;
                }
            }

            allRulesMatchedLastFrame = allRulesPassed;
            return allRulesPassed;
        }

        public bool IsPoseCurrentlyDetected()
        {
            return poseAlreadyDetected;
        }

        #region Helper Functions
        private static bool GetIsFacingObject(Vector3 bonePosition, Vector3 comparisonPosition, Vector3 boneDirection, float minAllowedDotProduct = 0.8F)
        {
            return Vector3.Dot((comparisonPosition - bonePosition).normalized, boneDirection.normalized) > minAllowedDotProduct;
        }

        private Vector2 GetDegreeAngleDifferenceXY(Vector3 a, Vector3 b)
        {
            var angleX = Mathf.DeltaAngle(a.x, b.x);
            var angleY = Mathf.DeltaAngle(a.y, b.y);

            return new Vector2(angleX, angleY);
        }

        private bool GetIsFacingDirection(Vector3 boneDirection, Vector3 TargetDirectionDirection, float thresholdInDegrees)
        {
            return (Vector3.Angle(boneDirection.normalized, TargetDirectionDirection.normalized) < thresholdInDegrees);
        }

        private Vector2 GetBoneRotationThreshold(HandPoseScriptableObject pose, int fingerNum, int boneNum)
        {
            return pose.GetBoneRotationthreshold(fingerNum, boneNum);
        }
        #endregion
    }
}