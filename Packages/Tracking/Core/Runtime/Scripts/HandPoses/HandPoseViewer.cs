using System;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity
{
    [ExecuteInEditMode]
    public class HandPoseViewer : LeapProvider
    {
        [HideInInspector]
        public override Frame CurrentFrame
        {
            get
            {
                List<Hand> hands = new List<Hand>();
                foreach (var hand in currentHandsAndPosedObjects)
                {
                    hands.Add(hand.Item1);
                }

                return new Frame(0, (long)Time.realtimeSinceStartup, 90, hands);
            }
        }

        [HideInInspector]
        public override Frame CurrentFixedFrame => new Frame();

        /// <summary>
        /// Pose to use
        /// </summary>
        public HandPoseScriptableObject handPose;

        public void SetHandPose(HandPoseScriptableObject poseToSet)
        {
            handPose = poseToSet;
        }

        [SerializeField]
        public Transform handLocation = null;

        private List<Tuple<Hand, HandPoseScriptableObject>> currentHandsAndPosedObjects = new List<Tuple<Hand, HandPoseScriptableObject>>();

        private void Update()
        {
            UpdateHands();
        }

        private void UpdateHands()
        {
            currentHandsAndPosedObjects.Clear();

            if (handPose == null)
            {
                return;
            }

            Hand posedHand = new Hand();
            Hand mirroredHand = new Hand();

            posedHand.CopyFrom(handPose.GetSerializedHand());
            mirroredHand.CopyFrom(handPose.GetMirroredHand());

            Vector3 handPosition = transform.position;

            if (handLocation != null)
            {
                handPosition = handLocation.position;
            }

            posedHand.SetTransform(handPosition, handLocation.rotation);
            mirroredHand.SetTransform(handPosition, handLocation.rotation);

            currentHandsAndPosedObjects.Add(new Tuple<Hand, HandPoseScriptableObject>(posedHand, handPose));
            currentHandsAndPosedObjects.Add(new Tuple<Hand, HandPoseScriptableObject>(mirroredHand, handPose));

            DispatchUpdateFrameEvent(CurrentFrame);
        }
    }
}