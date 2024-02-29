using Leap;
using Leap.Unity;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackingMarkerManager : MonoBehaviour
{
    public LeapServiceProvider leapServiceProvider;

    [Tooltip("The virtual object that the markers are associated with")]
    public Transform trackedObject;

    [Tooltip("A collections of markers positioned relative to the Tracked Object")]
    public TrackingMarker[] markers;

    const float MM_TO_M = 1e-3f;
    LeapTransform leapToUnityTransform;
    LeapTransform trackerPosWorldSpace;

    Vector3 targetPos;
    Quaternion targetRot;

    private void Start()
    {
        // Produce a reusable LeapTransform to convert from Leap coordinate space and units to Unity
        leapToUnityTransform = new LeapTransform(Vector3.zero, Quaternion.identity, new Vector3(MM_TO_M, MM_TO_M, MM_TO_M));
        leapToUnityTransform.MirrorZ();

        // Ensure the LeapServiceProvider exists for us to position the device in world space
        if (leapServiceProvider == null)
        {
            leapServiceProvider = FindObjectOfType<LeapServiceProvider>();
        }

        // Listen to events for the new marker poses
        if (leapServiceProvider != null)
        {
            leapServiceProvider.GetLeapController().FiducialPose -= OnFiducialMarkerPose;
            leapServiceProvider.GetLeapController().FiducialPose += OnFiducialMarkerPose;
        }
        else
        {
            Debug.Log("Unable to begin Fiducial Marker tracking. Cannot connect to a Leap Service Provider.");
        }

        targetPos = trackedObject.position;
        targetRot = trackedObject.rotation;
    }

    private void OnFiducialMarkerPose(object sender, FiducialPoseEventArgs poseEvent)
    {
        // We cannot place the marker properly while the ServiceProvider does not exist
        if (leapServiceProvider == null || leapServiceProvider.enabled == false)
        {
            return;
        }

        // Find a matching TrackingMarker to the one from the event by matching ids
        TrackingMarker markerObject = null;

        for(int i = 0; i < markers.Length; i++)
        {
            if (markers[i] != null && markers[i].id == poseEvent.id)
            {
                markerObject = markers[i];
                break;
            }
        }

        if(markerObject == null)
        {
            // We don't have this marker
            return;
        }

        // Get the device position in world space as a LeapTransform to use in future calculations
        trackerPosWorldSpace = leapServiceProvider.DeviceOriginWorldSpace;

        // Convert the LeapMatrix3x3 into something we can access the rotation from
        Matrix4x4 rotationMatrix = poseEvent.rotation.ToUnityRotationMatrix();

        // Convert the Leap Pose to worldspace Unity units
        Vector3 markerPos = GetMarkerWorldSpacePosition(poseEvent.translation.ToVector3());
        Quaternion markerRot = GetMarkerWorldSpaceRotation(rotationMatrix.rotation);

        // Find the offset from the marker to the tracked object, to apply the inverse to the tracked transform
        Vector3 posOffset = trackedObject.position - markerObject.transform.position;
        Quaternion rotOffset = Quaternion.Inverse(trackedObject.rotation) * markerObject.transform.rotation;

        targetPos = markerPos + posOffset;
        targetRot = markerRot * Quaternion.Inverse(rotOffset);
    }

    private void Update()
    {
        trackedObject.position = targetPos;
        trackedObject.rotation = targetRot;
    }

    #region Utilities

    Vector3 GetMarkerWorldSpacePosition(Vector3 trackedMarkerPosition)
    {
        // Apply the leapToUnityTransform Transform and then apply the trackerPosWorldSpace Transform
        trackedMarkerPosition = leapToUnityTransform.TransformPoint(trackedMarkerPosition);
        trackedMarkerPosition = trackerPosWorldSpace.TransformPoint(trackedMarkerPosition);

        return trackedMarkerPosition;
    }

    Quaternion GetMarkerWorldSpaceRotation(Quaternion trackedMarkerRotation)
    {
        // Apply the leapToUnityTransform Transform and then apply the trackerPosWorldSpace Transform
        trackedMarkerRotation = leapToUnityTransform.TransformQuaternion(trackedMarkerRotation);
        trackedMarkerRotation = trackerPosWorldSpace.TransformQuaternion(trackedMarkerRotation);

        return trackedMarkerRotation;
    }

    #endregion
}