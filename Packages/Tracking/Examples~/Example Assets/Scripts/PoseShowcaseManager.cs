using Leap.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.XR.OpenXR.Input;

public class PoseShowcaseManager : MonoBehaviour
{
    [Serializable]
    struct ShowCasePose
    {
        public HandPoseViewer poseViewer;
        public Light spotlight;
        public string poseName;
    }
    [SerializeField]
    List<ShowCasePose> poseList = new List<ShowCasePose>();

    private void Start()
    {
        for (int i = 0; i < poseList.Count; i++)
        {
            var pose = poseList[i];

            if(pose.spotlight == null)
            {
                pose.spotlight = pose.poseViewer.GetComponentInChildren<Light>();
            }

            if(pose.poseName == null)
            {
                pose.poseName = pose.poseViewer.name;
            }
        }
    }

    public void PoseDetected(string inputString)
    {
        TurnOnGreenLight(inputString);
    }

    private void TurnOnGreenLight(string inputString)
    {
        foreach (var pose in poseList)
        {
            if (string.Equals(inputString, pose.poseName, StringComparison.OrdinalIgnoreCase))
            {
                if (pose.spotlight.color != Color.green)
                {
                    pose.spotlight.color = Color.green;
                }
            }
        }

    }

    public void PoseLost(string inputString)
    {
        TurnOffGreenLight(inputString);
    }

    private void TurnOffGreenLight(string inputString)
    {
        foreach (var pose in poseList)
        {
            if (string.Equals(inputString, pose.poseName, StringComparison.OrdinalIgnoreCase))
            {
                if (pose.spotlight.color != Color.white)
                {
                    pose.spotlight.color = Color.white;
                }
            }
        }

    }

}
