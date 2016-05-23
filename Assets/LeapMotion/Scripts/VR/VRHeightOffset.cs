using UnityEngine;
using UnityEngine.VR;
using System;
using System.Linq;

public class VRHeightOffset : MonoBehaviour {

  [Serializable]
  public class DeviceHeightPair {
    public string deviceName;
    public float heightOffset;

    public DeviceHeightPair(string deviceName, float heightOffset) {
      this.deviceName = deviceName;
      this.heightOffset = heightOffset;
    }
  }

  public DeviceHeightPair[] _deviceOffsets;

  void Reset() {
    _deviceOffsets = new DeviceHeightPair[1];
    _deviceOffsets[0] = new DeviceHeightPair("oculus", 1f);
  }

  void Start() {
    if (VRDevice.isPresent && VRSettings.enabled && _deviceOffsets != null) {
#if UNITY_5_4_OR_NEWER
      string deviceName = VRSettings.loadedDeviceName;
#else
      string deviceName = VRDevice.family;
#endif
      var deviceHeightPair = _deviceOffsets.FirstOrDefault(d => deviceName.ToLower().Contains(d.deviceName.ToLower()));
      if (deviceHeightPair != null) {
        transform.Translate(Vector3.up * deviceHeightPair.heightOffset);
      }
    }
  }
}
