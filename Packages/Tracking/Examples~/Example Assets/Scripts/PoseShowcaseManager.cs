using Leap.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

public class PoseShowcaseManager : MonoBehaviour
{
    [SerializeField]
    List<HandPoseViewer> poseViewers = new List<HandPoseViewer>();
    
    struct ShowCasePose
    {
        public HandPoseViewer poseViewer;
        [HideInInspector]
        public Light spotlight;
        [HideInInspector]
        public HandPoseDetector detector;
    }
    List<ShowCasePose> poseList = new List<ShowCasePose>();

    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < poseViewers.Count; i++)
        {
            ShowCasePose showCasePose = new ShowCasePose();
            showCasePose.poseViewer = poseViewers[i];

            if(showCasePose.detector == null)
            {
                showCasePose.detector = showCasePose.poseViewer.GetComponentInChildren<HandPoseDetector>();
            }
            if(showCasePose.spotlight == null)
            {
                showCasePose.spotlight = showCasePose.poseViewer.GetComponentInChildren<Light>();
            }
            if (showCasePose.detector.GetPosesToDetect().Count <= 0)
            {
                showCasePose.detector.AddPoseToDetect(showCasePose.poseViewer.handPose);
            }
            poseList.Add(showCasePose);
        }
    }

    // Update is called once per frame
    void Update()
    {
        foreach (var pose in poseList)
        {
            if(pose.detector.GetCurrentlyDetectedPose() != null)
            {
                if (pose.spotlight.color != Color.green)
                {
                    pose.spotlight.color = Color.green;
                }
            }
            else
            {
                if (pose.spotlight.color != Color.white)
                {
                    pose.spotlight.color = Color.white;
                }
            }
        }
    }

    void PoseDetected()
    {

    }

}
