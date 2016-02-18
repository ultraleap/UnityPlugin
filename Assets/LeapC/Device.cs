/******************************************************************************\
* Copyright (C) 2012-2016 Leap Motion, Inc. All rights reserved.               *
* Leap Motion proprietary and confidential. Not for distribution.              *
* Use subject to the terms of the Leap Motion SDK Agreement available at       *
* https://developer.leapmotion.com/sdk_agreement, or another agreement         *
* between Leap Motion and you, your company or other organization.             *
\******************************************************************************/
namespace Leap
{
    using System;
    using System.Runtime.InteropServices;
    using LeapInternal;

    /**
   * The Device class represents a physically connected device.
   *
   * The Device class contains information related to a particular connected
   * device such as device id, field of view relative to the device,
   * and the position and orientation of the device in relative coordinates.
   *
   * The position and orientation describe the alignment of the device relative to the user.
   * The alignment relative to the user is only descriptive. Aligning devices to users
   * provides consistency in the parameters that describe user interactions.
   *
   * Note that Device objects can be invalid, which means that they do not contain
   * valid device information and do not correspond to a physical device.
   * Test for validity with the Device::isValid() function.
   * @since 1.0
   */

    public class Device
    {
        IntPtr _hDevice = IntPtr.Zero;
        float _horizontalViewAngle = 0;
        float _verticalViewAngle = 0;
        float _range = 0;
        float _baseline = 0;
        bool _isValid = false;
        bool _isEmbedded = false;
        bool _isStreaming = false;
        string _serialNumber = "Unknown";

        /**
     * Constructs a Device object.
     *
     * An uninitialized device is considered invalid.
     * Get valid Device objects from a DeviceList object obtained using the
     * Controller::devices() method.
     *
     * \include Device_Device.txt
     *
     * @since 1.0
     */
        public Device(){
        }

        public Device (IntPtr deviceHandle,
                       float horizontalViewAngle,
                       float verticalViewAngle,
                       float range,
                       float baseline,
                       bool isEmbedded,
                       bool isStreaming,
                       string serialNumber)
        {
            _hDevice = deviceHandle;
            _horizontalViewAngle = horizontalViewAngle;
            _verticalViewAngle = verticalViewAngle;
            _range = range;
            _baseline = baseline;
             _isValid = true;
            _isEmbedded = isEmbedded;
            _isStreaming = isStreaming;
            _serialNumber = serialNumber;
        }

        public void Update (
            float horizontalViewAngle,
            float verticalViewAngle,
            float range,
            float baseline,
            bool isEmbedded,
            bool isStreaming,
            string serialNumber)
        {
            _horizontalViewAngle = horizontalViewAngle;
            _verticalViewAngle = verticalViewAngle;
            _range = range;
            _baseline = baseline;
            _isValid = true;
            _isEmbedded = isEmbedded;
            _isStreaming = isStreaming;
            _serialNumber = serialNumber;
        }

        public void Update (Device updatedDevice)
        {
            _horizontalViewAngle = updatedDevice.HorizontalViewAngle;
            _verticalViewAngle = updatedDevice.VerticalViewAngle;
            _range = updatedDevice.Range;
            _baseline = updatedDevice.Baseline;
            _isValid = updatedDevice.IsValid;
            _isEmbedded = updatedDevice.IsEmbedded;
            _isStreaming = updatedDevice.IsStreaming;
            _serialNumber = updatedDevice.SerialNumber;
        }

        public bool UsesHandle(IntPtr handle){
            return handle == _hDevice;
        }

        public IntPtr Handle{
            get{
                return _hDevice;
            }
            private set{}
        }

        public bool SetPaused(bool pause){
            ulong prior_state = 0;
            ulong set_flags = 0;
            ulong clear_flags = 0;
            if(pause)
                set_flags = (ulong)eLeapDeviceFlag.eLeapDeviceFlag_Stream;
            else 
                clear_flags = (ulong)eLeapDeviceFlag.eLeapDeviceFlag_Stream;

            eLeapRS result = eLeapRS.eLeapRS_UnknownError;
            if(IsValid){
                result = LeapC.SetDeviceFlags(_hDevice, set_flags, clear_flags, out prior_state);
            }
            if(result == eLeapRS.eLeapRS_Success)
                return true;

            return false;
        }

