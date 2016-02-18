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
    using System.Collections.Generic;

 /**
  * The list of FailedDevice objects contains an entry for every failed Leap Motion
  * hardware device connected to the client computer. FailedDevice objects report
  * the device pnpID string and reason for failure.
  *
  * Get the list of FailedDevice objects from Controller::failedDevices().
  *
  * @since 2.4.0
  */

public class FailedDeviceList : List<FailedDevice>{

   /**
    * Constructs an empty list.
    * @since 2.4.0
    */
  public FailedDeviceList() {
  }

   /**
    * Appends the contents of another FailedDeviceList to this one.
    * @since 2.4.0
    */
  public FailedDeviceList Append(FailedDeviceList other) {
    this.AddRange(other);
    return this;
  }

/**
    * Reports whether the list is empty.
    * @since 2.4.0
    */  public bool IsEmpty {
    get {
      return this.Count == 0;
    } 
  }

}

}
