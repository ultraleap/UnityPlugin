using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace Leap.Unity
{
    public static class HandTrackingHintManager
    {
        static List<string> currentHints = new List<string>();

        static Controller _leapController;
        static Controller LeapController
        {
            get
            {
                if (_leapController == null) // Find any existing controller
                {
                    LeapServiceProvider provider = Object.FindObjectOfType<LeapServiceProvider>(true);

                    if (provider != null)
                    {
                        _leapController = provider.GetLeapController();
                    }
                    else // No leapserviceprovider, we should make a new controller ourselves
                    {
                        _leapController = new Controller(0, "Leap Service", false);
                    }
                }
                return _leapController;
            }
            set
            {
                _leapController = value;
            }
        }

        /// <summary>
        /// Capture the values in UltraleapSettings and set up the setting of those hints when the first device is discovered
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartupHints()
        {
            currentHints = UltraleapSettings.Instance.startupHints.ToList();

            if (currentHints.Count != 0)
            {
                LeapController.Device -= NewLeapDevice;
                LeapController.Device += NewLeapDevice;
            }
        }

        /// <summary>
        /// Only used for initial device detection, then will send Startup Hints for the first device
        /// </summary>
        private static void NewLeapDevice(object sender, DeviceEventArgs e)
        {
            if (currentHints.Count != 0)
            {
                LeapController.Device -= NewLeapDevice;
                RequestHandTrackingHints(currentHints.ToArray());
            }
        }

        /// <summary>
        /// Remove a hint from the existing hints and then send them all to the Service for the current device of the current controller
        /// </summary>
        public static void RemoveHint(string hint)
        {
            if (currentHints.Remove(hint))
            {
                RequestHandTrackingHints(currentHints.ToArray());
            }
            else
            {
                Debug.Log("Hand Tracking Hint: " + hint + " was not previously set. No hints were changed.");
            }
        }

        /// <summary>
        /// Add a hint to the existing hints and then send them all to the Service for the current device of the current controller
        /// </summary>
        public static void AddHint(string hint)
        {
            if (!currentHints.Contains(hint))
            {
                currentHints.Add(hint);
            }

            RequestHandTrackingHints(currentHints.ToArray());
        }

        /// <summary>
        /// Send a specific set of hints, if this does not include previously set ones, they will be cleared.
        /// </summary>
        /// <param name="hints">The hints you wish to send</param>
        /// <param name="device">An optional specific Device, otherwise the first found Device of the first Controller will be used</param>
        public static void RequestHandTrackingHints(string[] hints, Device device = null)
        {
            LeapController.RequestHandTrackingHints(hints, device);
            currentHints = hints.ToList();

            // Log the requeste hints
            string logString = "Hand Tracking Hints have been requested:";

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
                logString = "Hand Tracking Hints have been cleared";
            }

            Debug.Log(logString);
        }
    }
}