/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

namespace Leap {
  using System.Collections.Generic;

  /// <summary>
  /// The list of FailedDevice objects contains an entry for every failed Leap Motion
  /// hardware device connected to the client computer. FailedDevice objects report
  /// the device pnpID string and reason for failure.
  /// 
  /// Get the list of FailedDevice objects from Controller.FailedDevices().
  /// 
  /// @since 3.0
  /// </summary>
  public class FailedDeviceList : List<FailedDevice> {

    /// <summary>
    /// Constructs an empty list.
    /// </summary>
    public FailedDeviceList() { }

    /// <summary>
    /// Appends the contents of another FailedDeviceList to this one.
    /// </summary>
    public FailedDeviceList Append(FailedDeviceList other) {
      AddRange(other);
      return this;
    }

    /// <summary>
    /// Reports whether the list is empty.
    /// </summary>
    public bool IsEmpty {
      get { return Count == 0; }
    }
  }
}
