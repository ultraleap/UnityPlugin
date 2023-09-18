using Leap;
using Leap.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Modifies a hand tracking data source using data from the PoseToIRImageConfidence
/// source
/// </summary>
public class ImageBasedConfidenceProvider : PostProcessProvider
{
    public PoseToIRImageConfidence imageBasedConfidence;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// Post-processes the input frame in place to give hands bouncy-feeling physics.
    /// </summary>
    public override void ProcessFrame(ref Frame inputFrame)
    {
        var leftHand = inputFrame.Hands.FirstOrDefault(h => h.IsLeft);
        var rightHand = inputFrame.Hands.FirstOrDefault(h => !h.IsLeft);

        if (leftHand != null && imageBasedConfidence != null)
        {
            UpdateHandFromConfidence(ref leftHand, imageBasedConfidence.TipConfidence.LeftConfidence);
        }

        if (rightHand != null && imageBasedConfidence != null)
        {
            UpdateHandFromConfidence(ref rightHand, imageBasedConfidence.TipConfidence.RightConfidence);
        }
    }

    private void UpdateHandFromConfidence(ref Hand inputHand, HandTipConfidences confidenceInfo)
    {
        if (confidenceInfo != null)
        {
            UpdateFinger(ref inputHand, (int)Finger.FingerType.TYPE_THUMB, confidenceInfo.ThumbTip);
            //UpdateFinger(ref inputHand, (int)Finger.FingerType.TYPE_INDEX, confidenceInfo.IndexTip);
            //UpdateFinger(ref inputHand, (int)Finger.FingerType.TYPE_PINKY, confidenceInfo.PinkyTip);
            //UpdateFinger(ref inputHand, (int)Finger.FingerType.TYPE_RING, confidenceInfo.RingTip);
            //UpdateFinger(ref inputHand, (int)Finger.FingerType.TYPE_MIDDLE, confidenceInfo.MiddleTip);
        }
    }

    private void UpdateFinger(ref Hand hand, int fingerIndex, (float confidence, Vector3 lastGoodPosition_LeapSpace_proximal, Vector3 lastGoodPosition_LeapSpace_intermediate, Vector3 lastGoodPosition_LeapSpace_distal, Vector3 lastGoodPosition_PixelSpace) fingerConfidence)
    {
        hand.Fingers[fingerIndex].Bone(Bone.BoneType.TYPE_DISTAL).NextJoint = fingerConfidence.lastGoodPosition_LeapSpace_distal;
        hand.Fingers[fingerIndex].Bone(Bone.BoneType.TYPE_INTERMEDIATE).NextJoint = fingerConfidence.lastGoodPosition_LeapSpace_intermediate;
        hand.Fingers[fingerIndex].Bone(Bone.BoneType.TYPE_PROXIMAL).NextJoint = fingerConfidence.lastGoodPosition_LeapSpace_proximal;
    }
}
