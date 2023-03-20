using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TMPro;
using Leap.Unity;

namespace Leap.Unity.Examples
{
    public class PoseRecorderExampleManager : MonoBehaviour
    {
        [Header("Count Down")]
        public GameObject countDownGO;
        public TextMeshProUGUI countDownText;
        public float recordCountDown = 3;

        [Header("References")]
        public HandPoseRecorder recorder;
        public HandPoseEditor editor;
        public HandPoseDetector detector;
        public HandPoseValidator validator;

        private List<HandPoseScriptableObject> posesRecordedThisSession = new List<HandPoseScriptableObject>();

        bool capturing = false;

        private void Awake()
        {
            recorder.OnPoseSaved += OnPoseSaved;
        }

        void Update()
        {
            if (!capturing)
            {
                HandPoseScriptableObject detectedPose = detector.GetCurrentlyDetectedPose();
                if (detectedPose != null)
                {
                    countDownGO.SetActive(true);
                    countDownText.text = "Detected pose: " + detectedPose.name;
                }
                else
                {
                    countDownGO.SetActive(false);
                    countDownText.text = "No pose detected";
                }
            }
        }

        public void OnPoseSaved(HandPoseScriptableObject pose)
        {
            editor.handPose = pose;
            editor.gameObject.SetActive(true);

            editor.transform.position = Camera.main.transform.position;
            editor.transform.rotation = Camera.main.transform.rotation;
        }

        public void BeginRecording()
        {
            if (!capturing)
            {
                capturing = true;
                StartCoroutine(RecordAfterCountDown());
            }
        }

        IEnumerator RecordAfterCountDown()
        {
            countDownGO.SetActive(true);
            float timeLeft = recordCountDown;

            while (timeLeft > 0)
            {
                timeLeft -= Time.deltaTime;
                countDownText.text = Mathf.CeilToInt(timeLeft).ToString();
                yield return null;
            }

            countDownGO.SetActive(false);
            HandPoseScriptableObject savedPose = recorder.SaveCurrentHandPose();
            posesRecordedThisSession.Add(savedPose);
            capturing = false;
            detector.SetPosesToDetect(posesRecordedThisSession); 
        }
    }
}