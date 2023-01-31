using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Leap.Unity
{
    public class HandPoseDetector : MonoBehaviour
    {
        [SerializeField]
        List<HandPoseScriptableObject> PosesToDetect;

        [SerializeField]
        HandModelBase activePlayerHand;

        [SerializeField]
        bool checkThumb, checkIndex, checkMiddle, checkRing, checkPinkie;

        [SerializeField]
        float fingerRotationThreshold = 15f;

        List<int> fingerIndexesToCheck = new List<int>();

        [SerializeField]
        bool allEnabledBonesMatched = true;

        [SerializeField]
        int perFingerBoneMatches=3;

        [SerializeField]
        float debugAngle = 0;

        public static event Action<HandPoseScriptableObject> PoseHasBeenDetected;
        public static event Action PoseHasNotBeenDetected;


        // Start is called before the first frame update
        void Start()
        {
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
            

            foreach (HandPoseScriptableObject pose in PosesToDetect)
            {
                allEnabledBonesMatched = true;
                if (CompareFinger(pose.GetSerializedHand()) == false)
                {
                    allEnabledBonesMatched = false;
                }

                if (allEnabledBonesMatched)
                {
                    Debug.Log(pose.name);
                    PoseHasBeenDetected.Invoke(pose);
                }
                else
                {
                    PoseHasNotBeenDetected.Invoke();
                }
            }
        }

        private bool CompareFinger(Hand hand)
        {
            int numMatchedBones = 0;
            foreach (int fingerNum in fingerIndexesToCheck)
            {
                foreach (Bone bone in hand.Fingers[fingerNum].bones)
                {
                    if (activePlayerHand.GetLeapHand() != null)
                    {
                        Bone comparisonBone = activePlayerHand.GetLeapHand().Fingers[fingerNum].Bone(bone.Type);

                        if (Quaternion.Angle(bone.Rotation, comparisonBone.Rotation) < fingerRotationThreshold)
                        {
                            numMatchedBones++;
                        }
                    }
                }
            }


            if (numMatchedBones >= perFingerBoneMatches)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
