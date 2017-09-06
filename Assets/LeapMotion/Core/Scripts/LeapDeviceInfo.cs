/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity{
  
  /// <summary>
  /// Leap device info struct.
  /// </summary>
  public struct LeapDeviceInfo {

    public bool isEmbedded;

    /// <summary>
    /// Distance between focal points of cameras in meters.
    /// </summary>
    public float baseline;
  

    // TODO: LeapDeviceInfo.forwardOffset is no longer used for Temporal Warping as
    // of 9/1/17. To be removed / refactored as part of the Collapsing the Rig Hierarchy.

    /// <summary>
    /// The distance on the camera-facing axis from the tracked headset position to the
    /// focal point of the Leap cameras. In Leap space, cameras face upward, Vector3.up!
    /// </summary>
    public float forwardOffset;

    /// <summary>
    /// The field of view in degrees along the baseline axis.
    /// </summary>
    public float horizontalViewAngle;

    /// <summary>
    /// The field of view in degress perpendicular to the baseline axis.
    /// </summary>
    public float verticalViewAngle;

    /// <summary>
    /// The maximum radius for reliable tracking in meters.
    /// </summary>
    public float trackingRange;

    /// <summary>
    /// A unique alphanumeric device hardware ID.
    /// </summary>
    public string serialID;
    
    public static LeapDeviceInfo GetLeapDeviceInfo() {

      LeapDeviceInfo deviceInfo;
      deviceInfo.isEmbedded = false;
      deviceInfo.baseline = 0.04f;
      deviceInfo.forwardOffset = 0.12F;
      deviceInfo.horizontalViewAngle = 2.303835f * Mathf.Rad2Deg;
      deviceInfo.verticalViewAngle = 2.007129f * Mathf.Rad2Deg;
      deviceInfo.trackingRange = 470f / 1000f;
      deviceInfo.serialID = "";

      return deviceInfo;
    }
  }
}
