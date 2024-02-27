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

    private void Start()
    {
        SetMarkerOffsets();

        leapToUnityTransform = new LeapTransform(Vector3.zero, Quaternion.identity, new Vector3(MM_TO_M, MM_TO_M, MM_TO_M));
        leapToUnityTransform.MirrorZ();

        if (leapServiceProvider == null)
        {
            leapServiceProvider = FindObjectOfType<LeapServiceProvider>();
        }

        if(leapServiceProvider != null)
        {
            leapServiceProvider.GetLeapController().FiducialPose -= OnFiducialMarkerPose;
            leapServiceProvider.GetLeapController().FiducialPose += OnFiducialMarkerPose;
        }
        else
        {
            Debug.Log("Unable to begin Fiducial Marker tracking. Cannot connect to a Leap Service Provider.");
        }
    }

    /// <summary>
    /// Set the offset values for each marker to be re-applied when positioning the parent objects relative to the tracked markers at runtime
    /// </summary>
    void SetMarkerOffsets()
    {
        foreach (var marker in markers)
        {
            marker.poitionOffset = marker.transform.position - trackedObject.position;
            marker.rotationOffset = Quaternion.Inverse(trackedObject.rotation) * marker.transform.rotation;
        }
    }

    private void OnFiducialMarkerPose(object sender, FiducialPoseEventArgs poseEvent)
    {
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
            // We don't have this marker as a child
            return;
        }

        Vector3 markerPos;
        Quaternion markerRot;

        // Get the device position in world space as a LeapTransform
        trackerPosWorldSpace = leapServiceProvider.DeviceOriginWorldSpace;

        //

        // Access the translation and rotation of the marker in Leap coordinates
        Vector3 unityTranslation = poseEvent.translation.ToVector3();

        // Apply the leapToUnityTransform Transform and then apply the trackerPosWorldSpace Transform
        unityTranslation = leapToUnityTransform.TransformPoint(unityTranslation);
        markerPos = trackerPosWorldSpace.TransformPoint(unityTranslation);

        //

        // Convert the LeapMatrix3x3 into something we can access the rotation from
        Matrix4x4 rotationMatrix = poseEvent.rotation.ToUnityRotationMatrix();
        Quaternion unityRotation = rotationMatrix.rotation;

        // Apply the leapToUnityTransform Transform and then apply the trackerPosWorldSpace Transform
        unityRotation = leapToUnityTransform.TransformQuaternion(unityRotation);
        markerRot = trackerPosWorldSpace.TransformQuaternion(unityRotation);

        //////////
        // Place trackedObjectParent relative to the tracked marker position
        // Apply the target position and rotation to the parent object
        trackedObject.position = markerPos - markerRot * markerObject.poitionOffset;
        trackedObject.rotation = markerRot * Quaternion.Inverse(markerObject.rotationOffset);
    }
}