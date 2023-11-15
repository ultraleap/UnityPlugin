#if UNITY_EDITOR

using Leap.Unity.HandsModule;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Leap.Unity.Recording
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
        protected bool _lossyCompression = true;

        [Range(0, 3)]
        public int frameSmoothing = 4;

        [Space, Header("Recording")]
        public KeyCode toggleRecordingKey = KeyCode.None;

        public virtual bool shouldHaveCommonParent
        {
            get
            {
                if (rightHandBoneRoot != null) return true;

                if (additionalObjects.Length == 0) return false;

                var secondaryIsEmpty = true;

                foreach (GameObject secondary in additionalObjects)
                {
                    if (secondary != null)
                    {
                        secondaryIsEmpty = false;
                    }
                }

                return !secondaryIsEmpty;
            }
        }

        public void OnDisable()
        {
            if (_targetClip == null)
                return;

            if (m_Recorder && m_Recorder.isRecording)
            {
                m_Recorder.SaveToClip(_targetClip);
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

            if (shouldHaveCommonParent && !AreObjectsChildren())
            {
                Debug.LogError("Could not start a Hand Recording - had multiple elements to record " +
                                    "with no common parent!");
                return;
            }

            _targetClip = null;

            if (!automaticallyGenerateAnimationClip)
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

            recording = true;
        }

        void MakeAnimaitonClipAsset()
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

            AssetDatabase.CreateAsset(newItem, fullPath);
            AssetDatabase.Refresh();

            _targetClip = newItem;
        }

        public void LateUpdate()
        {
            if (_targetClip == null)
            {
                return;
            }

            if (recording)
            {
                // Take a snapshot and record all the bindings values for this frame.
                m_Recorder.TakeSnapshot(Time.deltaTime);
            }
        }

        public void EndRecording()
        {
            var filterOptions = new CurveFilterOptions
            {
                keyframeReduction = true
            };

            if (_lossyCompression)
            {
                filterOptions.positionError = .5f;
                filterOptions.rotationError = .5f;
                filterOptions.scaleError = .5f;
                filterOptions.floatError = .5f;
            }

            int fps = 60;

            switch (frameSmoothing)
            {
                case 1:
                    fps = 30;
                    break;
                case 2:
                    fps = 15;
                    break;
                case 3:
                    fps = 6;
                    break;
            }

            m_Recorder.SaveToClip(_targetClip, fps, filterOptions);
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

            recording = false;
        }

        void FilterClip(AnimationClip clip)
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

        public void AutoSelectHandRoots()
        {
            HandBinder leftBinder = null;
            HandBinder rightBinder = null;

            HandBinder[] childBinders = GetComponentsInChildren<HandBinder>(true);

            foreach (var binder in childBinders)
            {
                if (binder.Chirality == Chirality.Left)
                    leftBinder = binder;
                else
                    rightBinder = binder;
            }

            switch (handsToRecord)
            {
                case ChiralityOptions.BOTH:
                    SetBinderToRoot(leftBinder, Chirality.Left);
                    SetBinderToRoot(rightBinder, Chirality.Right);
                    break;
                case ChiralityOptions.LEFT:
                    SetBinderToRoot(leftBinder, Chirality.Left);
                    break;
                case ChiralityOptions.RIGHT:
                    SetBinderToRoot(rightBinder, Chirality.Right);
                    break;
            }
        }

        void SetBinderToRoot(HandBinder binder, Chirality chirality)
        {
            GameObject root = null;

            if (binder == null)
            {
                Debug.LogWarning("Hand Recorder: Unable to set " + chirality + " hand to record. There is no suitable HandBinder under the HandRecorder");
            }
            else
            {
                root = binder.BoundHand?.elbow?.boundTransform?.gameObject;

                if (root == null)
                    root = binder.BoundHand?.wrist?.boundTransform?.gameObject;

                if (root == null)
                {
                    Debug.LogWarning("Hand Recorder: Unable to set " + chirality + " hand to record. The HandBinder has no suitable root");
                }
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

        // Find the common parent between *all* recorded objects
        public virtual bool AreObjectsChildren()
        {
            if (rightHandBoneRoot != null &&
                !rightHandBoneRoot.transform.IsChildOf(transform))
            {
                return false;
            }

            if (additionalObjects != null)
            {
                foreach (GameObject secondary in additionalObjects)
                {
                    if (secondary != null &&
                        !secondary.transform.IsChildOf(transform))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
#endif