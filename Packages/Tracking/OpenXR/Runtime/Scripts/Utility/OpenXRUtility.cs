/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;

namespace Leap.Tracking.OpenXR
{
    /// <summary>
    /// A utility to pass data to the core plugin through one central entry point
    /// </summary>
    public class OpenXRUtility
    {
        static HandTrackingFeature currentFeatureInstance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (XRGeneralSettings.Instance != null &&
                XRGeneralSettings.Instance.Manager != null &&
                XRGeneralSettings.Instance.Manager.ActiveLoaderAs<OpenXRLoaderBase>() != null &&
                OpenXRSettings.Instance != null &&
                OpenXRSettings.Instance.GetFeature<HandTrackingFeature>() != null &&
                OpenXRSettings.Instance.GetFeature<HandTrackingFeature>().SupportsHandTracking)
            {
                currentFeatureInstance = OpenXRSettings.Instance.GetFeature<HandTrackingFeature>();

                if (currentFeatureInstance != null && currentFeatureInstance.SupportsHandTracking)
                {
                    if (currentFeatureInstance.IsUltraleapHandTracking)
                    {
                        HandTrackingSourceUtility.LeapOpenXRTrackingAvailable = true;

                        if (currentFeatureInstance.SupportsHandTrackingHints)
                        {
                            HandTrackingSourceUtility.LeapOpenXRHintingAvailable = true;

                            HandTrackingHintManager.OnOpenXRHintRequest -= RequestOpenXRHints;
                            HandTrackingHintManager.OnOpenXRHintRequest += RequestOpenXRHints;
                        }
                    }
                    else
                    {
                        HandTrackingSourceUtility.NonLeapOpenXRTrackingAvailable = true;
                    }
                }
            }
        }

        private static void RequestOpenXRHints(string[] hints)
        {
            currentFeatureInstance.SetHandTrackingHints(hints);
        }
    }
}