/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using UnityEngine.VR;
using System;
using System.Linq;

public class VRHeightOffset : MonoBehaviour {

  [Serializable]
  public class DeviceHeightPair {
    public string DeviceName;
    public float HeightOffset;

    public DeviceHeightPair(string deviceName, float heightOffset) {
      DeviceName = deviceName;
      HeightOffset = heightOffset;
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
      var deviceHeightPair = _deviceOffsets.FirstOrDefault(d => deviceName.ToLower().Contains(d.DeviceName.ToLower()));
      if (deviceHeightPair != null) {
        transform.Translate(Vector3.up * deviceHeightPair.HeightOffset);
      }
    }
  }
}
