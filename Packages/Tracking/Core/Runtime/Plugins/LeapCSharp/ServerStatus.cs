/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

namespace LeapInternal
{
    using System;
    using System.Linq.Expressions;
    using System.Runtime.InteropServices;
    using System.Threading;
    using UnityEngine;

    public static class ServerStatus
    {
        static LeapC.LEAP_SERVER_STATUS lastStatus;
        static LeapC.LEAP_SERVER_STATUS_DEVICE[] lastDevices;

        static readonly object lockObject = new object();
        static bool isCheckingStatus = false;

        public static void GetStatus()
        {
            if (isCheckingStatus)
                return;

            Thread thread = new Thread(() =>
            {
                isCheckingStatus = true;
                while (true)
                {
                    UpdateStatus();
                    Thread.Sleep(10000);
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }

        private static void UpdateStatus()
        {
            IntPtr statusPtr = new IntPtr();
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

        public static bool IsServiceVersionValid(LEAP_VERSION _requiredVersion)
        {
            GetStatus();

            if (lastStatus.version != null)
            {
                string[] versions = lastStatus.version.Split('v')[1].Split('-')[0].Split('.');
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
            GetStatus();

            string[] serials = new string[0];
            if (lastDevices != null)
            {
                serials = new string[lastDevices.Length];
                for (int i = 0; i < lastDevices.Length; i++)
                {
                    serials[i] = lastDevices[i].serial;
                }
            }

            return serials;
        }

        public static string GetDeviceType(string _serial)
        {
            GetStatus();

            if (lastDevices != null)
            {
                for (int i = 0; i < lastDevices.Length; i++)
                {
                    if (_serial == "" || _serial == lastDevices[i].serial)
                    {
                        return lastDevices[i].type;
                    }
                }
            }

            return "";
        }

        static void MarshalUnmananagedArray2Struct<T>(IntPtr unmanagedArray, int length, out T[] mangagedArray)
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
}