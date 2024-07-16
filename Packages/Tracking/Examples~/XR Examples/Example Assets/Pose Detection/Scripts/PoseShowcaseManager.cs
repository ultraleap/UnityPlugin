/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Leap.Examples
{
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

        private void Start()
        {
            for (int i = 0; i < poseList.Count; i++)
            {
                var pose = poseList[i];

                if (pose.spotlight == null)
                {
                    pose.spotlight = pose.poseViewer.GetComponentInChildren<Light>();
                }

                if (pose.poseName == null)
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
            detectionText.text = inputString;
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

            detectionText.text = "No pose detected";
        }
    }
}