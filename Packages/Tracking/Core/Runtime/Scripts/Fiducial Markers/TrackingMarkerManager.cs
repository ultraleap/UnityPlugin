using Leap.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackingMarkerManager : MonoBehaviour
{
    public LeapServiceProvider leapServiceProvider;

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
        Debug.Log(poseEvent.id);
    }
}