using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

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
        List<HandModelBase> handsToDetect = new();

        /// <summary>
        /// Which fingers should be checked when doing pose detection?
        /// </summary>
        [SerializeField]
        bool checkThumb, checkIndex, checkMiddle, checkRing, checkPinkie;

        /// <summary>
        /// How many bones need to match per finger?
        /// Note by default, a hand has 4 bones.
        /// </summary>
        [SerializeField]
        int perFingerBoneMatches = 3;

        

        List<int> fingerIndexesToCheck = new();

        public static event Action<HandPoseScriptableObject> PoseHasBeenDetected;
        public static event Action PoseHasNotBeenDetected;

        // Start is called before the first frame update
        void Start()
        {
            fingerIndexesToCheck.Clear();
            if (checkThumb) { fingerIndexesToCheck.Add(0); }
            if (checkIndex) { fingerIndexesToCheck.Add(1); }
            if (checkMiddle) { fingerIndexesToCheck.Add(2); }
            if (checkRing) { fingerIndexesToCheck.Add(3); }
            if (checkPinkie) { fingerIndexesToCheck.Add(4); }
        }

        // When the Inspector is changed
        private void OnValidate()
        {
            fingerIndexesToCheck.Clear();
            if (checkThumb) { fingerIndexesToCheck.Add(0); }
            if (checkIndex) { fingerIndexesToCheck.Add(1); }
            if (checkMiddle) { fingerIndexesToCheck.Add(2); }
            if (checkRing) { fingerIndexesToCheck.Add(3); }
            if (checkPinkie) { fingerIndexesToCheck.Add(4); }
        }

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
                handsToDetect = GameObject.FindObjectsOfType<HandModelBase>().ToList();
                if(handsToDetect.Count <= 0) 
                {
                    Debug.Log("Skipping pose detection, there are no Leap hands in the scene");
                    return;
                }
            }

            foreach (var activePlayerHand in handsToDetect)
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
            }
        }

        private bool CompareHands(HandPoseScriptableObject pose, HandModelBase activePlayerHand)
        {
            Hand serializedHand = pose.GetSerializedHand();
            Hand playerHand = activePlayerHand.GetLeapHand();
            int numMatchedBones = 0;
            int numMatchedFingers = 0;
            float fingerRotationThreshold = 0;

            // Edoes the anach finger
            foreach (int fingerNum in fingerIndexesToCheck)
            {
                // Each bone in the finger 
                for (int i = 0; i < serializedHand.Fingers[fingerNum].bones.Length; i++)
                {
                    // Get the same bone for both comparison hand and player hand
                    Bone serializedPoseBone = serializedHand.Fingers[fingerNum].bones[i];
                    Bone activeHandBone = playerHand.Fingers[fingerNum].Bone(serializedPoseBone.Type);
                    
                    // Get the user defined rotation threshold for the current bone (threshold is defined in the pose scriptable object)
                    fingerRotationThreshold = GetBoneRotationThreshold(pose, fingerNum, (int)serializedPoseBone.Type);

                    // Get the rotation of the current bone by comparing the top and bottom joint positions of the bone.
                    var activeHandBoneRotation = Math.Abs(Vector3.Angle(activeHandBone.PrevJoint, activeHandBone.NextJoint));
                    var serializedPoseBoneRotation = Math.Abs(Vector3.Angle(serializedPoseBone.PrevJoint, serializedPoseBone.NextJoint));

                    // Get the difference in angle between the pose rotation and our hand rotation
                    var angleDifference = Math.Floor(Math.Abs(activeHandBoneRotation - serializedPoseBoneRotation) * 360);

                    // check is the angle difference is lower than the threshold
                    if (angleDifference <= fingerRotationThreshold)
                    {
                        numMatchedBones++;
                    }
                }

                if(numMatchedBones >= perFingerBoneMatches)
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

        private float GetBoneRotationThreshold(HandPoseScriptableObject pose, int fingerNum, int boneNum)
        {
            return pose.GetBoneRotation(fingerNum, boneNum);
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
