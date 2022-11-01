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
using UnityEngine.XR.OpenXR;
using Ultraleap.Tracking.OpenXR;

namespace Leap.Unity
{
    /// <summary>
    /// XRLeapProviderManager offers a single access point while selecting the most suitable Hand Tracking Data source
    /// available at the time of application launch.
    /// The order of selection is: OpenXR -> LeapC
    /// </summary>
    public class XRLeapProviderManager : PostProcessProvider
    {
        [SerializeField] private OpenXRLeapProvider openXRLeapProvider;
        [SerializeField] private LeapXRServiceProvider leapXRServiceProvider;

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
        }

        /// <summary>
        /// An optional override to force the use of the LeapC tracking data
        /// </summary>
        [Tooltip("Forces the use of the non-OpenXR provider, using LeapC hand tracking")]
        public bool forceLeapService;

        private IEnumerator Start()
        {
            while (XRGeneralSettings.Instance == null) yield return new WaitForEndOfFrame();

            if (XRGeneralSettings.Instance.Manager.activeLoader.name == "Open XR Loader" &&
                OpenXRSettings.Instance.GetFeature<HandTrackingFeature>() != null &&
                OpenXRSettings.Instance.GetFeature<HandTrackingFeature>().enabled &&
                !forceLeapService)
            {
                Debug.Log("Using OpenXR for Hand Tracking");
                _leapProvider = openXRLeapProvider;
                Destroy(leapXRServiceProvider.gameObject);
            }
            else
            {
                Debug.Log("Using LeapService for Hand Tracking");
                _leapProvider = leapXRServiceProvider;
                Destroy(openXRLeapProvider.gameObject);
            }


            inputLeapProvider = LeapProvider;
            OnProviderSet?.Invoke(inputLeapProvider);
        }

        public override void ProcessFrame(ref Frame inputFrame)
        {
        }
    }
}