        public bool IsPaused(){
            return false;
        }
        /**
     * The distance to the nearest edge of the Leap Motion controller's view volume.
     *
     * The view volume is an axis-aligned, inverted pyramid centered on the device origin
     * and extending upward to the range limit. The walls of the pyramid are described
     * by the horizontalViewAngle and verticalViewAngle and the roof by the range.
     * This function estimates the distance between the specified input position and the
     * nearest wall or roof of the view volume.
     *
     * \include Device_distanceToBoundary.txt
     *
     * @param position The point to use for the distance calculation.
     * @returns The distance in millimeters from the input position to the nearest boundary.
     * @since 1.0
     */
        public float DistanceToBoundary (Vector position)
        {
            return 0;
        }

        /**
     * Compare Device object equality.
     *
     * \include Device_operator_equals.txt
     *
     * Two Device objects are equal if and only if both Device objects represent the
     * exact same Device and both Devices are valid.
     * @since 1.0
     */
        public bool Equals (Device other)
        {
            return this.SerialNumber == other.SerialNumber;
        }

        /**
     * A string containing a brief, human readable description of the Device object.
     *
     * @returns A description of the Device as a string.
     * @since 1.0
     */
        public override string ToString ()
        {
            return "Device serial# " + this.SerialNumber;
        }

        /**
     * The angle of view along the x axis of this device.
     *
     * \image html images/Leap_horizontalViewAngle.png
     *
     * The Leap Motion controller scans a region in the shape of an inverted pyramid
     * centered at the device's center and extending upwards. The horizontalViewAngle
     * reports the view angle along the long dimension of the device.
     *
     * \include Device_horizontalViewAngle.txt
     *
     * @returns The horizontal angle of view in radians.
     * @since 1.0
     */
        public float HorizontalViewAngle {
            get {
                return _horizontalViewAngle;
            } 
        }

/**
     * The angle of view along the z axis of this device.
     *
     * \image html images/Leap_verticalViewAngle.png
     *
     * The Leap Motion controller scans a region in the shape of an inverted pyramid
     * centered at the device's center and extending upwards. The verticalViewAngle
     * reports the view angle along the short dimension of the device.
     *
     * \include Device_verticalViewAngle.txt
     *
     * @returns The vertical angle of view in radians.
     * @since 1.0
     */
        public float VerticalViewAngle {
            get {
                return _verticalViewAngle;
            } 
        }

/**
     * The maximum reliable tracking range from the center of this device.
     *
     * The range reports the maximum recommended distance from the device center
     * for which tracking is expected to be reliable. This distance is not a hard limit.
     * Tracking may be still be functional above this distance or begin to degrade slightly
     * before this distance depending on calibration and extreme environmental conditions.
     *
     * \include Device_range.txt
     *
     * @returns The recommended maximum range of the device in mm.
     * @since 1.0
     */
        public float Range {
            get {
                return _range;
            } 
        }

/**
     * The distance between the center points of the stereo sensors.
     *
     * The baseline value, together with the maximum resolution, influence the
     * maximum range.
     *
     * @returns The separation distance between the center of each sensor, in mm.
     * @since 2.2.5
     */
        public float Baseline {
            get {
                return _baseline;
            } 
        }

/**
     * Reports whether this is a valid Device object.
     *
     * \include Device_isValid.txt
     *
     * @returns True, if this Device object contains valid data.
     * @since 1.0
     */
        public bool IsValid {
            get {
//                IntPtr device;
//                eLeapRS result = LeapC.OpenDevice(_hDevice, out device);
//                if(result == eLeapRS.eLeapRS_Success){
//                    LEAP_DEVICE_INFO deviceInfo = new LEAP_DEVICE_INFO();
//                    deviceInfo.serial = Marshal.AllocCoTaskMem(_serialNumber.Length);
//                    deviceInfo.size =(uint) Marshal.SizeOf(deviceInfo);
//                    deviceInfo.serial_length = (uint)_serialNumber.Length;
//                    result = LeapC.GetDeviceInfo (device, out deviceInfo);
//                    if (deviceInfo.serial_length != (uint)_serialNumber.Length) {
//                        Marshal.FreeCoTaskMem(deviceInfo.serial);
//                        deviceInfo.serial = Marshal.AllocCoTaskMem((int)deviceInfo.serial_length);
//                        deviceInfo.size = (uint) Marshal.SizeOf(deviceInfo);
//                        result = LeapC.GetDeviceInfo (device, out deviceInfo);
//                    }
//                    if(result == eLeapRS.eLeapRS_Success){
//                        string serialnumber = Marshal.PtrToStringAnsi(deviceInfo.serial);
//                        Marshal.FreeCoTaskMem(deviceInfo.serial);
//                        this.Update(deviceInfo.h_fov,
//                            deviceInfo.v_fov,
//                            deviceInfo.range,
//                            deviceInfo.baseline,
//                            (deviceInfo.caps == (UInt32)eLeapDeviceCaps.eLeapDeviceCaps_Embedded),
//                            (deviceInfo.status == (UInt32)eLeapDeviceStatus.eLeapDeviceStatus_Streaming),
//                            serialnumber);
//                        return true;
//                    }
//                }
                return false;
            } 
        }

/**
     * Reports whether this device is embedded in another computer or computer
     * peripheral.
     *
     * @returns True, if this device is embedded in a laptop, keyboard, or other computer
     * component; false, if this device is a standalone controller.
     * @since 1.2
     */
        public bool IsEmbedded {
            get {
                return _isEmbedded;
            } 
        }

/**
     * Reports whether this device is streaming data to your application.
     *
     * Currently only one controller can provide data at a time.
     * @since 1.2
     */
        public bool IsStreaming {
            get {
                return _isStreaming;
            } 
        }

/**
     * Deprecated. Always reports false.
     *
     * @since 2.1
     * @deprecated 2.1.1
     */
        public bool IsFlipped {
            get {
                return false;
            } 
        }

/**
     * The device type.
     *
     * Use the device type value in the (rare) circumstances that you
     * have an application feature which relies on a particular type of device.
     * Current types of device include the original Leap Motion peripheral,
     * keyboard-embedded controllers, and laptop-embedded controllers.
     *
     * @returns The physical device type as a member of the DeviceType enumeration.
     * @since 1.2
     */
        public Device.DeviceType Type {
            get {
                return DeviceType.TYPE_INVALID;
            } 
        }

/**
     * An alphanumeric serial number unique to each device.
     *
     * Consumer device serial numbers consist of 2 letters followed by 11 digits.
     *
     * When using multiple devices, the serial number provides an unambiguous
     * identifier for each device.
     * @since 2.2.2
     */
        public string SerialNumber {
            get {
                return _serialNumber;
            } 
        }

