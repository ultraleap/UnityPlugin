/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

namespace Leap {
  using System;
  using LeapInternal;

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
    IEquatable<Device> {

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
                   float horizontalViewAngle,
                   float verticalViewAngle,
                   float range,
                   float baseline,
                   DeviceType type,
                   bool isStreaming,
                   string serialNumber) {
      Handle = deviceHandle;
      HorizontalViewAngle = horizontalViewAngle;
      VerticalViewAngle = verticalViewAngle;
      Range = range;
      Baseline = baseline;
      Type = type;
      IsStreaming = isStreaming;
      SerialNumber = serialNumber;
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    public void Update(
        float horizontalViewAngle,
        float verticalViewAngle,
        float range,
        float baseline,
        bool isStreaming,
        string serialNumber) {
      HorizontalViewAngle = horizontalViewAngle;
      VerticalViewAngle = verticalViewAngle;
      Range = range;
      Baseline = baseline;
      IsStreaming = isStreaming;
      SerialNumber = serialNumber;
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    public void Update(Device updatedDevice) {
      HorizontalViewAngle = updatedDevice.HorizontalViewAngle;
      VerticalViewAngle = updatedDevice.VerticalViewAngle;
      Range = updatedDevice.Range;
      Baseline = updatedDevice.Baseline;
      IsStreaming = updatedDevice.IsStreaming;
      SerialNumber = updatedDevice.SerialNumber;
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    public IntPtr Handle { get; private set; }

    public bool SetPaused(bool pause) {
      ulong prior_state = 0;
      ulong set_flags = 0;
      ulong clear_flags = 0;
      if (pause) {
        set_flags = (ulong)eLeapDeviceFlag.eLeapDeviceFlag_Stream;
      } else {
        clear_flags = (ulong)eLeapDeviceFlag.eLeapDeviceFlag_Stream;
      }

      eLeapRS result = LeapC.SetDeviceFlags(Handle, set_flags, clear_flags, out prior_state);

      return result == eLeapRS.eLeapRS_Success;
    }

    /// <summary>
    /// Compare Device object equality. 
    /// 
    /// Two Device objects are equal if and only if both Device objects represent the 
    /// exact same Device and both Devices are valid. 
    /// 
    /// @since 1.0 
    /// </summary>
    public bool Equals(Device other) {
      return SerialNumber == other.SerialNumber;
    }

    /// <summary>
    /// A string containing a brief, human readable description of the Device object.
    /// @since 1.0
    /// </summary>
    public override string ToString() {
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
    public bool IsStreaming { get; private set; }

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
    /// The software has detected a possible smudge on the translucent cover
    /// over the Leap Motion cameras.
    /// 
    /// Not implemented yet.
    /// @since 3.0
    /// </summary>
    public bool IsSmudged {
      get {
        throw new NotImplementedException();
      }
    }

    /// <summary>
    /// The software has detected excessive IR illumination, which may interfere 
    /// with tracking. If robust mode is enabled, the system will enter robust mode when 
    /// isLightingBad() is true. 
    /// 
    /// Not implemented yet. 
    /// @since 3.0 
    /// </summary>
    public bool IsLightingBad {
      get {
        throw new NotImplementedException();
      }
    }

    /// <summary>
    /// The available types of Leap Motion controllers.
    /// </summary>
    public enum DeviceType {
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

      [Obsolete]
      TYPE_LAPTOP,
      [Obsolete]
      TYPE_KEYBOARD
    }
  }
}
