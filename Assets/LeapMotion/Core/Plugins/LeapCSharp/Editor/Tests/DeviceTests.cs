/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using NUnit.Framework;
using System;

namespace Leap.LeapCSharp.Tests {
  [TestFixture()]
  public class DeviceTests {
    Controller controller;

    [OneTimeSetUp]
    public void Init() {
      controller = new Controller();
      System.Threading.Thread.Sleep(500);
    }

    [Test]
    public void DeviceIsConnected() {
      Assert.True(controller.IsConnected,
        "A Leap device must be connected to successfully test LeapCSharp.");
    }

    [Test()]
    public void Device_operator_equals() {
      Device thisDevice = new Device();
      Device thatDevice = new Device();
      // !!!Device_operator_equals
      Boolean isEqual = thisDevice == thatDevice;
      // !!!END
      Assert.False(isEqual);

    }

    [Test()]
    public void DeviceList_operator_index() {
      // !!!DeviceList_operator_index
      DeviceList allDevices = controller.Devices;
      for (int index = 0; index < allDevices.Count; index++) {
        Console.WriteLine(allDevices[index]);
      }
      // !!!END

    }

    [Test()]
    public void DeviceList_isEmpty() {
      // !!!DeviceList_isEmpty
      if (!controller.Devices.IsEmpty) {
        Device leapDevice = controller.Devices[0];
      }
      // !!!END
    }
  }
}

