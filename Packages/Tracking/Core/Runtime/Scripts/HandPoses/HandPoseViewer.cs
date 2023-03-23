using Leap.Unity;
using Leap;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
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
        /// 
        [HideInInspector]
        public HandPoseScriptableObject handPose;

        /// <summary>
        /// Selected hand pose for viewing
        /// </summary>
        [HideInInspector]
        public int Selected = 0;
        /// <summary>
        /// List for pose scriptable objects in the editor
        /// </summary>
        [HideInInspector]
        public Dictionary<int, string> PoseScritableIntName = new Dictionary<int, string>();

        public void SetHandPose(HandPoseScriptableObject poseToSet)
        {
            handPose = poseToSet;
        }

        [SerializeField]
        public Transform handLocation = null;
        [SerializeField]
        public Transform unusedHandLocation = null;

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


            Vector3 handPosition = this.transform.position;
            Vector3 unusedHandPosition = Camera.main.transform.position + (Camera.main.transform.forward * 0.5f);

            if (handLocation != null)
            {
                handPosition = handLocation.position;
            }
            if (unusedHandLocation != null)
            {
                unusedHandPosition = unusedHandLocation.position;
            }

            if (posedHand.IsLeft)
            {
                posedHand.SetTransform(handPosition, (posedHand.Rotation * handLocation.rotation));
                mirroredHand.SetTransform(unusedHandPosition, (posedHand.Rotation * handLocation.rotation));
            }
            else
            {
                posedHand.SetTransform(handPosition, (posedHand.Rotation * handLocation.rotation));
                mirroredHand.SetTransform(unusedHandPosition, (posedHand.Rotation * handLocation.rotation));
            }

            currentHandsAndPosedObjects.Add(new Tuple<Hand, HandPoseScriptableObject>(posedHand, handPose));
            currentHandsAndPosedObjects.Add(new Tuple<Hand, HandPoseScriptableObject>(mirroredHand, handPose));

            DispatchUpdateFrameEvent(CurrentFrame);
        }
    }
}
