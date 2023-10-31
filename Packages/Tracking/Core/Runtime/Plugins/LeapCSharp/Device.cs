/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap
{
    using LeapInternal;
    using System;

    /// <summary>
    /// The Device class represents a physically connected device.
    /// 
    /// The Device class contains information related to a particular connected
    /// device such as device id, field of view relative to the device,
    /// and the position and orientation of the device in relative coordinates.
    /// 
    /// The position and orientation describe the alignment of the device relative to the user.
    /// The alignment relative to the user is only descriptive. Aligning devices to users
    /// provides consistency in the parameters that describe user interactions.
    /// 
    /// Note that Device objects can be invalid, which means that they do not contain
    /// valid device information and do not correspond to a physical device.
    /// @since 1.0
    /// </summary>
    public class Device :
      IEquatable<Device>
    {

        /// <summary>
        /// Constructs a default Device object.
        /// 
        /// Get valid Device objects from a DeviceList object obtained using the
        /// Controller.Devices() method.
        /// 
        /// @since 1.0
        /// </summary>
        public Device() { }

        public Device(IntPtr deviceHandle,
                      IntPtr internalHandle,
                      float horizontalViewAngle,
                      float verticalViewAngle,
                      float range,
                      float baseline,
                      DeviceType type,
                      bool isStreaming,
                      uint status,
                      string serialNumber,
                      uint deviceID)
        {
            Handle = deviceHandle;
            HorizontalViewAngle = horizontalViewAngle;
            VerticalViewAngle = verticalViewAngle;
            Range = range;
            Baseline = baseline;
            Type = type;
            IsStreaming = isStreaming;
            SerialNumber = serialNumber;
            DeviceID = deviceID;
            UpdateStatus((eLeapDeviceStatus)status);
        }

        public Device(IntPtr deviceHandle,
                      IntPtr internalHandle,
                      float horizontalViewAngle,
                      float verticalViewAngle,
                      float range,
                      float baseline,
                      DeviceType type,
                      bool isStreaming,
                      uint status,
                      string serialNumber) : this(deviceHandle, internalHandle, horizontalViewAngle,
                          verticalViewAngle, range, baseline, type, isStreaming, status, serialNumber, 0)
        {
        }

        /// <summary>
        /// For internal use only.
        /// </summary>
        public void Update(
            float horizontalViewAngle,
            float verticalViewAngle,
            float range,
            float baseline,
            uint status,
            string serialNumber)
        {
            HorizontalViewAngle = horizontalViewAngle;
            VerticalViewAngle = verticalViewAngle;
            Range = range;
            Baseline = baseline;
            SerialNumber = serialNumber;
            UpdateStatus((eLeapDeviceStatus)status);
        }

        /// <summary>
        /// For internal use only.
        /// </summary>
        public void Update(Device updatedDevice)
        {
            HorizontalViewAngle = updatedDevice.HorizontalViewAngle;
            VerticalViewAngle = updatedDevice.VerticalViewAngle;
            Range = updatedDevice.Range;
            Baseline = updatedDevice.Baseline;
            IsStreaming = updatedDevice.IsStreaming;
            SerialNumber = updatedDevice.SerialNumber;
        }

        /// <summary>
        /// Updates the status fields by parsing the uint given by the event
        /// </summary>
        internal void UpdateStatus(eLeapDeviceStatus status)
        {
            if ((status & eLeapDeviceStatus.eLeapDeviceStatus_Streaming) == eLeapDeviceStatus.eLeapDeviceStatus_Streaming)
                IsStreaming = true;
            else
                IsStreaming = false;
            if ((status & eLeapDeviceStatus.eLeapDeviceStatus_Smudged) == eLeapDeviceStatus.eLeapDeviceStatus_Smudged)
                IsSmudged = true;
            else
                IsSmudged = false;
            if ((status & eLeapDeviceStatus.eLeapDeviceStatus_Robust) == eLeapDeviceStatus.eLeapDeviceStatus_Robust)
                IsLightingBad = true;
            else
                IsLightingBad = false;
            if ((status & eLeapDeviceStatus.eLeapDeviceStatus_LowResource) == eLeapDeviceStatus.eLeapDeviceStatus_LowResource)
                IsLowResource = true;
            else
                IsLowResource = false;
        }

        /// <summary>
        /// For internal use only.
        /// </summary>
        public IntPtr Handle { get; private set; }

        [Obsolete("Use LeapC.SetLeapPause instead")]
        public bool SetPaused(bool pause)
        {
            return false;
        }

        /// <summary>
        /// Compare Device object equality. 
        /// 
        /// Two Device objects are equal if and only if both Device objects represent the 
        /// exact same Device and both Devices are valid. 
        /// 
        /// @since 1.0 
        /// </summary>
        public bool Equals(Device other)
        {
            return SerialNumber == other.SerialNumber;
        }

        /// <summary>
        /// A string containing a brief, human readable description of the Device object.
        /// @since 1.0
        /// </summary>
        public override string ToString()
        {
            return "Device serial# " + this.SerialNumber;
        }

        /// <summary>
        /// The angle in radians of view along the x axis of this device.
        /// 
        /// The Leap Motion controller scans a region in the shape of an inverted pyramid
        /// centered at the device's center and extending upwards. The horizontalViewAngle
        /// reports the view angle along the long dimension of the device.
        /// 
        /// @since 1.0
        /// </summary>
        public float HorizontalViewAngle { get; private set; }

        /// <summary>
        /// The angle in radians of view along the z axis of this device.
        /// 
        /// The Leap Motion controller scans a region in the shape of an inverted pyramid
        /// centered at the device's center and extending upwards. The verticalViewAngle
        /// reports the view angle along the short dimension of the device.
        /// 
        /// @since 1.0
        /// </summary>
        public float VerticalViewAngle { get; private set; }

        /// <summary>
        /// The maximum reliable tracking range from the center of this device.
        /// 
        /// The range reports the maximum recommended distance from the device center
        /// for which tracking is expected to be reliable. This distance is not a hard limit.
        /// Tracking may be still be functional above this distance or begin to degrade slightly
        /// before this distance depending on calibration and extreme environmental conditions.
        /// 
        /// @since 1.0
        /// </summary>
        public float Range { get; private set; }

        /// <summary>
        /// The distance in mm between the center points of the stereo sensors.
        /// 
        /// The baseline value, together with the maximum resolution, influence the
        /// maximum range.
        /// 
        /// @since 2.2.5
        /// </summary>
        public float Baseline { get; private set; }

        /// <summary>
        /// Reports whether this device is streaming data to your application.
        /// 
        /// Currently only one controller can provide data at a time.
        /// @since 1.2
        /// </summary>
        public bool IsStreaming { get; internal set; }

        /// <summary>
        /// The device type.
        /// 
        /// Use the device type value in the (rare) circumstances that you
        /// have an application feature which relies on a particular type of device.
        /// Current types of device include the original Leap Motion peripheral,
        /// keyboard-embedded controllers, and laptop-embedded controllers.
        /// 
        /// @since 1.2
        /// </summary>
        public DeviceType Type { get; private set; }

        /// <summary>
        /// An alphanumeric serial number unique to each device.
        /// 
        /// Consumer device serial numbers consist of 2 letters followed by 11 digits.
        /// 
        /// When using multiple devices, the serial number provides an unambiguous
        /// identifier for each device.
        /// @since 2.2.2
        /// </summary>
        public string SerialNumber { get; private set; }

        /// <summary>
        /// Reports the ID assoicated with the device
        /// </summary>
        public uint DeviceID { get; private set; }

        private bool poseSet = false;
        private Pose devicePose = Pose.identity;

        /// <summary>
        /// The transform to world coordinates from 3D Leap coordinates.
        /// </summary>
        public Pose DevicePose
        {
            get
            {
                if (poseSet) // Assumes the devicePose never changes and so, uses the cached pose.
                {
                    return devicePose;
                }

                bool deviceTransformAvailable = LeapC.GetDeviceTransformAvailable(Handle);

                if (!deviceTransformAvailable)
                {
                    devicePose = Pose.identity;
                    poseSet = true;
                    return Pose.identity;
                }

                float[] data = new float[16];
                eLeapRS result = LeapC.GetDeviceTransform(Handle, data);

                if (result != eLeapRS.eLeapRS_Success || data == null)
                {
                    devicePose = Pose.identity;
                    poseSet = true;
                    return Pose.identity;
                }

                // Using the LEAP->OPENXR device transform matrix
                // Unitys matrices are generated as 4 columns:
                Matrix4x4 deviceTransform = new Matrix4x4(
                                                    new Vector4(data[0], data[1], data[2], data[3]),
                                                    new Vector4(data[4], data[5], data[6], data[7]),
                                                    new Vector4(data[8], data[9], data[10], data[11]),
                                                    new Vector4(data[12], data[13], data[14], data[15]));


                // An example of the expected matrix if it were 8cm forward from the head origin
                // Unitys matrices are generated as 4 columns:
                //Matrix4x4 deviceTransform = new Matrix4x4(
                //                                    new Vector4(-0.001f, 0, 0, 0),
                //                                    new Vector4(0, 0, -0.001f, 0),
                //                                    new Vector4(0, -0.001f, 0, 0),
                //                                    new Vector4(0, 0, -0.08f, 1));

                if (deviceTransform == Matrix4x4.identity)
                {
                    devicePose = Pose.identity;
                    poseSet = true;
                    return Pose.identity;
                }

                Matrix4x4 openXRToUnity = new Matrix4x4(
                                                    new Vector4(1f, 0, 0, 0),
                                                    new Vector4(0, 1f, 0, 0),
                                                    new Vector4(0, 0, -1f, 0),
                                                    new Vector4(0, 0, 0, 1));

                deviceTransform = openXRToUnity * deviceTransform;

                Vector3 outputPos = deviceTransform.GetPosition();
                //Quaternion outputRot = deviceTransform.rotation; // Note: the matrices we receive are not rotatrion matrices. This produces unexpected results

                devicePose = new Pose(outputPos, Quaternion.identity);

                poseSet = true;
                return devicePose;
            }
        }

        /// <summary>
        /// Returns the internal status field of the current device
        /// </summary>
        protected uint GetDeviceStatus()
        {
            eLeapRS result;

            LEAP_DEVICE_INFO deviceInfo = new LEAP_DEVICE_INFO();
            deviceInfo.serial = IntPtr.Zero;
            deviceInfo.size = (uint)System.Runtime.InteropServices.Marshal.SizeOf(deviceInfo);
            result = LeapC.GetDeviceInfo(Handle, ref deviceInfo);

            if (result != eLeapRS.eLeapRS_Success)
                return 0;
            uint status = deviceInfo.status;
            System.Runtime.InteropServices.Marshal.FreeCoTaskMem(deviceInfo.serial);
            return status;
        }


        /// <summary>
        /// The software has detected a possible smudge on the translucent cover
        /// over the Leap Motion cameras.
        /// @since 3.0
        /// </summary>
        public bool IsSmudged { get; internal set; }

        /// <summary>
        /// The software has entered low-resource mode
        /// @since 3.0
        /// </summary>
        public bool IsLowResource { get; internal set; }

        /// <summary>
        /// The software has detected excessive IR illumination, which may interfere 
        /// with tracking. If robust mode is enabled, the system will enter robust mode when 
        /// isLightingBad() is true. 
        /// @since 3.0 
        /// </summary>
        public bool IsLightingBad { get; internal set; }

        /// <summary>
        /// The available types of Leap Motion controllers.
        /// </summary>
        public enum DeviceType
        {
            TYPE_INVALID = -1,

            /// <summary>
            /// A standalone USB peripheral. The original Leap Motion controller device.
            /// @since 1.2
            /// </summary>
            TYPE_PERIPHERAL = (int)eLeapDeviceType.eLeapDeviceType_Peripheral,

            /// <summary>
            /// Internal research product codename "Dragonfly".
            /// </summary>
            TYPE_DRAGONFLY = (int)eLeapDeviceType.eLeapDeviceType_Dragonfly,

            /// <summary>
            /// Internal research product codename "Nightcrawler".
            /// </summary>
            TYPE_NIGHTCRAWLER = (int)eLeapDeviceType.eLeapDeviceType_Nightcrawler,

            /// <summary>
            /// Research product codename "Rigel".
            /// </summary>
            TYPE_RIGEL = (int)eLeapDeviceType.eLeapDevicePID_Rigel,

            /// <summary>
            /// The Ultraleap Stereo IR 170 (SIR170) hand tracking module.
            /// </summary>
            TYPE_SIR170 = (int)eLeapDeviceType.eLeapDevicePID_SIR170,

            /// <summary>
            /// The Ultraleap 3Di hand tracking camera.
            /// </summary>
            TYPE_3DI = (int)eLeapDeviceType.eLeapDevicePID_3Di,

            /// <summary>
            /// The Ultraleap Leap Motion Controller 2 hand tracking camera.
            /// </summary>
            TYPE_LMC2 = (int)eLeapDeviceType.eLeapDevicePID_LMC2
        }
    }
}