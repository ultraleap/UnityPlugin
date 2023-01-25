/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

namespace Leap
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// The DeviceList class represents a list of Device objects.
    /// 
    /// Get a DeviceList object by calling Controller.Devices().
    /// @since 1.0
    /// </summary>
    public class DeviceList :
      List<Device>
    {

        /// <summary>
        /// Constructs an empty list of devices.
        /// @since 1.0
        /// </summary>
        public DeviceList() { }

        /// <summary>
        /// For internal use only.
        /// </summary>
        public Device FindDeviceByHandle(IntPtr deviceHandle)
        {
            for (int d = 0; d < this.Count; d++)
            {
                if (this[d].Handle == deviceHandle)
                    return this[d];
            }
            return null;
        }

        /// <summary>
        /// For internal use only.
        /// </summary>
        public Device FindDeviceByID(uint deviceID)
        {
            for (int d = 0; d < this.Count; d++)
            {
                if (this[d].DeviceID == deviceID)
                    return this[d];
            }
            return null;
        }

        /// <summary>
        /// The devices that are currently streaming tracking data.
        /// If no streaming devices are found, returns null
        /// </summary>
        public IEnumerable<Device> ActiveDevices
        {
            get
            {
                for (int d = 0; d < Count; d++)
                {
                    this[d].UpdateStatus(LeapInternal.eLeapDeviceStatus.eLeapDeviceStatus_Streaming);
                }

                return this.Where(d => d.IsStreaming);
            }
        }

        /// <summary>
        /// For internal use only.
        /// </summary>
        public void AddOrUpdate(Device device)
        {
            Device existingDevice = FindDeviceByHandle(device.Handle);
            if (existingDevice != null)
            {
                existingDevice.Update(device);
            }
            else
            {
                Add(device);
            }
        }

        /// <summary>
        /// Reports whether the list is empty.
        /// @since 1.0
        /// </summary>
        public bool IsEmpty
        {
            get { return Count == 0; }
        }
    }
}