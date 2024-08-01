/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

#if UNITY_EDITOR

using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Events;

namespace Leap.Recording
{
    public class HandRecorder : MonoBehaviour
    {
        public enum ChiralityOptions
        {
            BOTH,
            LEFT,
            RIGHT,
            NONE
        }

        public enum HandSelection
        {
            AUTOMATIC,
            MANUAL
        }

        [HideInInspector]
        public bool recording = false;
        protected GameObjectRecorder m_Recorder;

        [Header("Hands")]
        public ChiralityOptions handsToRecord = ChiralityOptions.BOTH;
        public HandSelection handSelection = HandSelection.AUTOMATIC;

        public GameObject leftHandBoneRoot;
        public GameObject rightHandBoneRoot;

        [Space, Header("Objects")]
        public GameObject[] additionalObjects;

        [Space, Header("Animation")]
        public bool automaticallyGenerateAnimationClip = true;

        private AnimationClip _targetClip;
        public AnimationClip targetClip;

        [SerializeField]
        public bool _lossyCompression = true;

        [Range(5, 60)]
        public int captureFramerate = 15;

        [Space, Header("Recording")]
        public KeyCode toggleRecordingKey = KeyCode.None;

        public UnityEvent OnRecordingStart;
        public UnityEvent<AnimationClip> OnRecordingComplete;

        private float nextCaptureTime = 0;
        private string fullClipPath;

        private void OnDisable()
        {
            if (recording)
            {
                EndRecording();
            }
        }

        private void Update()
        {
            if (toggleRecordingKey != KeyCode.None && Input.GetKeyDown(toggleRecordingKey))
            {
                if (recording)
                    EndRecording();
                else
                    StartRecording();
            }
        }

        public virtual void StartRecording()
        {
            if (handSelection == HandSelection.AUTOMATIC)
            {
                AutoSelectHandRoots();
            }

            if(automaticallyGenerateAnimationClip)
            {
                MakeAnimaitonClipAsset();
            }
            else
            {
                _targetClip = targetClip;
            }

            if (_targetClip == null)
            {
                Debug.Log("No Target Clip selected. Generating a new animation clip.");
                MakeAnimaitonClipAsset();
            }

            m_Recorder = new GameObjectRecorder(gameObject);

            if (leftHandBoneRoot != null)
            {
                m_Recorder.BindComponentsOfType<Transform>(leftHandBoneRoot, true);
            }

            if (rightHandBoneRoot != null)
            {
                m_Recorder.BindComponentsOfType<Transform>(rightHandBoneRoot, true);
            }

            // Bind all the secondary objects too
            // This is currently recursive, we may want to change that in future
            foreach (GameObject otherObj in additionalObjects)
            {
                m_Recorder.BindComponentsOfType<Transform>(otherObj, true);
            }

            OnRecordingStart?.Invoke();

            recording = true;
            nextCaptureTime = Time.time + (1 / captureFramerate);
        }

        private void MakeAnimaitonClipAsset()
        {
            AnimationClip newItem = new AnimationClip(); ;

            if (!Directory.Exists("Assets/Hand Recordings/"))
            {
                Directory.CreateDirectory("Assets/Hand Recordings/");
            }

            string fullPath = "Assets/Hand Recordings/" + gameObject.name + ".asset";

            int fileIterator = 1;
            while (File.Exists(fullPath))
            {
                fullPath = "Assets/Hand Recordings/" + gameObject.name + " (" + fileIterator + ")" + ".asset";
                fileIterator++;
            }

            fullClipPath = fullPath;

            AssetDatabase.CreateAsset(newItem, fullPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            _targetClip = newItem;
        }

        private void LateUpdate()
        {
            if (_targetClip == null)
            {
                return;
            }

            if (recording && Time.time >= nextCaptureTime)
            {
                // Take a snapshot and record all the bindings values for this frame.
                m_Recorder.TakeSnapshot(Time.deltaTime);
                nextCaptureTime = Time.time + (1 / captureFramerate);
            }
        }

        public void EndRecording()
        {
            var filterOptions = new CurveFilterOptions
            {
                keyframeReduction = true,
                positionError = 1f,
                rotationError = 0.5f,
                scaleError = 1f,
                floatError = 1f,
            };

            if (_lossyCompression)
            {
                m_Recorder.SaveToClip(_targetClip, captureFramerate, filterOptions);
            }
            else
            {
                m_Recorder.SaveToClip(_targetClip, captureFramerate);
            }

            FilterClip(_targetClip);

            m_Recorder.ResetRecording();

            int numHands = 0;

            if (leftHandBoneRoot != null)
                numHands += 1;

            if (rightHandBoneRoot)
                numHands += 1;

            string completionMessage;

            if (additionalObjects != null)
            {
                completionMessage = $"Recorded {numHands} hands and {additionalObjects.Length} " +
                    $"secondary objects to the animation {_targetClip.name} successfully. To replay " +
                    "the animation, make sure the hierarchy is the same as the recorded one and " +
                    $"attach the animation to the common parent ({gameObject.name}).";
            }
            else
            {
                completionMessage = $"Recorded {numHands} hands to the animation {_targetClip.name} " +
                    "successfully. To replay the animation, make sure the hierarchy is the same as " +
                    "the recorded one and attach the animation to the common parent " +
                    $"({gameObject.name}).";
            }

            Debug.Log(completionMessage);

            if (automaticallyGenerateAnimationClip)
            {
                Debug.Log("Animation Clip saved to " + fullClipPath);
            }

            OnRecordingComplete?.Invoke(_targetClip);

            recording = false;
        }

        private void FilterClip(AnimationClip clip)
        {
            foreach (var bind in AnimationUtility.GetCurveBindings(clip))
            {
                var curve = AnimationUtility.GetEditorCurve(clip, bind);
                for (var i = 0; i < curve.keys.Length; ++i)
                {
                    AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
                    AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
                }

                AnimationUtility.SetEditorCurve(clip, bind, curve);
            }
        }

        private void AutoSelectHandRoots()
        {
            HandModelBase leftHand = null;
            HandModelBase rightHand = null;

            HandModelBase[] childHands = GetComponentsInChildren<HandModelBase>(true);

            foreach (var hand in childHands)
            {
                if (hand.Handedness == Chirality.Left)
                    leftHand = hand;
                else
                    rightHand = hand;
            }

            switch (handsToRecord)
            {
                case ChiralityOptions.BOTH:
                    SetHandModelBaseToRoot(leftHand, Chirality.Left);
                    SetHandModelBaseToRoot(rightHand, Chirality.Right);
                    break;
                case ChiralityOptions.LEFT:
                    SetHandModelBaseToRoot(leftHand, Chirality.Left);
                    break;
                case ChiralityOptions.RIGHT:
                    SetHandModelBaseToRoot(rightHand, Chirality.Right);
                    break;
            }
        }

        private void SetHandModelBaseToRoot(HandModelBase hand, Chirality chirality)
        {
            GameObject root = null;

            if (hand == null)
            {
                Debug.LogWarning("Hand Recorder: Unable to set " + chirality + " hand to record. There is no suitable HandModelBase under the HandRecorder");
            }
            else
            {
                root = hand.gameObject;
            }

            switch (chirality)
            {
                case Chirality.Left:
                    leftHandBoneRoot = root;
                    break;
                case Chirality.Right:
                    rightHandBoneRoot = root;
                    break;
            }
        }
    }
}
#endif