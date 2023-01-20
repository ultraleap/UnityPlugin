/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

#if !UNITY_EDITOR_LINUX

using LeapInternal;
using NUnit.Framework;
using System;

namespace Leap.LeapCSharp.Tests
{
    public class LeapCStressTests
    {
        [Test]
        public void TestCreateDestroy()
        {
            IntPtr connHandle = IntPtr.Zero;
            int iterations = 5000;
            for (int i = 0; i < iterations; i++)
            {
                //LEAP_CONNECTION_MESSAGE msg  = new LEAP_CONNECTION_MESSAGE();
                LeapC.CreateConnection(out connHandle);
                LeapC.OpenConnection(connHandle);
                LeapC.DestroyConnection(connHandle);
            }
        }

        [Test]
        public void TestCreateDestroyWithConfigRequest()
        {
            IntPtr connHandle = IntPtr.Zero;
            int iterations = 5000;
            uint requestId;
            for (int i = 0; i < iterations; i++)
            {
                //LEAP_CONNECTION_MESSAGE msg  = new LEAP_CONNECTION_MESSAGE();
                LeapC.CreateConnection(out connHandle);
                LeapC.OpenConnection(connHandle);
                LeapC.RequestConfigValue(connHandle, "tracking_version", out requestId);
                LeapC.DestroyConnection(connHandle);
            }
        }
    }
}

#endif