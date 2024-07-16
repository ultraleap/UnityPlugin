/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using TMPro;
using UnityEngine;

namespace Leap.Examples
{
    public class PoseDetectorUIText : MonoBehaviour
    {
        public GameObject textGameobject;
        public TextMeshProUGUI text;

        [Header("References")]
        public HandPoseDetector detector;

        void Update()
        {
            HandPoseScriptableObject detectedPose = detector.GetCurrentlyDetectedPose();
            if (detectedPose != null)
            {
                textGameobject.SetActive(true);
                text.text = "Detected pose: " + detectedPose.name;
            }
            else
            {
                textGameobject.SetActive(false);
                text.text = "No pose detected";
            }
        }
    }
}