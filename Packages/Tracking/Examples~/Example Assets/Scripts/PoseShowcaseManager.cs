using Leap.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class PoseShowcaseManager : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI detectionText;

    [Serializable]
    struct ShowCasePose
    {
        public string poseName;
        public HandPoseViewer poseViewer;
        public Light spotlight;
    }
    [SerializeField]
    List<ShowCasePose> poseList = new List<ShowCasePose>();


    List<string> detectedPosesNames = new List<string>(); 

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
        UpdateDetectionText(inputString);
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

    private void UpdateDetectionText(string inputString, bool add = true)
    {
        if(add)
        {
            detectedPosesNames.Add(inputString);
        }
        else
        {
            detectedPosesNames.Remove(inputString);
        }

        if (detectedPosesNames.Count != 0)
        {
            detectionText.text = detectedPosesNames.Last();
        }
        else
        {
            detectionText.text = "No pose detected";
        }
    }

    public void PoseLost(string inputString)
    {
        TurnOffGreenLight(inputString);
        UpdateDetectionText(inputString, false);
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
