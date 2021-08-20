/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

namespace Leap {
  using System;

  /// <summary>
  /// The FailedDevice class provides information about Leap Motion hardware that
  /// has been physically connected to the client computer, but is not operating
  /// correctly.
  /// 
  /// Failed devices do not provide any tracking data and do not show up in the
  /// Controller.Devices() list.
  /// 
  /// Get the list of failed devices using Controller.FailedDevices().
  /// 
  /// @since 3.0
  /// </summary>
  public class FailedDevice :
    IEquatable<FailedDevice> {

    public FailedDevice() {
      Failure = FailureType.FAIL_UNKNOWN;
      PnpId = "0";
    }

    /// <summary>
    /// Test FailedDevice equality.
    /// True if the devices are the same.
    /// @since 3.0
    /// </summary>
    public bool Equals(FailedDevice other) {
      return PnpId == other.PnpId;
    }

    /// <summary>
    /// The device plug-and-play id string.
    /// @since 3.0
    /// </summary>
    public string PnpId { get; private set; }

    /// <summary>
    /// The reason for device failure.
    /// The failure reasons are defined as members of the FailureType enumeration.
    /// 
    /// @since 3.0
    /// </summary>
    public FailureType Failure { get; private set; }

    /// <summary>
    /// The errors that can cause a device to fail to properly connect to the service.
    /// 
    /// @since 3.0
    /// </summary>
    public enum FailureType {
      /// <summary>
      /// The cause of the error is unknown.
      /// </summary>
      FAIL_UNKNOWN,
      /// <summary>
      /// The device has a bad calibration record.
      /// </summary>
      FAIL_CALIBRATION,
      /// <summary>
      /// The device firmware is corrupt or failed to update.
      /// </summary>
      FAIL_FIRMWARE,
      /// <summary>
      /// The device is unresponsive.
      /// </summary>
      FAIL_TRANSPORT,
      /// <summary>
      /// The service cannot establish the required USB control interfaces.
      /// </summary>
      FAIL_CONTROl
    }
  }
}
