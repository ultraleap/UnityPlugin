using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace Leap.Unity
{
    public static class HandTrackingHintManager
    {
        static Dictionary<Device, List<string>> currentDeviceHints = new Dictionary<Device, List<string>>();

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

            LeapController.Connect -= OnLeapCConnect;
            LeapController.Connect += OnLeapCConnect;
        }

        /// <summary>
        /// Only used for initial device detection, then will send Startup Hints for the first device
        /// </summary>
        private static void NewLeapDevice(object sender, DeviceEventArgs e)
        {
            if (!currentDeviceHints.ContainsKey(e.Device))
            {
                currentDeviceHints.Add(e.Device, UltraleapSettings.Instance.startupHints.ToList());

                if (currentDeviceHints[e.Device].Count != 0)
                {
                    RequestHandTrackingHints(currentDeviceHints[e.Device].ToArray());
                }
            }

            LeapController.Device -= NewLeapDevice;
        }

        /// <summary>
        /// Remove a hint from the existing hints and then send them all to the Service for the current device of the current controller
        /// </summary>
        public static void RemoveHint(string hint, Device device = null)
        {
            // Find the first available on the Controller, or we cannot set a hint
            if (device == null && !TryGetFirstDevice(out device))
            {
                Debug.Log("Unable to remove Hand Tracking Hints. No device was provided and no devices are currently connected.");
                return;
            }

            if (currentDeviceHints.ContainsKey(device))
            {
                if(currentDeviceHints[device].Remove(hint))
                {
                    RequestHandTrackingHints(currentDeviceHints[device].ToArray(), device);
                }
                else
                {
                    Debug.Log("Hand Tracking Hint: " + hint + " was not previously set. No hints were changed.");
                }
            }
        }

        /// <summary>
        /// Add a hint to the existing hints and then send them all to the Service for the current device of the current controller
        /// </summary>
        public static void AddHint(string hint, Device device = null)
        {
            // Find the first available on the Controller, or we cannot set a hint
            if (device == null && !TryGetFirstDevice(out device))
            {
                Debug.Log("Unable to add Hand Tracking Hints. No device was provided and no devices are currently connected.");
                return;
            }

            if (currentDeviceHints.ContainsKey(device))
            {
                currentDeviceHints[device].Add(hint);
            }
            else
            {
                currentDeviceHints.Add(device, new List<string> { hint });
            }

            RequestHandTrackingHints(currentDeviceHints[device].ToArray(), device);
        }

        /// <summary>
        /// Used to re-send the existing hints for each device that has had hints sent.
        /// Consider calling this when a connection is lost and re-established
        /// </summary>
        public static void RequestAllExistingHints()
        {
            foreach (var device in currentDeviceHints.Keys)
            {
                RequestHandTrackingHints(currentDeviceHints[device].ToArray(), device);
            }
        }

        /// <summary>
        /// Send a specific set of hints, if this does not include previously set ones, they will be cleared.
        /// </summary>
        /// <param name="hints">The hints you wish to send</param>
        /// <param name="device">An optional specific Device, otherwise the first found Device of the first Controller will be used</param>
        public static void RequestHandTrackingHints(string[] hints, Device device = null)
        {
            if(hints == null)
            {
                hints = new string[0];
            }

            // Find the first available on the Controller, or we cannot set a hint
            if (device == null && !TryGetFirstDevice(out device))
            {
                Debug.Log("Unable to request Hand Tracking Hints. No device was provided and no devices are currently connected.");
                return;
            }

            LeapController.RequestHandTrackingHints(hints, device);

            if (currentDeviceHints.ContainsKey(device))
            {
                currentDeviceHints[device] = hints.ToList();
            }
            else
            {
                currentDeviceHints.Add(device, hints.ToList());
            }

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

        private static bool TryGetFirstDevice(out Device device)
        {
            device = LeapController.Devices.ActiveDevices.FirstOrDefault();

            if(device == null)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Re-request all existing hints when the connection is established.
        /// This may be on a re-connection, or the initial connection.
        /// </summary>
        private static void OnLeapCConnect(object sender, ConnectionEventArgs eventArgs)
        {
            RequestAllExistingHints();
        }
    }
}