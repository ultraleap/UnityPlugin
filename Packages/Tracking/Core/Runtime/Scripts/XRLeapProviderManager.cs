/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2022.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.Management;

namespace Leap.Unity
{
    /// <summary>
    /// XRLeapProviderManager offers a single access point while selecting the most suitable Hand Tracking Data source
    /// available at the time of application launch.
    /// The order of selection is: OpenXR -> LeapC
    /// </summary>
    public class XRLeapProviderManager : LeapProvider
    {
        [SerializeField] private LeapProvider openXRLeapProvider;
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

        public override Frame CurrentFrame => LeapProvider.CurrentFrame;
        public override Frame CurrentFixedFrame => LeapProvider.CurrentFixedFrame;

        /// <summary>
        /// An optional override to force the use of the LeapC tracking data
        /// </summary>
        [Tooltip("Forces the use of the non-OpenXR provider, using LeapC hand tracking")]
        public bool forceLeapService;

        private IEnumerator Start()
        {
            while (XRGeneralSettings.Instance == null) yield return new WaitForEndOfFrame();

            if (!forceLeapService && openXRLeapProvider != null && openXRLeapProvider.CanProvideData)
            {
                Debug.Log("Using OpenXR for Hand Tracking");
                LeapProvider = openXRLeapProvider;

                if (leapXRServiceProvider != null)
                {
                    Destroy(leapXRServiceProvider.gameObject);
                }
            }
            else
            {
                Debug.Log("Using LeapService for Hand Tracking");
                LeapProvider = leapXRServiceProvider;

                if (openXRLeapProvider != null)
                {
                    Destroy(openXRLeapProvider.gameObject);
                }
            }

            LeapProvider.gameObject.SetActive(true);

            OnProviderSet?.Invoke(LeapProvider);
        }

        /// <summary>
        /// Directly pass the Frame data through to anyone that is listening to our own events
        /// </summary>
        void HandleUpdateFrame(Frame _frame)
        {
            DispatchUpdateFrameEvent(_frame);
        }

        /// <summary>
        /// Directly pass the Frame data through to anyone that is listening to our own events
        /// </summary>
        void HandleFixedFrame(Frame _frame)
        {
            DispatchFixedFrameEvent(_frame);
        }
    }
}