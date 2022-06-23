/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2021.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

#if XR_MANAGEMENT_AVAILABLE
using UnityEngine.XR.Management;
#endif

namespace Leap.Unity
{

    /// <summary>
    /// Wraps various (but not all) "XR" calls with Unity 5.6-supporting "VR" calls
  	/// via #ifdefs.
  	/// </summary>
    public static class XRSupportUtil
    {

        private static System.Collections.Generic.List<XRNodeState> nodeStates =
          new System.Collections.Generic.List<XRNodeState>();
#if UNITY_2020_1_OR_NEWER
        static List<Vector3> _boundaryPoints = new List<Vector3>();
#endif

        public static bool IsXREnabled()
        {
#if SVR
            if (SvrManager.Instance != null)
            {
                return SvrManager.Instance.Initialized;
            }
#endif

#if XR_MANAGEMENT_AVAILABLE
            return XRGeneralSettings.Instance.Manager != null;
#else
            return XRSettings.enabled;
#endif
        }

        public static bool IsXRDevicePresent()
        {
#if SVR
            if (SvrManager.Instance != null)
            {
                return SvrManager.Instance.status.running;
            }
#endif

#if XR_MANAGEMENT_AVAILABLE
            return XRGeneralSettings.Instance.Manager.activeLoader != null;
#elif UNITY_2020_1_OR_NEWER
            return XRSettings.isDeviceActive;
#else
            return XRDevice.isPresent;
#endif
        }

        static bool outputPresenceWarning = false;
        public static bool IsUserPresent(bool defaultPresence = true)
        {
#if SVR
            if (SvrManager.Instance != null)
            {
                return true; // TODO user presence can be queried with recent versions of the SVR SDK (>= v4.0.4) - a proximity detector is fitted to some devices.
            }
#endif

            var devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, devices);

            if (devices.Count == 0 && !outputPresenceWarning)
            {
                Debug.LogWarning("No head-mounted devices found. Possibly no HMD is available to the XR system.");
                outputPresenceWarning = true;
            }

            if (devices.Count != 0)
            {
                var device = devices[0];
                if (device.TryGetFeatureValue(CommonUsages.userPresence, out var userPresent))
                {
                    return userPresent;
                }
            }
            return defaultPresence;
        }

        public static Vector3 GetXRNodeCenterEyeLocalPosition()
        {
#if SVR
            if (SvrManager.Instance != null)
            {
                if (XRSupportUtil.IsXREnabled())
                {
                    return SvrManager.Instance.head.transform.localPosition;
                }
                else
                {
                    Debug.LogWarning("Cannot read XRNodeCenterEyeLocalPosition as XR is not enabled");
                    return Vector3.zero;
                }
            }
#endif
            InputTracking.GetNodeStates(nodeStates);
            Vector3 position;

            foreach (XRNodeState state in nodeStates)
            {
                if (state.nodeType == XRNode.CenterEye &&
                    state.TryGetPosition(out position))
                {
                    return position;
                }
            }

            return Vector3.zero;
        }

        public static Quaternion GetXRNodeCenterEyeLocalRotation()
        {
#if SVR
            if (SvrManager.Instance != null)
            {
                if (XRSupportUtil.IsXREnabled())
                {
                    return SvrManager.Instance.head.transform.localRotation;
                }
                else
                {
                    Debug.LogWarning("Cannot read XRNodeCenterEyeLocalRotation as XR is not enabled");
                    return Quaternion.identity;
                }
            }
#endif
            InputTracking.GetNodeStates(nodeStates);
            Quaternion rotation;

            foreach (XRNodeState state in nodeStates)
            {
                if (state.nodeType == XRNode.CenterEye &&
                    state.TryGetRotation(out rotation))
                {
                    return rotation;
                }
            }

            return Quaternion.identity;
        }

        public static Vector3 GetXRNodeHeadLocalPosition()
        {
#if SVR
            if (SvrManager.Instance != null)
            {
                if (XRSupportUtil.IsXREnabled())
                {
                    return SvrManager.Instance.head.localPosition;
                }
                else
                {
                    Debug.LogWarning("Cannot read XRNodeHeadLocalPosition as XR is not enabled");
                    return Vector3.zero;
                }
            }
#endif
            InputTracking.GetNodeStates(nodeStates);
            Vector3 position;

            foreach (XRNodeState state in nodeStates)
            {
                if (state.nodeType == XRNode.Head &&
                   state.TryGetPosition(out position))
                {
                    return position;
                }
            }

            return Vector3.zero;
        }

