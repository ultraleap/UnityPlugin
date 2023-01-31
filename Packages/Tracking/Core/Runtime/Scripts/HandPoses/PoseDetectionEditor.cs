using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Leap.Unity
{
    [ExecuteInEditMode]
    public class PoseDetectionEditor : LeapProvider
    {
        public override Frame CurrentFrame
        { get
            { 
                return new Frame(0, (long)Time.realtimeSinceStartup, 90, currentHands);
            }
        }
        public Frame TestFrame = null;

        public override Frame CurrentFixedFrame => new Frame();

        [SerializeField]
        HandPoseScriptableObject poseScriptableObject;

        List<Hand> currentHands = new List<Hand>();

        Hand PosedHand;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void OnValidate()
        {
            
            if (poseScriptableObject != null)
            {
                PosedHand = poseScriptableObject.GetSerializedHand();
                PosedHand.PalmPosition = new Vector3(0, 0, 0);
                //PosedHand.Rotation = Quaternion.identity;
                //PosedHand.


                //currentHand = poseScriptableObject.GetSerializedHand();
                currentHands.Clear();
                currentHands.Add(PosedHand);
            }
            TestFrame = CurrentFrame;
            DispatchUpdateFrameEvent(CurrentFrame);
        }


    }
}
