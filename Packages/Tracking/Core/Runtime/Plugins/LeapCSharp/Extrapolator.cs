/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap;
using System;
using System.Runtime.InteropServices;

// ReSharper disable once CheckNamespace
namespace LeapInternal
{
    public class Extrapolator : IDisposable
    {
        private readonly IntPtr _handle;

        public Extrapolator()
        {
            if (Native.LeapCreateExtrapolator(out _handle) is not eLeapRS.eLeapRS_Success)
            {
                throw new Exception("Failed to construct a Leap Extrapolator");
            }
        }

        public eLeapRS SetDeviceTransform(float[] deviceTransform) 
        {
            if (deviceTransform.Length != 16)
            {
                throw new ArgumentException("Device transform must be a array of 16 floats");
            }
            return Native.LeapExtrapolatorSetDeviceTransform(_handle, deviceTransform);
        }

        public void AddTrackingFrame(LEAP_TRACKING_EVENT trackingEvent)
        {
            if (Native.LeapExtrapolatorAddTrackingFrame(_handle, trackingEvent) is not eLeapRS.eLeapRS_Success)
            {
                throw new Exception("Failed to add tracking frame");
            }
        }


        public void AddHeadPose(LEAP_HEAD_POSE_EVENT headPoseEvent)
        {
            if (Native.LeapExtrapolatorAddHeadPose(_handle, headPoseEvent) is not eLeapRS.eLeapRS_Success)
            {
                throw new Exception("Failed to add head pose");
            }
        }


        private ulong GetFrameSize(long timestamp)
        {
            eLeapRS result = Native.LeapExtrapolatorGetFrameSize(_handle, timestamp, out ulong numBytes);

            if (result is eLeapRS.eLeapRS_TimestampTooEarly or eLeapRS.eLeapRS_RoutineIsNotSeer)
            {
                throw new ArgumentOutOfRangeException(nameof(timestamp), "Timestamp was invalid");
            }
            
            if (result is not eLeapRS.eLeapRS_Success)
            {
                throw new Exception("Failed to retrieve extrapolated frame size");
            }

            return numBytes;
        }

        public Frame GetExtrapolatedFrame(long timestamp)
        {
            var size = GetFrameSize(timestamp);
            var trackingBuffer = Marshal.AllocHGlobal((int)size);
            eLeapRS result = Native.LeapExtrapolatorGetExtrapolatedFrame(_handle, timestamp, trackingBuffer, size);

            if (result is not eLeapRS.eLeapRS_Success)
            {
                throw new Exception("Failed to extrapolate frame");
            }

            Frame frame = new Frame();
            StructMarshal<LEAP_TRACKING_EVENT>.PtrToStruct(trackingBuffer, out LEAP_TRACKING_EVENT trackingEvent);
            frame.CopyFrom(ref trackingEvent);
            Marshal.FreeHGlobal(trackingBuffer);

            return frame;
        }

    private void ReleaseUnmanagedResources()
        {
            Native.LeapDestroyExtrapolator(_handle);
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~Extrapolator()
        {
            ReleaseUnmanagedResources();
        }
        
        internal class Native
        {
            [DllImport("LeapCExtrapolate", EntryPoint = "LeapCreateExtrapolator")]
            internal static extern eLeapRS LeapCreateExtrapolator(out IntPtr phExtrapolator);
            
            [DllImport("LeapCExtrapolate", EntryPoint = "LeapDestroyExtrapolator")]
            internal static extern void LeapDestroyExtrapolator(IntPtr hExtrapolator);

            [DllImport("LeapCExtrapolate", EntryPoint = "LeapExtrapolatorSetDeviceTransform")]
            internal static extern eLeapRS LeapExtrapolatorSetDeviceTransform(IntPtr hExtrapolator, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)] float[] transform);

            [DllImport("LeapCExtrapolate", EntryPoint = "LeapExtrapolatorAddTrackingFrame")]
            internal static extern eLeapRS LeapExtrapolatorAddTrackingFrame(IntPtr hExtrapolator, LEAP_TRACKING_EVENT pTrackingEvent);
            
            [DllImport("LeapCExtrapolate", EntryPoint = "LeapExtrapolatorAddHeadPose")]
            internal static extern eLeapRS LeapExtrapolatorAddHeadPose(IntPtr hExtrapolator, LEAP_HEAD_POSE_EVENT pHeadPoseEvent);

            [DllImport("LeapCExtrapolate", EntryPoint = "LeapExtrapolatorGetFrameSize")]
            internal static extern eLeapRS LeapExtrapolatorGetFrameSize(IntPtr hExtrapolator, Int64 timestamp, out UInt64 pncbEvent);

            [DllImport("LeapCExtrapolate", EntryPoint = "LeapExtrapolatorGetExtrapolatedFrame")]
            internal static extern eLeapRS LeapExtrapolatorGetExtrapolatedFrame(IntPtr hExtrapolator, Int64 timestamp, IntPtr pEvent, UInt64 ncbEvent);
        }
    }
}