        public Vector Position {
            get {
                return Vector.Zero;
            } 
        }

        public Matrix Orientation {
            get {
                return Matrix.Identity;
            } 
        }
        /**
     * The software has detected a possible smudge on the translucent cover
     * over the Leap Motion cameras.
     *
     * \include Device_isSmudged.txt
     *
     * @since 2.4.0
     */  public bool IsSmudged {
            get {
                return false; //TODO implement or remove Is Smudged
            } 
        }
        
        /**
     * The software has detected excessive IR illumination, which may interfere
     * with tracking. If robust mode is enabled, the system will enter robust mode when
     * isLightingBad() is true.
     *
     * \include Device_isLightingBad.txt
     *
     * @since 2.4.0
     */  public bool IsLightingBad {
            get {
                return false; //TODO Implement or remove IsLightingBad
            } 
        }
/**
     * Returns an invalid Device object.
     *
     * You can use the instance returned by this function in comparisons testing
     * whether a given Device instance is valid or invalid. (You can also use the
     * Device::isValid() function.)
     *
     * \include Device_invalid.txt
     *
     * @returns The invalid Device instance.
     * @since 1.0
     */
        public static Device Invalid {
            get {
                return new Device ();
            } 
        }

        /**
     * The available types of Leap Motion controllers.
     * @since 1.2
     */
        public enum DeviceType
        {
            TYPE_INVALID = -1,
            /**
       * A standalone USB peripheral. The original Leap Motion controller device.
       * @since 1.2
       */
            TYPE_PERIPHERAL = 1,
            /**
       * A controller embedded in a keyboard.
       * @since 1.2
       */
            TYPE_LAPTOP,
            /**
       * A controller embedded in a laptop computer.
       * @since 1.2
       */
            TYPE_KEYBOARD
        }

    }

}
