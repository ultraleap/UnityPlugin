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
        public enum TrackingSource
        {
            AUTOMATIC,
            OPEN_XR,
            LEAP_SERVICE
        }

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
        /// The chosen source of hand tracking data
        /// </summary>
        [Tooltip("AUTOMATIC - Windows will use LeapService, other platforms will use OpenXR if available and LeapService if OpenXR if not" +
                    "OPEN_XR - Use OpenXR as the source of tracking" +
                    "LEAP_SERVICE - Use LeapC as the source of tracking")]
        public TrackingSource trackingSource;

        private IEnumerator Start()
        {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            // Default to Leap Service when using Editor/Windows due to OpenXR tracking source issues when Autmatically selecting
            if (trackingSource == TrackingSource.AUTOMATIC)
            {
                trackingSource = TrackingSource.LEAP_SERVICE;
            }
#endif

            if (trackingSource == TrackingSource.AUTOMATIC)
            {
                while (XRGeneralSettings.Instance == null) yield return new WaitForEndOfFrame();

                if (openXRLeapProvider != null && openXRLeapProvider.CanProvideData)
                {
                    SelectTrackingSource(TrackingSource.OPEN_XR);
                }
                else
                {
                    SelectTrackingSource(TrackingSource.LEAP_SERVICE);
                }
            }
            else
            {
                SelectTrackingSource(trackingSource);
            }

            LeapProvider.gameObject.SetActive(true);

            OnProviderSet?.Invoke(LeapProvider);
        }


        /// <summary>
        /// Sets the tracking source by enabling and deleting respective Providers
        /// </summary>
        /// <param name="_source">A tracking source. Where possible, this should not be AUTOMATIC when this method is called</param>
        void SelectTrackingSource(TrackingSource _source)
        {
            if (_source == TrackingSource.AUTOMATIC)
            {
                Debug.LogWarning("No specific Tracking Source selected. Automatically selecting Leap Service");
                _source = TrackingSource.LEAP_SERVICE;
            }

            if (_source == TrackingSource.OPEN_XR)
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