using Leap;
using Leap.Unity;
using Leap.Unity.Interaction;

using UnityEngine;

public class HandPoseSnapper : MonoBehaviour
{
    public HandPoseSnapScriptableObject[] possibleHandPoseSnaps;
    public HandPoseSnapScriptableObject currentPoseSnap;

    public LeapProvider handDataProvider;
    public HandPoseSnapPostProcessProvider handPoseSnapPostProcessProvider;

    public bool snapping = false;

    private void Update()
    {
        if (snapping)
            SnapToHand();
    }

    public void SnapToHand()
    {
        // TODO: make this work well with physics updates from Contact Hands.
        // e.g. Physics.SyncTransforms() causes object to ping when released, but if it is not used
        //the object is re-transformed when released, so jumps weirdly

        Hand leapHand = handDataProvider?.CurrentFrame?.GetHand(currentPoseSnap.chirality);

        if (leapHand == null)
            return;

        // Position the object on top of the parent
        transform.position = leapHand.PalmPosition;
        // Set the rotation based on the parent and stored offset rotation
        transform.rotation = leapHand.Rotation * currentPoseSnap.poseToObjectOffset.rotation;
        // Move the child back to the reference location
        transform.Translate(currentPoseSnap.poseToObjectOffset.position);
    }

    public void BeginInteractionHandSnap(InteractionController interactionController)
    {
        BeginSnap(((InteractionHand)interactionController).handDataMode == HandDataMode.PlayerLeft ? Chirality.Left : Chirality.Right);
    }

    public void BeginSnap(Chirality handToSnapTo)
    {
        SetNearestPoseSnapToCurrent(handToSnapTo);

        if (currentPoseSnap.chirality == Chirality.Left)
            handPoseSnapPostProcessProvider.currentHandPoseLeft = currentPoseSnap.handPose;
        else
            handPoseSnapPostProcessProvider.currentHandPoseRight = currentPoseSnap.handPose;

        snapping = true;
    }

    public void EndSnap()
    {
        if (currentPoseSnap.chirality == Chirality.Left)
            handPoseSnapPostProcessProvider.currentHandPoseLeft = null;
        else
            handPoseSnapPostProcessProvider.currentHandPoseRight = null;

        snapping = false;
    }

    public void SnapOnce(Chirality handToSnapTo)
    {
        BeginSnap(handToSnapTo);
        SnapToHand();
        EndSnap();
    }

    void SetNearestPoseSnapToCurrent(Chirality handToUse)
    {
        var positionOffset = transform.InverseTransformPoint(transform.position) - transform.InverseTransformPoint(handDataProvider.GetHand(handToUse).GetPalmPose().position);

        // TODO: Consider using rotation to choose a "closer" hand
        //var rotationOffset = Quaternion.Inverse(handDataProvider.GetHand(handToUse).GetPalmPose().rotation) * transform.rotation;
        // Perhaps try adding thr ability to move the original offet that is stored in GrabHelperObject.cs

        int nearestIndex = -1;
        float nearestDist = float.MaxValue;

        for (int i = 0; i < possibleHandPoseSnaps.Length; i++)
        {
            if (possibleHandPoseSnaps[i].chirality != handToUse)
                continue;

            float dist = Vector3.Distance(positionOffset, possibleHandPoseSnaps[i].poseToObjectOffset.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearestIndex = i;
            }
        }

        currentPoseSnap = possibleHandPoseSnaps[nearestIndex];
    }
}