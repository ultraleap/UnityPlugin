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
  public class Device:
    IEquatable<Device>
  {
    /**
     * Constructs a default Device object.
     *
     * Get valid Device objects from a DeviceList object obtained using the
     * Controller::devices() method.
     *
     * \include Device_Device.txt
     *
     * @since 1.0
     */
    public Device() {}

    public Device(IntPtr deviceHandle,
                   float horizontalViewAngle,
                   float verticalViewAngle,
                   float range,
                   float baseline,
                   bool isEmbedded,
                   bool isStreaming,
                   string serialNumber)
    {
      Handle = deviceHandle;
      HorizontalViewAngle = horizontalViewAngle;
      VerticalViewAngle = verticalViewAngle;
      Range = range;
      Baseline = baseline;
      IsEmbedded = isEmbedded;
      IsStreaming = isStreaming;
      SerialNumber = serialNumber;
    }

    /* For internal use. */
    public void Update(
        float horizontalViewAngle,
        float verticalViewAngle,
        float range,
        float baseline,
        bool isEmbedded,
        bool isStreaming,
        string serialNumber)
    {
      HorizontalViewAngle = horizontalViewAngle;
      VerticalViewAngle = verticalViewAngle;
      Range = range;
      Baseline = baseline;
      IsEmbedded = isEmbedded;
      IsStreaming = isStreaming;
      SerialNumber = serialNumber;
    }

    /* For internal use. */
    public void Update(Device updatedDevice)
    {
      HorizontalViewAngle = updatedDevice.HorizontalViewAngle;
      VerticalViewAngle = updatedDevice.VerticalViewAngle;
      Range = updatedDevice.Range;
      Baseline = updatedDevice.Baseline;
      IsEmbedded = updatedDevice.IsEmbedded;
      IsStreaming = updatedDevice.IsStreaming;
      SerialNumber = updatedDevice.SerialNumber;
    }

    /* For internal use. */
    public IntPtr Handle { get; private set; }

    public bool SetPaused(bool pause)
    {
      ulong prior_state = 0;
      ulong set_flags = 0;
      ulong clear_flags = 0;
      if (pause)
        set_flags = (ulong)eLeapDeviceFlag.eLeapDeviceFlag_Stream;
      else
        clear_flags = (ulong)eLeapDeviceFlag.eLeapDeviceFlag_Stream;

      eLeapRS result = LeapC.SetDeviceFlags(Handle, set_flags, clear_flags, out prior_state);
      if (result == eLeapRS.eLeapRS_Success)
        return true;

      return false;
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
    public bool Equals(Device other)
    {
      return this.SerialNumber == other.SerialNumber;
    }

    /**
     * A string containing a brief, human readable description of the Device object.
     *
     * @returns A description of the Device as a string.
     * @since 1.0
     */
    public override string ToString()
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
    public float HorizontalViewAngle { get; private set; }

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
    public float VerticalViewAngle { get; private set; }

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
    public float Range { get; private set; }

    /**
     * The distance between the center points of the stereo sensors.
     *
     * The baseline value, together with the maximum resolution, influence the
     * maximum range.
     *
     * @returns The separation distance between the center of each sensor, in mm.
     * @since 2.2.5
     */
    public float Baseline { get; private set; }

    /**
     * Reports whether this device is embedded in another computer or computer
     * peripheral.
     *
     * @returns True, if this device is embedded in a laptop, keyboard, or other computer
     * component; false, if this device is a standalone controller.
     * @since 1.2
     */
    public bool IsEmbedded { get; private set; }

    /**
     * Reports whether this device is streaming data to your application.
     *
     * Currently only one controller can provide data at a time.
     * @since 1.2
     */
    public bool IsStreaming { get; private set; }

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
    public Device.DeviceType Type
    {
      get
      {
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
    public string SerialNumber { get; private set; }

    /**
     * The software has detected a possible smudge on the translucent cover
     * over the Leap Motion cameras.
     * 
     * Not implemented yet.
     *
     * \include Device_isSmudged.txt
     *
     * @since 3.0
     */
    public bool IsSmudged
    {
      get
      {
        return false; //TODO implement or remove Is Smudged
      }
    }

    /**
     * The software has detected excessive IR illumination, which may interfere
     * with tracking. If robust mode is enabled, the system will enter robust mode when
     * isLightingBad() is true.
     *
     * Not implemented yet.
     *
     * \include Device_isLightingBad.txt
     *
     * @since 3.0
     */
    public bool IsLightingBad
    {
      get
      {
        return false; //TODO Implement or remove IsLightingBad
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
