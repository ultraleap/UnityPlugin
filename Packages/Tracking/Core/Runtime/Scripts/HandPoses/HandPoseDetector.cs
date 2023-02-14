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
        [SerializeField]
        List<HandPoseScriptableObject> PosesToDetect;

        /// <summary>
        /// Which hand would you like to use for gesture recognition?
        /// If this is left blank, It will search for all hands in the scene
        /// </summary>
        [SerializeField]
        List<CapsuleHand> handsToDetect = new();


        [SerializeField]
        LeapProvider leapProvider;

        /// <summary>
        /// How many bones need to match per finger?
        /// Note by default, a hand has 4 bones.
        /// </summary>
        [SerializeField]
        int perFingerBoneMatches = 3;

        public static event Action<HandPoseScriptableObject> PoseHasBeenDetected;
        public static event Action PoseHasNotBeenDetected;

        private void Start()
        {
            PoseHasBeenDetected += PoseDetected;
            PoseHasNotBeenDetected += PoseNotDetected;
        }
        private void PoseDetected(HandPoseScriptableObject poseScriptableObject) { }
        private void PoseNotDetected() { }

        


        // Update is called once per frame
        void Update()
        {
            CompareHandShapes();
        }

        private void CompareHandShapes()
        {
            // If the user hasnt specified the hands to detect, check all Hand Model Bases.
            // This will only do this once unless manually cleared.
            if (handsToDetect.Count <= 0)
            {
                handsToDetect = GameObject.FindObjectsOfType<CapsuleHand>().ToList();
                if(handsToDetect.Count <= 0) 
                {
                    Debug.Log("Skipping pose detection, there are no Leap hands in the scene");
                    return;
                }
            }

            foreach (var activePlayerHand in leapProvider.CurrentFrame.Hands)
            {
                foreach (HandPoseScriptableObject pose in PosesToDetect)
                {
                    if (CompareHands(pose, activePlayerHand) == false)
                    {
                        PoseHasNotBeenDetected.Invoke();
                    }
                    else
                    {
                        Debug.Log(pose.name);
                        PoseHasBeenDetected.Invoke(pose);
                    }
                }
                foreach (var item in handsToDetect)
                {
                    var capsuleHand = (CapsuleHand)item;
                    if (capsuleHand != null && capsuleHand != null)
                    {
                        if (capsuleHand.GetLeapHand().Id == activePlayerHand.Id)
                        {
                            capsuleHand.SetIndividualSphereColors = true;
                            capsuleHand.SphereColors = capsuleHandColours;

                        }
                    }
                }
            }

        }

        Color[] capsuleHandColours = null;

        private bool CompareHands(HandPoseScriptableObject pose, Hand activePlayerHand)
        {
            Hand serializedHand = pose.GetSerializedHand();
            Hand playerHand = activePlayerHand;
            int numMatchedFingers = 0;

            if (serializedHand == null || playerHand == null)
            {
                return false;
            }

            var colourCapsuleHand = handsToDetect.FirstOrDefault();
            if (colourCapsuleHand != null)
            {
                if (capsuleHandColours == null)
                {
                    capsuleHandColours = colourCapsuleHand.SphereColors;
                }
            }

            foreach (int fingerNum in pose.GetFingerIndexesToCheck())
            {
                int numMatchedBones = 0;
                Quaternion lastBoneRotation = playerHand.Rotation;
                Quaternion lastSerializedBoneRotation = serializedHand.Rotation;
                // Each bone in the finger 
                for (int i = 0; i < serializedHand.Fingers[fingerNum].bones.Length; i++)
                {
                    // Get the same bone for both comparison hand and player hand
                    Bone activeHandBone = playerHand.Fingers[fingerNum].bones[i];
                    Bone serializedHandBone = serializedHand.Fingers[fingerNum].bones[i];


                    // Get the user defined rotation threshold for the current bone (threshold is defined in the pose scriptable object)
                    float fingerRotationThreshold = GetBoneRotationThreshold(pose, fingerNum, i);

                    Quaternion activeBoneRotation = activeHandBone.Rotation;
                    Quaternion serializedBoneRotation = serializedHandBone.Rotation;

                    Vector3 activeRotEuler = (Quaternion.Inverse(lastBoneRotation) * activeBoneRotation).eulerAngles;
                    Vector3 serializedRotEuler = (Quaternion.Inverse(lastSerializedBoneRotation) * serializedBoneRotation).eulerAngles;

                    Vector3 eulerDifference = GetEulerAngleDifference(serializedRotEuler, activeRotEuler);
                    float boneDifference = GetDegreeAngleDifference(serializedRotEuler, activeRotEuler);

                    lastBoneRotation = activeBoneRotation;
                    lastSerializedBoneRotation = serializedBoneRotation;

                    if (capsuleHandColours != null)
                    {
                        capsuleHandColours[fingerNum * 4 + i] = Color.Lerp(Color.green, Color.red, boneDifference / fingerRotationThreshold);
                    }

                    Debug.Log("Finger: " + serializedHand.Fingers[fingerNum].ToString() 
                        + " bone: " + serializedHand.Fingers[fingerNum].bones[i].Type 
                        + " rotationDifference: " + boneDifference);

                    // check is the angle difference is lower than the threshold
                    if (boneDifference <= fingerRotationThreshold && boneDifference >= -fingerRotationThreshold)
                    {
                        numMatchedBones++;
                    }
                }

                if(numMatchedBones >= perFingerBoneMatches)
                {
                    ++numMatchedFingers;
                }
            }



            if (numMatchedFingers >= pose.GetFingerIndexesToCheck().Count)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        Vector3 GetEulerAngleDifference(Vector3 a, Vector3 b)
        {
            return new Vector3(Mathf.DeltaAngle(a.x, b.x), Mathf.DeltaAngle(a.y, b.y), Mathf.DeltaAngle(a.z, b.z));
        }

        float GetDegreeAngleDifference(Vector3 a, Vector3 b)
        {
            var averageAngle = (Mathf.DeltaAngle(a.x, b.x) + Mathf.DeltaAngle(a.y, b.y)/* + Mathf.DeltaAngle(a.z, b.z)*/)/2;
            return averageAngle;
        }

        private float GetBoneRotationThreshold(HandPoseScriptableObject pose, int fingerNum, int boneNum)
        {
            return pose.GetBoneRotationthreshold(fingerNum, boneNum);
        }

        /// <summary>
        /// Clear list of hands to detect. This will update on the next frame to include all hands in the scene.
        /// </summary>
        public void ClearCurrentHandsToDetect()
        {
            handsToDetect.Clear();
        }
    }
}
