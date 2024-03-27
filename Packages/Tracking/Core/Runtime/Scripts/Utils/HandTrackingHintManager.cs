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
                        _leapController = new Controller(0, "Leap Service");
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
            LeapController.Device -= NewLeapDevice;
            LeapController.Device += NewLeapDevice;

            currentHints = UltraleapSettings.Instance.startupHints.ToList();
        }

        /// <summary>
        /// Re-request all hints when new devices connect
        private static void NewLeapDevice(object sender, DeviceEventArgs e)
        {
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
            if(!currentHints.Contains(hint))
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
            RequestHandTrackingHints(currentHints.ToArray());
        }

        /// <summary>
        /// Send a specific set of hints, if this does not include previously set ones, they will be cleared.
        /// </summary>
        /// <param name="hints">The hints you wish to send</param>
        public static void RequestHandTrackingHints(string[] hints)
        {
            if(hints == null)
            {
                hints = new string[0];
            }

            // Send the hint to all devices
            foreach (var device in LeapController.Devices.ActiveDevices)
            {
                LeapController.RequestHandTrackingHints(hints, device);
            }

            currentHints = hints.ToList();

            // Log the results
            LogRequestedHints(hints);
        }

        static void LogRequestedHints(string[] hints)
        {
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