/******************************************************************************\
* Copyright (C) 2012-2016 Leap Motion, Inc. All rights reserved.               *
* Leap Motion proprietary and confidential. Not for distribution.              *
* Use subject to the terms of the Leap Motion SDK Agreement available at       *
* https://developer.leapmotion.com/sdk_agreement, or another agreement         *
* between Leap Motion and you, your company or other organization.             *
\******************************************************************************/
namespace Leap
{

  using System;
  using System.Runtime.InteropServices;

  /**
   * The FailedDevice class provides information about Leap Motion hardware that
   * has been physically connected to the client computer, but is not operating
   * correctly.
   *
   * Failed devices do not provide any tracking data and do not show up in the
   * Controller:devices() list.
   *
   * Get the list of failed devices using Controller::failedDevices().
   *
   * \include FailedDevice_class.txt
   *
   * @since 3.0
   */

  //TODO Implement FailedDevices
  public class FailedDevice:
    IEquatable<FailedDevice>
  {
    public FailedDevice() {
      Failure = FailureType.FAIL_UNKNOWN;
      PnpId = "0";
    }

    /**
     * Test FailedDevice equality.
     * True if the devices are the same.
     * @since 3.0
     */
    public bool Equals(FailedDevice other)
    {
      return this.PnpId == other.PnpId;
    }

    /**
        * The device plug-and-play id string.
        * @since 3.0
        */
    public string PnpId { get; private set; }

    /**
     * The reason for device failure.
     *
     * The failure reasons are defined as members of the FailureType enumeration:
     *
     * **FailureType::FAIL_UNKNOWN**  The cause of the error is unknown.
     *
     * **FailureType::FAIL_CALIBRATION** The device has a bad calibration record.
     *
     * **FailureType::FAIL_FIRMWARE** The device firmware is corrupt or failed to update.
     *
     * **FailureType::FAIL_TRANSPORT** The device is unresponsive.
     *
     * **FailureType::FAIL_CONTROL** The service cannot establish the required USB control interfaces.
     *
     * **FailureType::FAIL_COUNT** Not currently used.
     *
     * @since 3.0
     */
    public FailedDevice.FailureType Failure { get; private set; }

    /**
     * The errors that can cause a device to fail to properly connect to the service.
     *
     * @since 3.0
     */
    public enum FailureType
    {
      /** The cause of the error is unknown.
       * @since 3.0
       */
      FAIL_UNKNOWN,
      /** The device has a bad calibration record.
       * @since 3.0
       */
      FAIL_CALIBRATION,
      /** The device firmware is corrupt or failed to update.
       * @since 3.0
       */
      FAIL_FIRMWARE,
      /** The device is unresponsive.
       * @since 3.0
       */
      FAIL_TRANSPORT,
      /** The service cannot establish the required USB control interfaces.
       * @since 3.0
       */
      FAIL_CONTROL,
      /** Not currently used.
       * @since 3.0
       */
      FAIL_COUNT
    }
  }
}
