/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using System.Collections;
using UnityEngine;

#if XR_MANAGEMENT_AVAILABLE
using UnityEngine.XR.Management;
#endif

namespace Leap.Unity
{
    /// <summary>
    /// XRLeapProviderManager offers a single access point while selecting the most suitable Hand Tracking Data source
    /// available at the time of application launch.
    /// The order of selection is: UL OpenXR -> Leap Direct -> OpenXR
    /// </summary>
    public class XRLeapProviderManager : LeapProvider
    {
        /// <summary>
        /// Tracking sources:
        /// AUTOMATIC - Automatically determines the most suitable tracking type
        /// OPEN_XR - Ultraleap or other hand tracking provider
        /// LEAP_DIRECT - Uses the Ultraleap Tracking Service directly
        /// </summary>
        public enum TrackingSourceType
        {
            AUTOMATIC,
            OPEN_XR,
            LEAP_DIRECT
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
        [Tooltip("AUTOMATIC - Windows will use LeapService, other platforms will use OpenXR if available and LeapService if OpenXR is not" +
                    "OPEN_XR - Use OpenXR as the source of tracking" +
                    "LEAP_DIRECT - Directly use LeapC as the source of tracking")]
        public TrackingSourceType trackingSource;

        private IEnumerator Start()
        {
#if !XR_MANAGEMENT_AVAILABLE
            trackingSource = TrackingSourceType.LEAP_DIRECT;
            Debug.Log("Unity XR Management not available. Automatically selecting LEAP_DIRECT for Hand Tracking");
#endif

            if (trackingSource == TrackingSourceType.AUTOMATIC)
            {
#if XR_MANAGEMENT_AVAILABLE
                while (XRGeneralSettings.Instance == null ||
                        XRGeneralSettings.Instance.Manager == null ||
                        XRGeneralSettings.Instance.Manager.activeLoader == null)
                {
                    yield return new WaitForEndOfFrame();
                }
                
                if (openXRLeapProvider != null && openXRLeapProvider.TrackingDataSource == TrackingSource.OPENXR_LEAP)
                {
                    SelectTrackingSource(TrackingSourceType.OPEN_XR);
                }
                else if (leapXRServiceProvider != null && leapXRServiceProvider.TrackingDataSource == TrackingSource.LEAPC)
                {
                    SelectTrackingSource(TrackingSourceType.LEAP_DIRECT);
                }
                else if (openXRLeapProvider != null && openXRLeapProvider.TrackingDataSource == TrackingSource.OPENXR)
                {
                    SelectTrackingSource(TrackingSourceType.OPEN_XR);
                }
                else
                {
                    Debug.LogWarning("No hand tracking sources found");
                    yield break;
                }
#endif
            }
            else
            {
                SelectTrackingSource(trackingSource);
            }

            LeapProvider.gameObject.SetActive(true);

            OnProviderSet?.Invoke(LeapProvider);

            yield break;
        }


        /// <summary>
        /// Sets the tracking source by enabling and deleting respective Providers
        /// </summary>
        /// <param name="_source">A tracking source. Where possible, this should not be AUTOMATIC when this method is called</param>
        void SelectTrackingSource(TrackingSourceType _source)
        {
            if (_source == TrackingSourceType.AUTOMATIC)
            {
                Debug.LogWarning("No specific Tracking Source selected. Automatically selecting LEAP_DIRECT");
                _source = TrackingSourceType.LEAP_DIRECT;
            }

            if (_source == TrackingSourceType.OPEN_XR)
            {
                LeapProvider = openXRLeapProvider;

                if (openXRLeapProvider != null && openXRLeapProvider.TrackingDataSource == TrackingSource.OPENXR_LEAP)
                {
                    Debug.Log("Using Ultraleap OpenXR for Hand Tracking");
                }
                else
                {
                    Debug.Log("Using OpenXR for Hand Tracking");
                }

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