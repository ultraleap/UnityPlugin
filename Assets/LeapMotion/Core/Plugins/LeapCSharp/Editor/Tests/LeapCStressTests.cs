/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using NUnit.Framework;
using LeapInternal;

namespace Leap.LeapCSharp.Tests {
  [TestFixture()]
  public class LeapCStressTests {
    [Test()]
    public void TestCreateDestroy() {
      IntPtr connHandle = IntPtr.Zero;
      int iterations = 5000;
      for (int i = 0; i < iterations; i++) {
        //LEAP_CONNECTION_MESSAGE msg  = new LEAP_CONNECTION_MESSAGE();
        LeapC.CreateConnection(out connHandle);
        LeapC.OpenConnection(connHandle);
        LeapC.DestroyConnection(connHandle);
      }
    }

    [Test()]
    public void TestCreateDestroyWithConfigRequest() {
      IntPtr connHandle = IntPtr.Zero;
      int iterations = 5000;
      uint requestId;
      for (int i = 0; i < iterations; i++) {
        //LEAP_CONNECTION_MESSAGE msg  = new LEAP_CONNECTION_MESSAGE();
        LeapC.CreateConnection(out connHandle);
        LeapC.OpenConnection(connHandle);
        LeapC.RequestConfigValue(connHandle, "tracking_version", out requestId);
        LeapC.DestroyConnection(connHandle);
      }
    }
  }
}
