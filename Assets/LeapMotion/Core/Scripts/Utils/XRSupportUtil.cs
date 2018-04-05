/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;

#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR;
#else
using UnityEngine.VR;
#endif

namespace Leap.Unity {

  /// <summary>
  /// Wraps various (but not all) "XR" calls with Unity 5.6-supporting "VR" calls
  /// via #ifdefs.
  /// </summary>
  public static class XRSupportUtil {

    public static bool IsXREnabled() {
      #if UNITY_2017_2_OR_NEWER
      return XRSettings.enabled;
      #else
      return VRSettings.enabled;
      #endif
    }

    public static bool IsXRDevicePresent() {
      #if UNITY_2017_2_OR_NEWER
      return XRDevice.isPresent;
      #else
      return VRDevice.isPresent;
      #endif
    }

    static bool outputPresenceWarning = false;
    public static bool IsUserPresent(bool defaultPresence = true) {
      #if UNITY_2017_2_OR_NEWER
      var userPresence = XRDevice.userPresence;
      if (userPresence == UserPresenceState.Present) {
        return true;
      } else if (!outputPresenceWarning && userPresence == UserPresenceState.Unsupported) {
        Debug.LogWarning("XR UserPresenceState unsupported (XR support is probably disabled).");
        outputPresenceWarning = true;
      }
      #else
      if (!outputPresenceWarning){
        Debug.LogWarning("XR UserPresenceState is only supported in 2017.2 and newer.");
        outputPresenceWarning = true;
      }
      #endif
      return defaultPresence;
    }

    public static Vector3 GetXRNodeCenterEyeLocalPosition() {
      #if UNITY_2017_2_OR_NEWER
      return InputTracking.GetLocalPosition(XRNode.CenterEye);
      #else
      return InputTracking.GetLocalPosition(VRNode.CenterEye);
      #endif
    }

    public static Quaternion GetXRNodeCenterEyeLocalRotation() {
      #if UNITY_2017_2_OR_NEWER
      return InputTracking.GetLocalRotation(XRNode.CenterEye);
      #else
      return InputTracking.GetLocalRotation(VRNode.CenterEye);
      #endif
    }

    public static Vector3 GetXRNodeHeadLocalPosition() {
      #if UNITY_2017_2_OR_NEWER
      return InputTracking.GetLocalPosition(XRNode.Head);
      #else
      return InputTracking.GetLocalPosition(VRNode.Head);
      #endif
    }

    public static Quaternion GetXRNodeHeadLocalRotation() {
      #if UNITY_2017_2_OR_NEWER
      return InputTracking.GetLocalRotation(XRNode.Head);
      #else
      return InputTracking.GetLocalRotation(VRNode.Head);
      #endif
    }

    public static void Recenter() {
      InputTracking.Recenter();
    }

    public static string GetLoadedDeviceName() {
      #if UNITY_2017_2_OR_NEWER
      return XRSettings.loadedDeviceName;
      #else
      return VRSettings.loadedDeviceName;
      #endif
    }

    public static bool IsRoomScale() {
      #if UNITY_2017_2_OR_NEWER
      return XRDevice.GetTrackingSpaceType() == TrackingSpaceType.RoomScale;
      #else
      return VRDevice.GetTrackingSpaceType() == TrackingSpaceType.RoomScale;
      #endif
    }

    public static float GetGPUTime() {
      float gpuTime = 0f;
      #if UNITY_5_6_OR_NEWER
      #if UNITY_2017_2_OR_NEWER
      UnityEngine.XR.XRStats.TryGetGPUTimeLastFrame(out gpuTime);
      #else
      UnityEngine.VR.VRStats.TryGetGPUTimeLastFrame(out gpuTime);
      #endif
      #else
      gpuTime = UnityEngine.VR.VRStats.gpuTimeLastFrame;
      #endif
      return gpuTime;
    }

  }

}
