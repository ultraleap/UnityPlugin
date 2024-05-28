using Leap;
using Leap.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandPoseSnapPostProcessProvider : PostProcessProvider
{
    public HandPoseScriptableObject currentHandPoseLeft = null;
    public HandPoseScriptableObject currentHandPoseRight = null;

    public void BeginPoseSnap(HandPoseScriptableObject handPose, Chirality chirality)
    {
        if(chirality == Chirality.Left)
            currentHandPoseLeft = handPose;
        else
            currentHandPoseRight = handPose;
    }

    public void StopPoseSnap(HandPoseScriptableObject handPose, Chirality chirality)
    {
        if (chirality == Chirality.Left && currentHandPoseLeft == handPose)
            currentHandPoseLeft = null;
        else if (chirality == Chirality.Right && currentHandPoseRight == handPose)
            currentHandPoseRight = null;
    }

    public override void ProcessFrame(ref Frame inputFrame)
    {
        foreach (var hand in inputFrame.Hands)
        {
            if (hand.IsLeft)
            {
                HandleSnap(hand, currentHandPoseLeft);
            }
            else
            {
                HandleSnap(hand, currentHandPoseRight);
            }
        }
    }

    void HandleSnap(Hand hand, HandPoseScriptableObject handPose)
    {
        if (handPose != null)
        {
            // Find the hand that should be used for this snap via its cirality
            Hand posedHand = new Hand();
            posedHand = posedHand.CopyFrom(handPose.GetSerializedHand());

            if (posedHand.IsLeft != hand.IsLeft)
            {
                posedHand = posedHand.CopyFrom(handPose.GetMirroredHand());
            }

            // Cache tracked hand information for later use
            Pose trackedHandPose = new Pose(hand.PalmPosition, hand.Rotation);

            // Move the trakced hand to match the posed hand to make swapping fingers easier
            hand.SetTransform(posedHand.PalmPosition, posedHand.Rotation);

            // Mix and match between fingers of each hand depending on if they are used for the pose
            List<int> fingerIndexesUsedInPose = handPose.GetFingerIndexesToCheck();
            List<Finger> fingers = hand.Fingers;

            foreach (var fingerIndex in fingerIndexesUsedInPose)
            {
                fingers[fingerIndex] = posedHand.Fingers[fingerIndex];
            }

            // Combine the tracked hand with the posed hand
            hand.Fill(hand.FrameId,
                hand.Id,
                hand.Confidence,
                posedHand.GrabStrength,
                posedHand.PinchStrength,
                posedHand.PinchDistance,
                hand.PalmWidth,
                hand.IsLeft,
                hand.TimeVisible,
                fingers,
                hand.PalmPosition,
                hand.StabilizedPalmPosition,
                hand.PalmVelocity,
                hand.PalmNormal,
                hand.Rotation,
                hand.Direction,
                hand.WristPosition);

            // Move the hand to its original position and orientation
            hand.SetTransform(trackedHandPose.position, trackedHandPose.rotation);
        }
    }
}