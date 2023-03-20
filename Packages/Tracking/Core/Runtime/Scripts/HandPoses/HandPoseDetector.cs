using Leap.Unity.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Leap.Unity
{
    [Serializable]
    public class PoseDetectionEvents : UnityEvent<HandPoseScriptableObject> { }

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

        [SerializeField]
        private HandPoseScriptableObject _poseToDetect;
        /// <summary>
        /// List of poses that the detector will check through. This list can be filled with different variations of the same pose.
        /// Once one of these poses is found, the class will call the "Pose Detected event"
        /// </summary>
        [SerializeField]
        private List<HandPoseScriptableObject> _posesToDetect;
        /// <summary>
        /// OPTIONAL. Specify a particular leap provider. If none is selected, the script will automatically find one in the scene.
        /// </summary>
        [SerializeField]
        private LeapProvider _leapProvider = null;


        /// <summary>
        /// Has a pose been detected since last time there was no pose was detected? 
        /// </summary>
        private bool _poseAlreadyDetected = false;
        /// <summary>
        /// Gives us the pose which has been detected.
        /// </summary>
        private HandPoseScriptableObject _detectedPose = null;
        /// <summary>
        /// did all of the bone directions match last frame? (For hysteresis)
        /// </summary>
        private bool _directionTargetsMatchedLastFrame = false;


        private bool cacheValidationData = false;

        public PoseDetectionEvents OnPoseDetected;
        public PoseDetectionEvents WhilePoseDetected;
        public UnityEvent OnPoseLost;

        public struct ValidationData
        {
            public int fingerNum;
            public int jointNum;
            public bool withinThreshold;
            public Chirality chirality;
            public ValidationData(Chirality _chirality, int _fingerNum, int _boneNum, bool _withinThreshold)
            {
                this.chirality = _chirality;
                this.fingerNum = _fingerNum;
                this.jointNum = _boneNum;
                this.withinThreshold = _withinThreshold;
            }
        }
        private List<ValidationData> _validationDatas = new List<ValidationData>();

        public List<ValidationData> GetValidationData()
        {
            return _validationDatas;
        }    

        public List<HandPoseScriptableObject> GetPosesToDetect()
        {
            return _posesToDetect;
        }

        public void SetPosesToDetect(List<HandPoseScriptableObject> posesToDetect)
        {
            _posesToDetect = posesToDetect;
        }

        public void AddPoseToDetect(HandPoseScriptableObject poseToAdd)
        {
            _posesToDetect.Add(poseToAdd);
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
            OBJECT = 0,
            WORLD = 1,
            CAMERALOCAL = 2
        };

        /// <summary>
        /// Adding the ablility of enum style direction selection in the Inspector.
        /// </summary>
        public enum AxisToFace { Back, Down, Forward, Left, Right, Up, Zero };
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
        public List<DirectionSource> Sources;
        //public List<FingerDirection> BoneDirectionTargets;

        [Serializable]
        public struct DirectionSource
        {
            public bool enabled;
            public int finger;
            public int bone;
            public List<SourceDirection> direction;
        }
        /// <summary>
        /// Holds information about different fingers, bones and their directional targets.
        /// </summary>
        [Serializable]
        public struct SourceDirection
        {
            public bool enabled;
            public TypeOfDirectionCheck typeOfDirectionCheck;
            public bool isPalmDirection;
            public Transform poseTarget;
            public AxisToFace axisToFace;
            public float rotationThreshold;
        }
        

        public void CreateDirectionSource()
        {
            DirectionSource directionSource = new DirectionSource();
            directionSource.enabled = true;
            directionSource.finger = (int)Finger.FingerType.TYPE_INDEX;
            directionSource.bone = (int)Bone.BoneType.TYPE_DISTAL;
            directionSource.direction = new List<SourceDirection>();

            SourceDirection sourceDirection = new SourceDirection();

            sourceDirection.typeOfDirectionCheck = TypeOfDirectionCheck.CAMERALOCAL;
            sourceDirection.isPalmDirection = false;
            sourceDirection.poseTarget = null;
            sourceDirection.axisToFace = AxisToFace.Forward;
            sourceDirection.rotationThreshold = 15;
            sourceDirection.enabled = true;

            directionSource.direction.Add(sourceDirection);
            Sources.Add(directionSource);
        }

        public void CreateSourceDirection(int sourceIndex)
        {
            SourceDirection fingerDirection = new SourceDirection();
            fingerDirection.typeOfDirectionCheck = TypeOfDirectionCheck.CAMERALOCAL;
            fingerDirection.isPalmDirection = false;
            fingerDirection.poseTarget = null;
            fingerDirection.axisToFace = AxisToFace.Forward;
            fingerDirection.rotationThreshold = 15;
            fingerDirection.enabled = true;

            Sources.ElementAt(sourceIndex).direction.Add(fingerDirection);
        }
        
        public void RemoveSource(int index)
        {
            Sources.RemoveAt(index);
        }
        public void RemoveDirection(int sourceIndex, int directionIndex)
        {
            Sources[sourceIndex].direction.RemoveAt(directionIndex);
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
            return _detectedPose;
        }

        private void Start()
        {
            if(_leapProvider == null)
            {
                _leapProvider = Hands.Provider;
            }

            //add the primary pose to the list of poses to be used
            if(_poseToDetect != null)
            {
                _posesToDetect.Add(_poseToDetect);
            }
        }

        // Update is called once per frame
        private void Update()
        {
            bool anyHandMatched = CompareAllHandsAndPoses();
            if (anyHandMatched && !_poseAlreadyDetected)
            {
                _poseAlreadyDetected = true;
                OnPoseDetected.Invoke(_detectedPose);
            }
            else if (!anyHandMatched && _poseAlreadyDetected)
            {
                _poseAlreadyDetected = false;
                OnPoseLost.Invoke();
                _detectedPose = null;
            }

            if(_detectedPose != null)
            {
                WhilePoseDetected.Invoke(_detectedPose);
            }
        }



        private bool CompareAllHandsAndPoses()
        {
            _validationDatas.Clear();
            foreach (var activePlayerHand in _leapProvider.CurrentFrame.Hands)
            {
                if ((CheckBothHands || activePlayerHand.GetChirality() == ChiralityToCheck))
                {
                    foreach (HandPoseScriptableObject pose in _posesToDetect)
                    {
                        bool poseDetectedThisFrame = ComparePoseToHand(pose, activePlayerHand);
                        if(poseDetectedThisFrame) 
                        {
                            _detectedPose = pose;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private bool ComparePoseToHand(HandPoseScriptableObject pose, Hand activePlayerHand)
        {

            // Check any finger directions set up in the pose detector
            if (Sources.Count > 0)
            {
                if (CheckPoseDirection(pose, activePlayerHand) == false)
                {
                    return false;
                }
            }
            
            Hand serializedHand = pose.GetSerializedHand();
            Hand playerHand = activePlayerHand;

            if(serializedHand.GetChirality() != playerHand.GetChirality())
            {
                serializedHand = pose.GetMirroredHand();
            }

            if (serializedHand == null || playerHand == null)
            {
                return false;
            }

            List<int> fingerIndexesToCheck = pose.GetFingerIndexesToCheck();
            int numMatchedFingers = 0;

            foreach (int fingerNum in fingerIndexesToCheck)
            {
                int numMatchedBones = 0;
                Quaternion lastBoneRotation = playerHand.Rotation;
                Quaternion lastSerializedBoneRotation = serializedHand.Rotation;

                // Each bone in the finger 
                for (int boneNum = 0; boneNum < serializedHand.Fingers[fingerNum].bones.Length; boneNum++)
                {
                    //Ignore the metacarpal as it will never really change.
                    if (serializedHand.Fingers[fingerNum].bones[boneNum].Type == Bone.BoneType.TYPE_METACARPAL)
                    {
                        Bone activeMetacarpalBone = playerHand.Fingers[fingerNum].bones[boneNum]; 
                        Bone serializedMetacarpalBone = serializedHand.Fingers[fingerNum].bones[boneNum];
                        lastBoneRotation = activeMetacarpalBone.Rotation;
                        lastSerializedBoneRotation = serializedMetacarpalBone.Rotation;
                        continue;
                    }

                    bool boneMatched = false;

                    // Get the same bone for both comparison hand and player hand
                    Bone activeHandBone = playerHand.Fingers[fingerNum].bones[boneNum];
                    Bone serializedHandBone = serializedHand.Fingers[fingerNum].bones[boneNum];

                    // Get the user defined rotation threshold for the current bone (threshold is defined in the pose scriptable object)
                    Vector2 jointRotationThresholds = GetBoneRotationThreshold(pose, fingerNum, boneNum-1); //i - 1 to ignore metacarpal

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
                    if (_poseAlreadyDetected)
                    {
                        if ((boneDifference.x <= (jointRotationThresholds.x + pose.GetHysteresisThreshold()) && boneDifference.x >= (-jointRotationThresholds.x - pose.GetHysteresisThreshold()))
                            && (boneDifference.y <= (jointRotationThresholds.y + pose.GetHysteresisThreshold()) && boneDifference.y >= (-jointRotationThresholds.y - pose.GetHysteresisThreshold())))
                        {
                            numMatchedBones++;
                            boneMatched = true;
                        }
                    }
                    // Otherwise, check if the difference between current hand and serialized hand is within the threshold.
                    else
                    {
                        if ((boneDifference.x <= jointRotationThresholds.x && boneDifference.x >= -jointRotationThresholds.x)
                            && (boneDifference.y <= jointRotationThresholds.y && boneDifference.y >= -jointRotationThresholds.y))
                        {
                            numMatchedBones++;
                            boneMatched = true;
                        }
                    }

                    if(cacheValidationData)
                    {
                        _validationDatas.Add(new ValidationData(serializedHand.GetChirality(), fingerNum, boneNum, boneMatched));
                    }

                }

                if(numMatchedBones >= 3)
                {
                    ++numMatchedFingers;
                    if (numMatchedFingers >= fingerIndexesToCheck.Count)
                    {
                        return true;
                    }
                }
            }

            return false;
        }


        private bool CheckPoseDirection(HandPoseScriptableObject pose, Hand activePlayerHand)
        {
            bool allBonesInCorrectDirection = true;
            foreach (var source in Sources)
            {
                
                bool oneDirectionCorrectInSource = false;
                if(source.direction.Count <= 0) 
                {
                    oneDirectionCorrectInSource = true;
                    continue;
                }
                
                foreach (var direction in source.direction)
                {
                    if(direction.enabled == false)
                    {
                        oneDirectionCorrectInSource = true;
                        continue;
                    }

                    if (direction.enabled == true)
                    {
                        Vector3 pointDirection;
                        Vector3 pointPosition;

                        if ((int)source.finger == 5)
                        {
                            pointDirection = activePlayerHand.PalmNormal.normalized;
                            pointPosition = activePlayerHand.PalmPosition;
                        }
                        else
                        {
                            pointDirection = activePlayerHand.Fingers[(int)source.finger].bones[source.bone].Direction.normalized;
                            pointPosition = activePlayerHand.Fingers[(int)source.finger].bones[source.bone].NextJoint;
                        }
                        float hysteresisToAdd = 0;

                        switch (direction.typeOfDirectionCheck)
                        {
                            case TypeOfDirectionCheck.OBJECT:
                                {
                                    if (_directionTargetsMatchedLastFrame)
                                    {
                                        hysteresisToAdd = -0.03f;
                                    }
                                    if (GetIsFacingObject(pointPosition, direction.poseTarget.position, pointDirection, 0.8f + hysteresisToAdd))
                                    {
                                        oneDirectionCorrectInSource = true;
                                    }
                                    break;
                                }
                            case TypeOfDirectionCheck.WORLD:
                                {
                                    if (_directionTargetsMatchedLastFrame)
                                    {
                                        hysteresisToAdd = 5f;
                                    }
                                    if (GetIsFacingDirection(pointDirection, GetAxis(direction.axisToFace), direction.rotationThreshold + hysteresisToAdd))
                                    {
                                        oneDirectionCorrectInSource = true;
                                    }
                                    break;
                                }
                            case TypeOfDirectionCheck.CAMERALOCAL:
                                {
                                    if (_directionTargetsMatchedLastFrame)
                                    {
                                        hysteresisToAdd = 5f;
                                    }
                                    if (GetIsFacingDirection(pointDirection,
                                        (Camera.main.transform.rotation.normalized * GetAxis(direction.axisToFace).normalized).normalized, direction.rotationThreshold + hysteresisToAdd))
                                    {
                                        oneDirectionCorrectInSource = true;
                                    }
                                    break;
                                }
                        }
                    }

                }
                if(oneDirectionCorrectInSource == false)
                {
                    allBonesInCorrectDirection = false;
                }
            }
            _directionTargetsMatchedLastFrame = allBonesInCorrectDirection;
            return allBonesInCorrectDirection;
        }

        public bool IsPoseCurrentlyDetected()
        {
            return _poseAlreadyDetected;
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

        private float GetAverageDegreeAngleDifferenceXY(Vector3 a, Vector3 b)
        {
            var averageAngle = (Mathf.DeltaAngle(a.x, b.x) + Mathf.DeltaAngle(a.y, b.y)) / 2;

            return averageAngle;
        }

        private bool GetIsFacingDirection(Vector3 boneDirection, Vector3 TargetDirectionDirection, float thresholdInDegrees)
        {
            return(Vector3.Angle(boneDirection.normalized, TargetDirectionDirection.normalized) < thresholdInDegrees);
        }

        private Vector2 GetBoneRotationThreshold(HandPoseScriptableObject pose, int fingerNum, int boneNum)
        {
            return pose.GetBoneRotationthreshold(fingerNum, boneNum);
        }
        #endregion
    }
}
