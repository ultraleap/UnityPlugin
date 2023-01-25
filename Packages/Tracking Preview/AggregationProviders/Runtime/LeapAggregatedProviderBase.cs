/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity.Encoding;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity
{
    using Attributes;

    /// <summary>
    /// Base class for aggregating frame data. Waits for frame data from all specified providers and then calls MergeFrames to combine them.
    /// Implement MergeFrames(Frame[] frames) in an inherited class
    /// </summary>
    public abstract class LeapAggregatedProviderBase : LeapProvider
    {
        #region Inspector

        /// <summary>
        /// A list of providers that are used for aggregation
        /// </summary>
        [Tooltip("Add all providers here that you want to be used for aggregation")]
        [EditTimeOnly]
        public LeapProvider[] providers;

        public enum FrameOptimizationMode
        {
            None,
            ReuseUpdateForPhysics,
            ReusePhysicsForUpdate,
        }
        [Tooltip("When enabled, the provider will only calculate one leap frame instead of two.")]
        [SerializeField]
        protected FrameOptimizationMode _frameOptimization = FrameOptimizationMode.None;

        #endregion

        #region Internal Settings & Memory

        protected Frame _transformedUpdateFrame, _transformedFixedFrame;

        // list of frames that are send to MergeFrames() to aggregate to a single frame
        Frame[] updateFramesToCombine;
        Frame[] fixedUpdateFramesToCombine;

        #endregion

        #region Edit-time Frame Data

#if UNITY_EDITOR
        private Frame _backingUntransformedEditTimeFrame = null;
        private Frame _untransformedEditTimeFrame
        {
            get
            {
                if (_backingUntransformedEditTimeFrame == null)
                {
                    _backingUntransformedEditTimeFrame = new Frame();
                }
                return _backingUntransformedEditTimeFrame;
            }
        }
        private Frame _backingEditTimeFrame = null;
        private Frame _editTimeFrame
        {
            get
            {
                if (_backingEditTimeFrame == null)
                {
                    _backingEditTimeFrame = new Frame();
                }
                return _backingEditTimeFrame;
            }
        }

        private Dictionary<TestHandFactory.TestHandPose, Hand> _cachedLeftHands
          = new Dictionary<TestHandFactory.TestHandPose, Hand>();
        private Hand _editTimeLeftHand
        {
            get
            {
                Hand cachedHand;
                if (_cachedLeftHands.TryGetValue(editTimePose, out cachedHand))
                {
                    return cachedHand;
                }
                else
                {
                    cachedHand = TestHandFactory.MakeTestHand(isLeft: true, pose: editTimePose);
                    _cachedLeftHands[editTimePose] = cachedHand;
                    return cachedHand;
                }
            }
        }

        private Dictionary<TestHandFactory.TestHandPose, Hand> _cachedRightHands
          = new Dictionary<TestHandFactory.TestHandPose, Hand>();
        private Hand _editTimeRightHand
        {
            get
            {
                Hand cachedHand;
                if (_cachedRightHands.TryGetValue(editTimePose, out cachedHand))
                {
                    return cachedHand;
                }
                else
                {
                    cachedHand = TestHandFactory.MakeTestHand(isLeft: false, pose: editTimePose);
                    _cachedRightHands[editTimePose] = cachedHand;
                    return cachedHand;
                }
            }
        }

#endif

        #endregion

        #region LeapProvider Implementation

        public override Frame CurrentFrame
        {
            get
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    _editTimeFrame.Hands.Clear();
                    _untransformedEditTimeFrame.Hands.Clear();
                    _untransformedEditTimeFrame.Hands.Add(_editTimeLeftHand);
                    _untransformedEditTimeFrame.Hands.Add(_editTimeRightHand);
                    transformFrame(_untransformedEditTimeFrame, _editTimeFrame);
                    return _editTimeFrame;
                }
#endif
                if (_frameOptimization == FrameOptimizationMode.ReusePhysicsForUpdate)
                {
                    return _transformedFixedFrame;
                }
                else
                {
                    return _transformedUpdateFrame;
                }
            }
        }

        public override Frame CurrentFixedFrame
        {
            get
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    _editTimeFrame.Hands.Clear();
                    _untransformedEditTimeFrame.Hands.Clear();
                    _untransformedEditTimeFrame.Hands.Add(_editTimeLeftHand);
                    _untransformedEditTimeFrame.Hands.Add(_editTimeRightHand);
                    transformFrame(_untransformedEditTimeFrame, _editTimeFrame);
                    return _editTimeFrame;
                }
