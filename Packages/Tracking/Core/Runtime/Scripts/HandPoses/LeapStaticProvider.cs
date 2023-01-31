/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace Leap.Unity
{

    using TestHandPose = TestHandFactory.TestHandPose;
    /// <summary>
    /// Provides Frame object data to the Unity application by firing events as soon
    /// as Frame data is available. Frames contain all currently tracked Hands in view
    /// of the Leap Motion Controller.
    /// 
    /// LeapProvider defines the basic interface our plugin expects to use to retrieve 
    /// Frame data. This abstraction allows you to create your own LeapProviders, which 
    /// is useful when testing or developing in a context where Ultraleap Hand Tracking 
    /// hardware isn't immediately available.
    /// </summary>
    public class LeapStaticProvider : LeapProvider
    {
        [SerializeField]
        HandPoseScriptableObject PoseScriptableObject = null;

        static Frame replaceFrame = new Frame();

        [SerializeField] private LeapProvider leapXRServiceProvider;

        /// <summary>
        /// An event that is fired when a LeapProvider is chosen
        /// </summary>
        public Action<LeapProvider> OnProviderSet;
        /// <summary>
        /// The currently chosen LeapProvider
        /// </summary>
        private LeapProvider _leapProvider = null;
        public LeapProvider LeapProvider
        {
            get
            {
                return (_leapProvider == null) ? leapXRServiceProvider : _leapProvider;
            }
            private set
            {
                if (Application.isPlaying && _leapProvider != null)
                {
                    _leapProvider.OnFixedFrame -= HandleFixedFrame;
                    _leapProvider.OnUpdateFrame -= HandleUpdateFrame;
                }

                _leapProvider = value;

                if (Application.isPlaying && _leapProvider != null)
                {
                    _leapProvider.OnFixedFrame -= HandleFixedFrame; // safeguard double-subscription
                    _leapProvider.OnFixedFrame += HandleFixedFrame;
                    _leapProvider.OnUpdateFrame -= HandleUpdateFrame; // safeguard double-subscription
                    _leapProvider.OnUpdateFrame += HandleUpdateFrame;
                }
            }
        }

        private void Start()
        {
            PoseScriptableObject = new HandPoseScriptableObject();
            LeapProvider.gameObject.SetActive(true);
            OnProviderSet?.Invoke(LeapProvider);
        }

        public override Frame CurrentFrame => LeapProvider.CurrentFrame;
        public override Frame CurrentFixedFrame => LeapProvider.CurrentFixedFrame;

        private Frame ReplaceFrameWithSerializedHand()
        {
            replaceFrame.Hands[0] = PoseScriptableObject.GetSerializedHand();
            return replaceFrame;
        }

        /// <summary>
        /// Directly pass the Frame data through to anyone that is listening to our own events
        /// </summary>
        void HandleUpdateFrame(Frame _frame)
        {
            _frame = ReplaceFrameWithSerializedHand();
            DispatchUpdateFrameEvent(_frame);
        }

        /// <summary>
        /// Directly pass the Frame data through to anyone that is listening to our own events
        /// </summary>
        void HandleFixedFrame(Frame _frame)
        {
            _frame = ReplaceFrameWithSerializedHand();
            DispatchFixedFrameEvent(_frame);
        }

    }



}