/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace Leap.Unity
{
    public static class HandTrackingHintManager
    {
        private static List<string> currentHints = new List<string>();

        public static Action<string[]> OnOpenXRHintRequest;

        /// <summary>
        /// Capture the values in UltraleapSettings and set up the setting of those hints when the first device is discovered
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartupHints()
        {
            currentHints = UltraleapSettings.Instance.startupHints.ToList();

            if (HandTrackingSourceUtility.LeapOpenXRHintingAvailable)
            {
                // Use OpenXR for Hints
            }
            else
            {
                // Use a LeapC connection for Hints
                SetupDeviceCallbacks();
            }

            RequestAllExistingHints();
        }

        /// <summary>
        /// Remove a hint from the existing hints and then send them all to the Service
        /// </summary>
        public static void RemoveHint(string hint)
        {
            if (currentHints.Contains(hint))
            {
                currentHints.Remove(hint);
                RequestHandTrackingHints(currentHints.ToArray());
            }
            else
            {
                Debug.Log("Hand Tracking Hint: " + hint + " was not previously requested. No hints were changed.");
            }
        }

        /// <summary>
        /// Add a hint to the existing hints and then send them all to the Service
        /// </summary>
        public static void AddHint(string hint)
        {
            if (!currentHints.Contains(hint))
            {
                currentHints.Add(hint);
                RequestHandTrackingHints(currentHints.ToArray());
            }
            else
            {
                Debug.Log("Hand Tracking Hint: " + hint + " was already requested. No hints were changed.");
            }
        }

        /// <summary>
        /// Used to re-send the existing hints for each device that has had hints sent.
        /// Consider calling this when a connection is lost and re-established
        /// </summary>
        public static void RequestAllExistingHints()
        {
            if (currentHints.Count != 0)
            {
                RequestHandTrackingHints(currentHints.ToArray());
            }
        }

        /// <summary>
        /// Send a specific set of hints, if this does not include previously set ones, they will be cleared.
        /// </summary>
        /// <param name="hints">The hints you wish to send</param>
        public static void RequestHandTrackingHints(string[] hints)
        {
            if (hints == null)
            {
                hints = new string[0];
            }

            currentHints = hints.ToList();

            string sourceName = "LeapC";

            if (HandTrackingSourceUtility.LeapOpenXRHintingAvailable)
            {
                // Use OpenXR for Hints
                OnOpenXRHintRequest?.Invoke(hints);
                sourceName = "OpenXR";
            }
            else
            {
                // Use a LeapC connection for Hints
                RequestHintsOnAllDevices(hints);
            }

            // Log the results
            LogRequestedHints(hints, sourceName);
        }

        static void LogRequestedHints(string[] hints, string sourceName)
        {
            // Log the requeste hints
            string logString = sourceName + " Hand Tracking Hints have been requested:";

            if (hints.Length > 0)
            {
                foreach (string hint in hints)
                {
                    logString += " " + hint + ",";
                }

                logString = logString.Remove(logString.Length - 1);
            }
            else
            {
                logString = sourceName + " Hand Tracking Hints have been cleared";
            }

            Debug.Log(logString);
        }

        #region LeapC Implementation

        static Controller _leapController;
        static Controller LeapController
        {
            get
            {
                if (_leapController == null) // Find any existing controller
                {
                    LeapServiceProvider provider = UnityEngine.Object.FindObjectOfType<LeapServiceProvider>(true);

                    if (provider != null && provider.GetLeapController() != null)
                    {
                        _leapController = provider.GetLeapController();
                    }
                    else // No leapserviceprovider, we should make a new controller ourselves
                    {
                        _leapController = new Controller(0);
                    }
                }

                return _leapController;
            }
            set
            {
                _leapController = value;
            }
        }

        private static void SetupDeviceCallbacks()
        {
            LeapController.Device -= NewLeapDevice;
            LeapController.Device += NewLeapDevice;
        }

        /// <summary>
        /// Re-request all hints when new devices connect
        private static void NewLeapDevice(object sender, DeviceEventArgs e)
        {
            RequestAllExistingHints();
        }

        private static void RequestHintsOnAllDevices(string[] hints)
        {
            // Send the hint to all devices
            foreach (var device in LeapController.Devices.ActiveDevices)
            {
                LeapController.RequestHandTrackingHints(hints, device);
            }
        }

        #endregion
    }
}