#endif
                if (_frameOptimization == FrameOptimizationMode.ReuseUpdateForPhysics)
                {
                    return _transformedUpdateFrame;
                }
                else
                {
                    return _transformedFixedFrame;
                }
            }
        }

        #endregion

        #region Unity Events

        protected virtual void Reset()
        {
            editTimePose = TestHandFactory.TestHandPose.DesktopModeA;
        }

        protected virtual void OnValidate()
        {
            validateInput();
        }

        private void validateInput()
        {
            if (detectCircularProviderReference(this, new List<LeapAggregatedProviderBase>()))
            {
                enabled = false;
                Debug.LogError("The input providers on the aggregation provider on " + gameObject.name
                             + " causes an infinite cycle, so it has been disabled.");
            }
        }

        /// <summary>
        /// aggregation providers wait for all their input providers' update events, so looping them won't work
        /// this detects a circular reference
        /// </summary>
        private bool detectCircularProviderReference(LeapAggregatedProviderBase currentProvider, List<LeapAggregatedProviderBase> seenProviders)
        {
            if (currentProvider.providers == null) return false;

            if (seenProviders.Contains(currentProvider)) return true;

            foreach (LeapProvider provider in currentProvider.providers)
            {
                if (provider is LeapAggregatedProviderBase)
                {
                    List<LeapAggregatedProviderBase> newSeenProvider = new List<LeapAggregatedProviderBase>(seenProviders);
                    newSeenProvider.Add(currentProvider);
                    if (detectCircularProviderReference(provider as LeapAggregatedProviderBase, newSeenProvider)) return true;
                }
            }
            return false;
        }

        protected virtual void Awake()
        {
            // if any of the providers are aggregation providers, warn the user 
            foreach (LeapProvider provider in providers)
            {
                if (provider is LeapAggregatedProviderBase)
                {
                    Debug.LogWarning("You are trying to aggregate an aggregation provider. This might lead to latency. " +
                        "consider writing your own aggregator instead");
                }
            }

            updateFramesToCombine = new Frame[providers.Length];
            fixedUpdateFramesToCombine = new Frame[providers.Length];

            // subscribe to the update events and fixed update events of all providers in the public list 'providers'
            // when an update event happens, add its frame to the framesToCombine lists and then check whether the whole list is filled.
            // if it is, call updateFrame or updateFixedFrame
            for (int i = 0; i < providers.Length; i++)
            {
                int idx = i;
                providers[i].OnUpdateFrame += (x) =>
                {
                    updateFramesToCombine[idx] = x;
                    if (CheckFramesFilled(updateFramesToCombine)) UpdateFrame();
                };
                providers[i].OnFixedFrame += (x) =>
                {
                    fixedUpdateFramesToCombine[idx] = x;
                    if (CheckFramesFilled(fixedUpdateFramesToCombine)) UpdateFixedFrame();
                };
            }
        }

        protected virtual void Start()
        {
            _transformedUpdateFrame = new Frame();
            _transformedFixedFrame = new Frame();
        }

        protected void OnPostRender()
        {
            // reset all frames in framesToCombineLists, if they haven't been used this unity frame
            // This can happen, if one of the providers doesn't dispatch an update event
            Utils.Fill(updateFramesToCombine, null);
            Utils.Fill(fixedUpdateFramesToCombine, null);
        }


        #endregion

        #region aggregation functions

        bool CheckFramesFilled(Frame[] frames)
        {
            foreach (Frame frame in frames)
            {
                if (frame == null) return false;
            }
            return true;
        }

        protected virtual void UpdateFrame()
        {
            // merge all update frames
            _transformedUpdateFrame = MergeFrames(updateFramesToCombine);

            // reset all the update frames received from providers to null again
            Utils.Fill(updateFramesToCombine, null);

#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isCompiling)
            {
                UnityEditor.EditorApplication.isPlaying = false;
                Debug.LogWarning("Unity hot reloading not currently supported. Stopping Editor Playback.");
                return;
            }
#endif

            if (_frameOptimization == FrameOptimizationMode.ReusePhysicsForUpdate)
            {
                DispatchUpdateFrameEvent(_transformedFixedFrame);
                return;
            }

            if (_transformedUpdateFrame != null)
            {
                DispatchUpdateFrameEvent(_transformedUpdateFrame);
            }
        }

        protected virtual void UpdateFixedFrame()
        {

            // merge all fixed update frames
            _transformedFixedFrame = MergeFrames(fixedUpdateFramesToCombine);

            // reset all the fixed update frames received from providers to null again
            Utils.Fill(fixedUpdateFramesToCombine, null);

            if (_frameOptimization == FrameOptimizationMode.ReuseUpdateForPhysics)
            {
                DispatchFixedFrameEvent(_transformedUpdateFrame);
                return;
            }

            if (_transformedFixedFrame != null)
            {
                DispatchFixedFrameEvent(_transformedFixedFrame);
            }
        }

        /// <summary>
        /// defines how a list of frames can be merged into a single frame. 
        /// This needs to be implemented in every aggregation provider
        /// </summary>
        /// <param name="frames"> a list of all frames received from the providers</param>
        /// <returns> a merged frame </returns>
        protected abstract Frame MergeFrames(Frame[] frames);

        #endregion

        #region Internal Methods

        protected virtual void transformFrame(Frame source, Frame dest)
        {
            dest.CopyFrom(source).Transform(new LeapTransform(transform));
        }

        #endregion

    }

}