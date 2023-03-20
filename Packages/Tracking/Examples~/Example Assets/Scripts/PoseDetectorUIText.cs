using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TMPro;
using Leap.Unity;

namespace Leap.Unity.Examples
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