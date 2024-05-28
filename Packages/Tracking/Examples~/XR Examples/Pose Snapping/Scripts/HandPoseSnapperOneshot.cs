using Leap;
using Leap.Unity;
using Leap.Unity.PhysicalHands;
using LeapInternal;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandPoseSnapperOneshot : MonoBehaviour, IPhysicalHandGrab
{
    public HandPoseSnapScriptableObject[] possibleHandPoseSnaps;

    bool grabbedLeft = false;
    bool grabbedRight = false;

    public void HandleHandGrabbed(ContactHand contactHand)
    {
        Hand snappedHand = GetPoseSnapOneshot(contactHand.ModifiedHand);

        if (GrabHelper.Instance.TryGetGrabHelperObjectFromRigid(GetComponent<Rigidbody>(), out GrabHelperObject helper))
        {
            helper.RecalculateOffsets(snappedHand);
        }

        if (contactHand is HardContactHand)
        {
            ((HardContactHand)contactHand).UpdateHand(snappedHand);// .TeleportHand(snappedHand);
        }
    }

    public Hand GetPoseSnapOneshot(Hand inputHand)
    {
        var positionOffset = transform.InverseTransformPoint(transform.position) - transform.InverseTransformPoint(inputHand.GetPalmPose().position);

        // TODO: Consider using rotation to choose a "closer" hand
        //var rotationOffset = Quaternion.Inverse(handDataProvider.GetHand(handToUse).GetPalmPose().rotation) * transform.rotation;
        // Perhaps try adding thr ability to move the original offet that is stored in GrabHelperObject.cs

        int nearestIndex = -1;
        float nearestDist = float.MaxValue;

        for (int i = 0; i < possibleHandPoseSnaps.Length; i++)
        {
            if (possibleHandPoseSnaps[i].chirality != inputHand.GetChirality())
                continue;

            float dist = Vector3.Distance(positionOffset, possibleHandPoseSnaps[i].poseToObjectOffset.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearestIndex = i;
            }
        }

        if (nearestIndex == -1)
            return inputHand;

        var currentPoseSnap = possibleHandPoseSnaps[nearestIndex];

        HandleHandSnap(ref inputHand, currentPoseSnap.handPose);

        HandleObjectSnap(inputHand, currentPoseSnap);

        return inputHand;
    }

    void HandleObjectSnap(Hand inputHand, HandPoseSnapScriptableObject currentPoseSnap)
    {
        // Position the object on top of the parent
        transform.position = inputHand.PalmPosition;
        // Set the rotation based on the parent and stored offset rotation
        transform.rotation = inputHand.Rotation * currentPoseSnap.poseToObjectOffset.rotation;
        // Move the child back to the reference location
        transform.Translate(currentPoseSnap.poseToObjectOffset.position);

        var rb = GetComponent<Rigidbody>();

        rb.MovePosition(transform.position);
        rb.MoveRotation(transform.rotation);
    }

    void HandleHandSnap(ref Hand hand, HandPoseScriptableObject handPose)
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

    // Grab Events

    public void OnHandGrab(ContactHand hand)
    {
        if (hand.Handedness == Chirality.Left)
        {
            if (!grabbedLeft)
            {
                HandleHandGrabbed(hand);
                grabbedLeft = true;
            }
        }
        else
        {
            if (!grabbedRight)
            {
                HandleHandGrabbed(hand);
                grabbedRight = true;
            }
        }
    }

    public void OnHandGrabExit(ContactHand hand)
    {
        if (hand.Handedness == Chirality.Left)
        {
            grabbedLeft = false;
        }
        else
        {
            grabbedRight = false;
        }
    }
}
