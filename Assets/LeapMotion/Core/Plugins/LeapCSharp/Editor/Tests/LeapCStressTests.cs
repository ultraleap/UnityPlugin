using System;
using System.Threading;
using System.Runtime.InteropServices;

using NUnit.Framework;
using LeapInternal;
using Leap;

namespace Leap.Tests {
    [TestFixture()]
    public class LeapCStressTests
    {
        [Test()]
        public void TestCreateDestroy ()
        {
          IntPtr connHandle = IntPtr.Zero;
          int iterations = 5000;
          for(int i = 0; i < iterations; i++){
            //LEAP_CONNECTION_MESSAGE msg  = new LEAP_CONNECTION_MESSAGE();
            LeapC.CreateConnection(out connHandle);
            LeapC.OpenConnection(connHandle);
            LeapC.DestroyConnection(connHandle);
          }
        }

        [Test()]
        public void TestCreateDestroyWithConfigRequest ()
        {
          IntPtr connHandle = IntPtr.Zero;
          int iterations = 5000;
          uint requestId;
          for(int i = 0; i < iterations; i++){
            //LEAP_CONNECTION_MESSAGE msg  = new LEAP_CONNECTION_MESSAGE();
            LeapC.CreateConnection(out connHandle);
            LeapC.OpenConnection(connHandle);
            LeapC.RequestConfigValue(connHandle, "tracking_version", out requestId);
            LeapC.DestroyConnection(connHandle);
          }
        }
    }
}