        public static Quaternion GetXRNodeHeadLocalRotation()
        {
#if SVR
            if (SvrManager.Instance != null)
            {
                if (XRSupportUtil.IsXREnabled())
                {
                    return SvrManager.Instance.head.transform.localRotation;
                }
                else
                {
                    Debug.LogWarning("Cannot read XRNodeHeadLocalRotation as XR is not enabled");
                    return Quaternion.identity;
                }
            }
#endif
            InputTracking.GetNodeStates(nodeStates);
            Quaternion rotation;

            foreach (XRNodeState state in nodeStates)
            {
                if (state.nodeType == XRNode.Head &&
                    state.TryGetRotation(out rotation))
                {
                    return rotation;
                }
            }

            return Quaternion.identity;
        }

        public static Vector3 GetXRNodeLocalPosition(int node)
        {
#if SVR
            if (SvrManager.Instance != null)
            {
                Debug.LogWarning("Not implemented yet");
                return Vector3.zero;
            }
#endif
            InputTracking.GetNodeStates(nodeStates);
            Vector3 position;

            foreach (XRNodeState state in nodeStates)
            {
                if (state.nodeType == (XRNode)node &&
                   state.TryGetPosition(out position))
                {
                    return position;
                }
            }

            return Vector3.zero;
        }

        public static Quaternion GetXRNodeLocalRotation(int node)
        {
#if SVR
            if (SvrManager.Instance != null)
            {
                Debug.LogWarning("Not implemented yet");
                return Quaternion.identity;
            }
#endif
            InputTracking.GetNodeStates(nodeStates);
            Quaternion rotation;

            foreach (XRNodeState state in nodeStates)
            {
                if (state.nodeType == (XRNode)node &&
                    state.TryGetRotation(out rotation))
                {
                    return rotation;
                }
            }
            return Quaternion.identity;
        }

        public static void Recenter()
        {
#if SVR
            if (SvrInput.Instance != null)
            {
                SvrInput.Instance.HandleRecenter();
                return;
            }
#endif
            var devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, devices);
            if (devices.Count == 0)
                return;

            var hmdDevice = devices[0];
#if !UNITY_2020_1_OR_NEWER
            if (hmdDevice.subsystem != null)
            {
#endif
                hmdDevice.subsystem.TryRecenter();
#if !UNITY_2020_1_OR_NEWER
            }
            else
            {
#pragma warning disable 0618
                InputTracking.Recenter();
#pragma warning restore 0618
            }
#else
#pragma warning disable 0618
            InputTracking.Recenter();
#pragma warning restore 0618
#endif
        }

        public static float GetGPUTime()
        {
#if SVR
            if (SvrManager.Instance != null)
            {
                Debug.LogWarning("Not implemented for android");
                return 0;
            }
#endif

            float gpuTime = 0f;
            XRStats.TryGetGPUTimeLastFrame(out gpuTime);
            return gpuTime;
        }

        public static string GetLoadedDeviceName()
        {
#if SVR
            if (SvrManager.Instance != null)
            {
                return "XR2_Device";
            }
#endif

            return XRSettings.loadedDeviceName;
        }

        /// <summary> Returns whether there's a floor available. </summary>
        public static bool IsRoomScale()
        {
#if SVR
            if (SvrManager.Instance != null)
            {
                return true;
            }
#endif

            var devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, devices);

            if (devices.Count == 0)
            {
                return false;
            }

            var hmdDevice = devices[0];
#if !UNITY_2020_1_OR_NEWER

            if (hmdDevice.subsystem != null)
            {
#endif
                return hmdDevice.subsystem.GetTrackingOriginMode().HasFlag(TrackingOriginModeFlags.Floor);
#if !UNITY_2020_1_OR_NEWER
            }
            else
            {
#pragma warning disable 0618
                return XRDevice.GetTrackingSpaceType() == TrackingSpaceType.RoomScale;
#pragma warning restore 0618
            }
#endif
        }

        /// <summary> Returns whether the playspace is larger than 1m on its shortest side. </summary>
        public static bool IsLargePlayspace()
        {
#if SVR
            if (SvrManager.Instance != null)
            {
                // The current SVR support for reading the playspace dimensions is crude. Likely to be improved on by 3rd parties
                // By default, we should consider the current playspace as 'large'
                return true;
            }
#endif

#if UNITY_2020_1_OR_NEWER // Oculus reports a floor centered space now...
            var devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, devices);

            if (devices.Count == 0)
            {
                return false;
            }

            var hmdDevice = devices[0];
            hmdDevice.subsystem.TryGetBoundaryPoints(_boundaryPoints);
            Bounds playspaceSize = new Bounds();
            foreach (Vector3 boundaryPoint in _boundaryPoints)
            {
                playspaceSize.Encapsulate(boundaryPoint);
            }

            return playspaceSize.size.magnitude > 1f; // Playspace is greater than 1m on its shortest axis
#else
            return IsRoomScale();
#endif
        }
    }
}