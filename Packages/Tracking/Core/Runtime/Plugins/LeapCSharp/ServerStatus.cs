/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2026.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

namespace LeapInternal
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;
#if UNITY_EDITOR
    using UnityEditor;
#endif

    public static class ServerStatus
    {
        private static class ServerStatusChecker
        {
            public static LeapC.LEAP_SERVER_STATUS LastStatus
            {
                get
                {
                    GetStatus();
                    LeapC.LEAP_SERVER_STATUS status;
                    lock (lockObject)
                        status = lastStatus;
                    return status;
                }
            }
            static LeapC.LEAP_SERVER_STATUS lastStatus;

            public static LeapC.LEAP_SERVER_STATUS_DEVICE[] LastDevices
            {
                get
                {
                    GetStatus();
                    LeapC.LEAP_SERVER_STATUS_DEVICE[] devices;
                    lock (lockObject)
                        devices = lastDevices;
                    return devices;
                }
            }
            static LeapC.LEAP_SERVER_STATUS_DEVICE[] lastDevices;

            static readonly object lockObject = new object();
            static int isCheckingStatus = 0;
            static CancellationTokenSource cancellation;
            static Thread statusThread;

            private static void GetStatus()
            {
                // Start the poller exactly once, even under concurrent first-callers.
                if (Interlocked.Exchange(ref isCheckingStatus, 1) != 0)
                    return;

                cancellation = new CancellationTokenSource();
                UnityEngine.Application.quitting += Stop;
#if UNITY_EDITOR
                AssemblyReloadEvents.beforeAssemblyReload += Stop;
#endif

                UpdateStatus();   // prime the cache synchronously for the first caller

                statusThread = new Thread(StatusLoop) { IsBackground = true, Name = "LeapC ServerStatus" };
                statusThread.Start();
            }

            private static void StatusLoop()
            {
                CancellationToken token = cancellation.Token;
                while (!token.IsCancellationRequested)
                {
                    UpdateStatus();
                    token.WaitHandle.WaitOne(10000);   // 10s poll, wakes immediately on Stop()
                }
            }

            private static void Stop()
            {
                // Flip the flag back (allows a later restart); only one Stop proceeds.
                if (Interlocked.Exchange(ref isCheckingStatus, 0) == 0)
                    return;

                UnityEngine.Application.quitting -= Stop;
#if UNITY_EDITOR
                AssemblyReloadEvents.beforeAssemblyReload -= Stop;
#endif
                cancellation.Cancel();
                statusThread.Join(2000);
                cancellation.Dispose();
                cancellation = null;
            }

            private static void UpdateStatus()
            {
                IntPtr statusPtr = new IntPtr();
                // Best-effort refresh: the eLeapRS return is ignored on purpose. A null status
                // pointer already signals failure (service down / timeout / bad data), so we
                // just keep the last known status.
                LeapC.GetServerStatus(1500, ref statusPtr);

                if (statusPtr != IntPtr.Zero)
                {
                    var status = Marshal.PtrToStructure<LeapC.LEAP_SERVER_STATUS>(statusPtr);
                    LeapC.LEAP_SERVER_STATUS_DEVICE[] devices;
                    MarshalUnmananagedArray2Struct(status.devices, (int)status.device_count, out devices);

                    lock (lockObject)
                    {
                        lastStatus = status;
                        lastDevices = devices;
                    }

                    LeapC.ReleaseServerStatus(ref status);
                }
            }

            private static void MarshalUnmananagedArray2Struct<T>(IntPtr unmanagedArray, int length, out T[] mangagedArray)
            {
                var size = Marshal.SizeOf(typeof(T));
                mangagedArray = new T[length];

                for (int i = 0; i < length; i++)
                {
                    IntPtr ins = new IntPtr(unmanagedArray.ToInt64() + i * size);
                    mangagedArray[i] = Marshal.PtrToStructure<T>(ins);
                }
            }
        }

        public static bool IsServiceVersionValid(LEAP_VERSION _requiredVersion)
        {
            if (ServerStatusChecker.LastStatus.version != null)
            {
                string[] versions = ServerStatusChecker.LastStatus.version.Split('v')[1].Split('-')[0].Split('.');
                LEAP_VERSION curVersion = new LEAP_VERSION { major = int.Parse(versions[0]), minor = int.Parse(versions[1]), patch = int.Parse(versions[2]) };

                if (curVersion.major > _requiredVersion.major)
                    return true;
                if (curVersion.major < _requiredVersion.major)
                    return false;

                if (curVersion.minor > _requiredVersion.minor)
                    return true;
                if (curVersion.minor < _requiredVersion.minor)
                    return false;

                if (curVersion.patch >= _requiredVersion.patch)
                    return true;

                return false;
            }

            return true;
        }

        public static string[] GetSerialNumbers()
        {
            string[] serials = new string[0];
            if (ServerStatusChecker.LastDevices != null)
            {
                serials = new string[ServerStatusChecker.LastDevices.Length];
                for (int i = 0; i < ServerStatusChecker.LastDevices.Length; i++)
                {
                    serials[i] = ServerStatusChecker.LastDevices[i].serial;
                }
            }

            return serials;
        }

        public static string GetDeviceType(string _serial)
        {
            if (ServerStatusChecker.LastDevices != null)
            {
                for (int i = 0; i < ServerStatusChecker.LastDevices.Length; i++)
                {
                    if (_serial == "" || _serial == ServerStatusChecker.LastDevices[i].serial)
                    {
                        return ServerStatusChecker.LastDevices[i].type;
                    }
                }
            }

            return "";
        }
    }
}