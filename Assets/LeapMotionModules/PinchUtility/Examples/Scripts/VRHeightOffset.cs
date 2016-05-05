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
    _deviceOffsets = new DeviceHeightPair[2];
    _deviceOffsets[0] = new DeviceHeightPair("Oculus", -0.1f);
    _deviceOffsets[1] = new DeviceHeightPair("Vive", 1.0f);
  }

  void Start() {
    if (VRDevice.isPresent && VRSettings.enabled && _deviceOffsets != null) {
      string deviceName = VRSettings.loadedDeviceName;
      var deviceHeightPair = _deviceOffsets.FirstOrDefault(d => d.deviceName == deviceName);
      if (deviceHeightPair != null) {
        transform.Translate(Vector3.up * deviceHeightPair.heightOffset);
      }
    }
  }

}
