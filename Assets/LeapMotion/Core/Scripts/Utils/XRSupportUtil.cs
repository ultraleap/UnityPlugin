using UnityEngine;

#if UNITY_5
using UnityEngine.VR;
#else
using UnityEngine.XR;
#endif

namespace Leap.Unity {

  /// <summary>
  /// Wraps various (but not all) "XR" calls with Unity 5.6-supporting "VR" calls
  /// via #ifdefs.
  /// </summary>
  public static class XRSupportUtil {

    public static bool IsXREnabled() {
      #if UNITY_5
      return VRSettings.enabled;
      #else
      return XRSettings.enabled;
      #endif
    }

    public static bool IsXRDevicePresent() {
      #if UNITY_5
      return VRDevice.isPresent;
      #else
      return XRDevice.isPresent;
      #endif
    }

    public static Vector3 GetXRNodeCenterEyeLocalPosition() {
      #if UNITY_5
      return InputTracking.GetLocalPosition(UnityEngine.VR.VRNode.CenterEye);
      #else
      return InputTracking.GetLocalPosition(XRNode.CenterEye);
      #endif
    }

    public static Quaternion GetXRNodeCenterEyeLocalRotation() {
      #if UNITY_5
      return InputTracking.GetLocalRotation(VRNode.CenterEye);
      #else
      return InputTracking.GetLocalRotation(XRNode.CenterEye);
      #endif
    }

    public static Vector3 GetXRNodeHeadLocalPosition() {
      #if UNITY_5
      return InputTracking.GetLocalPosition(VRNode.Head);
      #else
      return InputTracking.GetLocalPosition(XRNode.Head);
      #endif
    }

    public static Quaternion GetXRNodeHeadLocalRotation() {
      #if UNITY_5
      return InputTracking.GetLocalRotation(VRNode.Head);
      #else
      return InputTracking.GetLocalRotation(XRNode.Head);
      #endif
    }

    public static void Recenter() {
      InputTracking.Recenter();
    }

    public static string GetLoadedDeviceName() {
      #if UNITY_5
      return VRSettings.loadedDeviceName;
      #else
      return XRSettings.loadedDeviceName;
      #endif
    }

  }

}