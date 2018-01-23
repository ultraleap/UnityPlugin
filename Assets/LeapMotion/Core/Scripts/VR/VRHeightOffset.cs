/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using System;
using System.Linq;
using Leap.Unity;

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

  public KeyCode moveUpKey = KeyCode.None;
  public KeyCode moveDownKey = KeyCode.None;
  public float stepSize = 0.1f;

  void Reset() {
    _deviceOffsets = new DeviceHeightPair[1];
    _deviceOffsets[0] = new DeviceHeightPair("oculus", 1f);
  }

  void Start() {
    if (XRSupportUtil.IsXRDevicePresent()
        && XRSupportUtil.IsXREnabled()
        && _deviceOffsets != null) {
      string deviceName = XRSupportUtil.GetLoadedDeviceName();
      var deviceHeightPair = _deviceOffsets.FirstOrDefault(d => deviceName.ToLower().Contains(d.DeviceName.ToLower()));
      if (deviceHeightPair != null) {
        transform.Translate(Vector3.up * deviceHeightPair.HeightOffset);
      }
    }
  }

  private void Update() {
    if (Input.GetKeyDown(moveUpKey)) {
      transform.Translate(Vector3.up * stepSize);
    }

    if (Input.GetKeyDown(moveDownKey)) {
      transform.Translate(Vector3.down * stepSize);
    }
  }
}
