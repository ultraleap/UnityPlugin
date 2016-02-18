/******************************************************************************\
* Copyright (C) 2012-2016 Leap Motion, Inc. All rights reserved.               *
* Leap Motion proprietary and confidential. Not for distribution.              *
* Use subject to the terms of the Leap Motion SDK Agreement available at       *
* https://developer.leapmotion.com/sdk_agreement, or another agreement         *
* between Leap Motion and you, your company or other organization.             *
\******************************************************************************/
namespace Leap {

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
  * @since 2.4.0
  */

    //TODO Implement FailedDevices
public class FailedDevice{
        bool _isValid = false;
        string _pnpId = "0";
        FailureType _failureType = FailureType.FAIL_UNKNOWN;

  public FailedDevice(){}


   /**
    * Reports whether this FailedDevice object contains valid data.
    * An invalid FailedDevice does not represent a physical device and can
    * be the result of creating a new FailedDevice object with the constructor.
    * Get FailedDevice objects from Controller::failedDevices() only.
    * @since 2.4.0
    */
  public bool IsValid {
     get{
           return _isValid;
     }
  }

   /**
    * An invalid FailedDevice object.
    *
    * @since 2.4.0
    */
  public static FailedDevice Invalid() {
    return new FailedDevice();
  }

   /**
    * Test FailedDevice equality.
    * True if the devices are the same.
    * @since 2.4.0
    */
  public bool Equals(FailedDevice other) {
    return this.IsValid && other.IsValid && this.PnpId == other.PnpId;
  }

/**
    * The device plug-and-play id string.
    * @since 2.4.0
    */  public string PnpId {
    get {
      return _pnpId;
    } 
  }

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
    * @since 2.4.0
    */  public FailedDevice.FailureType Failure {
    get {
      return _failureType;
    } 
  }

     /**
      * The errors that can cause a device to fail to properly connect to the service.
      *
      * @since 2.4.0
      */
  public enum FailureType {
       /** The cause of the error is unknown.
        * @since 2.4.0
        */
    FAIL_UNKNOWN,
       /** The device has a bad calibration record.
        * @since 2.4.0
        */
    FAIL_CALIBRATION,
       /** The device firmware is corrupt or failed to update.
        * @since 2.4.0
        */
    FAIL_FIRMWARE,
       /** The device is unresponsive.
        * @since 2.4.0
        */
    FAIL_TRANSPORT,
       /** The service cannot establish the required USB control interfaces.
        * @since 2.4.0
        */
    FAIL_CONTROL,
       /** Not currently used.
        * @since 2.4.0
        */
    FAIL_COUNT
  }

}

}
