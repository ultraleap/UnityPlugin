using Leap;
using Leap.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackingMarkerManager : MonoBehaviour
{
    public LeapServiceProvider leapServiceProvider;

    public Transform trackingMarker;

    private void Start()
    {
        if(leapServiceProvider == null)
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

    private void OnFiducialMarkerPose(object sender, Leap.FiducialPoseEventArgs poseEvent)
    {
        //Debug.Log("" + poseEvent.translation.ToVector3().ToString());

        // Get the device position in world space as a LeapTransform
        LeapTransform trackerPosWorldSpace = leapServiceProvider.DeviceOriginWorldSpace;

        // Convert the LeapMatrix3x3 into something we can access the rotation from
        Matrix4x4 rotationMatrix = new Matrix4x4(new Vector4(poseEvent.rotation.m1.x, poseEvent.rotation.m2.x, poseEvent.rotation.m3.x, 0),
                                                 new Vector4(poseEvent.rotation.m1.y, poseEvent.rotation.m2.y, poseEvent.rotation.m3.y, 0),
                                                 new Vector4(poseEvent.rotation.m1.z, poseEvent.rotation.m2.z, poseEvent.rotation.m3.z, 0),
                                                 new Vector4(0, 0, 0, 1));

        // Access the translation and rotation of the marker in Leap coordinates
        Vector3 unityTranslation = poseEvent.translation.ToVector3();
        Quaternion unityRotation = rotationMatrix.rotation;

        // Generate a LeapTransform that has the same z mirror and unit conversion from CopyFromLeapCExtensions.TransformToUnityUnits()
        var MM_TO_M = 1e-3f;
        LeapTransform leapTransform = new LeapTransform(Vector3.zero, Quaternion.identity, new Vector3(MM_TO_M, MM_TO_M, MM_TO_M));
        leapTransform.MirrorZ();

        // Apply the TransformToUnityUnits Transform and then apply the world space device LeapTransform
        unityTranslation = leapTransform.TransformPoint(unityTranslation);

        Debug.Log(unityTranslation.ToString("F5"));

        trackingMarker.position = trackerPosWorldSpace.TransformPoint(unityTranslation);

        // Apply the TransformToUnityUnits Transform and then apply the world space device LeapTransform
        unityRotation = leapTransform.TransformQuaternion(unityRotation);
        trackingMarker.rotation = trackerPosWorldSpace.TransformQuaternion(unityRotation);
    }
}