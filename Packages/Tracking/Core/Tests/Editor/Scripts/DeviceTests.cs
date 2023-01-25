/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

#if !UNITY_EDITOR_LINUX

using NUnit.Framework;
using System;

namespace Leap.LeapCSharp.Tests
{
    public class DeviceTests
    {
        Controller controller;

        [OneTimeSetUp]
        public void Init()
        {
            controller = new Controller();
            System.Threading.Thread.Sleep(500);
        }

        [Test]
        public void DeviceIsConnected()
        {
            Assert.True(controller.IsConnected,
              "A Leap device must be connected to successfully test LeapCSharp.");
        }

        [Test]
        public void Device_operator_equals()
        {
            Device thisDevice = new Device();
            Device thatDevice = new Device();
            // !!!Device_operator_equals
            Boolean isEqual = thisDevice == thatDevice;
            // !!!END
            Assert.False(isEqual);
        }

        [Test]
        public void DeviceList_operator_index()
        {
            // !!!DeviceList_operator_index
            DeviceList allDevices = controller.Devices;
            for (int index = 0; index < allDevices.Count; index++)
            {
                Console.WriteLine(allDevices[index]);
            }
            // !!!END

        }

        [Test]
        public void DeviceList_isEmpty()
        {
            // !!!DeviceList_isEmpty
            if (!controller.Devices.IsEmpty)
            {
                Device leapDevice = controller.Devices[0];
            }
            // !!!END
        }
    }
}

#endif