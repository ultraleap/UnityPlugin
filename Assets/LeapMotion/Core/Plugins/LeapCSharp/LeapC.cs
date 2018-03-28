/******************************************************************************\
* Copyright (C) 2012-2017 Leap Motion, Inc. All rights reserved.               *
* Leap Motion proprietary and confidential. Not for distribution.              *
* Use subject to the terms of the Leap Motion SDK Agreement available at       *
* https://developer.leapmotion.com/sdk_agreement, or another agreement         *
* between Leap Motion and you, your company or other organization.             *
\******************************************************************************/


namespace LeapInternal {
  using System;
  using System.Runtime.InteropServices;

  public enum eLeapConnectionStatus : uint {
    eLeapConnectionStatus_NotConnected = 0, //!< // A connection has been established
    eLeapConnectionStatus_Connected, //!< The connection has not been completed. Call OpenConnection.
    eLeapConnectionStatus_HandshakeIncomplete, //!< The connection handshake has not completed
    eLeapConnectionStatus_NotRunning = 0xE7030004 //!< A connection could not be established because the server does not appear to be running
  };

  public enum eLeapDeviceCaps : uint {
    eLeapDeviceCaps_Color = 0x00000001, //!< The device can send color images
  };

  public enum eLeapDeviceType : uint {
    eLeapDeviceType_Peripheral = 0x0003, //!< The Leap Motion consumer peripheral
    eLeapDeviceType_Dragonfly = 0x1102, //!< Internal research product codename "Dragonfly"
    eLeapDeviceType_Nightcrawler = 0x1201 //!< Internal research product codename "Nightcrawler"
  };

  public enum eDistortionMatrixType {
    eDistortionMatrixType_64x64 //!< A 64x64 matrix of pairs of points.
  };

  public enum eLeapPolicyFlag : uint {
    eLeapPolicyFlag_BackgroundFrames = 0x00000001, //!< Allows frame receipt even when this application is not the foreground application
    eLeapPolicyFlag_Images = 0x00000002, //!< Allow streaming images
    eLeapPolicyFlag_OptimizeHMD = 0x00000004, //!< Optimize HMD Policy Flag
    eLeapPolicyFlag_AllowPauseResume = 0x00000008, //!< Modifies the security token to allow calls to LeapPauseDevice to succeed
    eLeapPolicyFlag_MapPoints = 0x00000080, //!< Allows streaming map points
    eLeapPolicyFlag_IncludeAllFrames = 0x00008000, //!< Include native-app frames when receiving background frames.
    eLeapPolicyFlag_NonExclusive = 0x00800000  //!< Allow background apps to also receive frames.
  };


  public enum eLeapDeviceStatus : uint {
    eLeapDeviceStatus_Streaming = 0x00000001, //!< Presently sending frames to all clients that have requested them
    eLeapDeviceStatus_Paused = 0x00000002, //!< Device streaming has been paused
    eLeapDeviceStatus_UnknownFailure = 0xE8010000, //!< The device has failed, but the failure reason is not known
    eLeapDeviceStatus_BadCalibration = 0xE8010001, //!< Bad calibration, cannot send frames
    eLeapDeviceStatus_BadFirmware = 0xE8010002, //!< Corrupt firmware and/or cannot receive a required firmware update
    eLeapDeviceStatus_BadTransport = 0xE8010003, //!< Exhibiting USB communications issues
    eLeapDeviceStatus_BadControl = 0xE8010004, //!< Missing critical control interfaces needed for communication
  };

  public enum eLeapImageType {
    eLeapImageType_Unknown = 0,
    eLeapImageType_Default, //!< Default processed IR image
    eLeapImageType_Raw //!< Image from raw sensor values
  };

  public enum eLeapImageFormat : uint {
    eLeapImageFormat_UNKNOWN = 0, //!< Unknown format (shouldn't happen)
    eLeapImageType_IR = 0x317249, //!< An infrared image
    eLeapImageType_RGBIr_Bayer = 0x49425247, //!< A Bayer RGBIr image with uncorrected RGB channels
  };

  public enum eLeapPerspectiveType {
    eLeapPerspectiveType_invalid = 0, //!< Reserved, invalid
    eLeapPerspectiveType_stereo_left = 1, //!< A canonically left image
    eLeapPerspectiveType_stereo_right = 2, //!< A canonically right image
    eLeapPerspectiveType_mono = 3, //!< Reserved for future use
  };

  public enum eLeapImageRequestError {
    eLeapImageRequestError_Unknown, //!< The reason for the failure is unknown
    eLeapImageRequestError_ImagesDisabled, //!< Images are turned off in the user's configuration
    eLeapImageRequestError_Unavailable, //!< The requested images are not available
    eLeapImageRequestError_InsufficientBuffer, //!< The provided buffer is not large enough for the requested images
  }

  public enum eLeapHandType {
    eLeapHandType_Left, //!< Left hand
    eLeapHandType_Right //!< Right hand
  };

  public enum eLeapLogSeverity {
    eLeapLogSeverity_Unknown = 0,
    eLeapLogSeverity_Critical,
    eLeapLogSeverity_Warning,
    eLeapLogSeverity_Information
  };

  public enum eLeapValueType : int {
    eLeapValueType_Unknown,
    eLeapValueType_Boolean,
    eLeapValueType_Int32,
    eLeapValueType_Float,
    eLeapValueType_String
  };

  public enum eLeapAllocatorType : uint {
    eLeapAllocatorType_Int8 = 0,
    eLeapAllocatorType_Uint8 = 1,
    eLeapAllocatorType_Int16 = 2,
    eLeapAllocatorType_UInt16 = 3,
    eLeapAllocatorType_Int32 = 4,
    eLeapAllocatorType_UInt32 = 5,
    eLeapAllocatorType_Float = 6,
    eLeapAllocatorType_Int64 = 8,
    eLeapAllocatorType_UInt64 = 9,
    eLeapAllocatorType_Double = 10,
  };

  public enum eLeapRS : uint {
    eLeapRS_Success = 0x00000000, //!< The operation completed successfully
    eLeapRS_UnknownError = 0xE2010000, //!< An unknown error has occurred
    eLeapRS_InvalidArgument = 0xE2010001, //!< An invalid argument was specified
    eLeapRS_InsufficientResources = 0xE2010002, //!< Insufficient resources existed to complete the request
    eLeapRS_InsufficientBuffer = 0xE2010003, //!< The specified buffer was not large enough to complete the request
    eLeapRS_Timeout = 0xE2010004, //!< The requested operation has timed out
    eLeapRS_NotConnected = 0xE2010005, //!< The connection is not open
    eLeapRS_HandshakeIncomplete = 0xE2010006, //!< The request did not succeed because the client has not finished connecting to the server
    eLeapRS_BufferSizeOverflow = 0xE2010007, //!< The specified buffer size is too large
    eLeapRS_ProtocolError = 0xE2010008, //!< A communications protocol error has occurred
    eLeapRS_InvalidClientID = 0xE2010009, //!< The server incorrectly specified zero as a client ID
    eLeapRS_UnexpectedClosed = 0xE201000A, //!< The connection to the service was unexpectedly closed while reading a message
    eLeapRS_CannotCancelImageFrameRequest = 0xE201000B, //!< An attempt to cancel an image request failed (either too late, or the image token was invalid)
    eLeapRS_NotAvailable = 0xE7010002, //!< A connection could not be established to the Leap Motion service
    eLeapRS_NotStreaming = 0xE7010004, //!< The requested operation can only be performed while the device is streaming
                                       /**
                                       * It is possible that the device identifier
                                       * is invalid, or that the device has been disconnected since being enumerated.
                                       */
    eLeapRS_CannotOpenDevice = 0xE7010005, //!< The specified device could not be opened. Invalid device identifier or the device has been disconnected since being enumerated.
  };

  public enum eLeapEventType {
    eLeapEventType_None = 0,
    eLeapEventType_Connection,
    eLeapEventType_ConnectionLost,
    eLeapEventType_Device,

    /// <summary>
    /// Note that unplugging a device generates an eLeapEventType_DeviceLost event
    /// message, not a failure message.
    /// </summary>
    eLeapEventType_DeviceFailure,

    eLeapEventType_Policy,
    eLeapEventType_Tracking = 0x100,
    eLeapEventType_ImageRequestError,
    eLeapEventType_ImageComplete,
    eLeapEventType_LogEvent,
    eLeapEventType_DeviceLost,
    eLeapEventType_ConfigResponse,
    eLeapEventType_ConfigChange,
    eLeapEventType_DeviceStatusChange,
    eLeapEventType_DroppedFrame,
    eLeapEventType_Image,
    eLeapEventType_PointMappingChange,
    eLeapEventType_LogEvents,
    eLeapEventType_HeadPose
  };

  public enum eLeapDeviceFlag : uint {
    /**
    * This flag is updated when the user pauses or resumes tracking on the device from the Leap control
    * panel.  Modification of this flag will fail if the AllowPauseResume policy is not set on this device
    * object.
    */
    eLeapDeviceFlag_Stream = 0x00000001 //!< Flag set if the device is presently streaming frames
  };
  public enum eLeapConnectionFlags : uint {
    eLeapConnectionFlags_Default = 0x00000000, //!< Currently there is only a default state flag.
  };

  public enum eLeapDroppedFrameType {
    eLeapDroppedFrameType_PreprocessingQueue,
    eLeapDroppedFrameType_TrackingQueue,
    eLeapDroppedFrameType_Other
  };

  //Note the following LeapC structs are just IntPtrs in C#:
  // LEAP_CONNECTION is an IntPtr
  // LEAP_DEVICE is an IntPtr
  // LEAP_CLOCK_REBASER is an IntPtr

  [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
  public struct LEAP_CONNECTION_CONFIG {
    public UInt32 size;
    public UInt32 flags;
    public string server_namespace;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_CONNECTION_INFO {
    public UInt32 size;
    public eLeapConnectionStatus status;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_CONNECTION_EVENT {
    public UInt32 flags;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_DEVICE_REF {
    public IntPtr handle; //void *
    public UInt32 id;
    public LEAP_DEVICE_REF(IntPtr handle, UInt32 id) {
      this.handle = handle;
      this.id = id;
    }
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_CONNECTION_LOST_EVENT {
    public UInt32 flags;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_ALLOCATOR {
    public LeapInternal.Allocate allocate;
    public LeapInternal.Deallocate deallocate;
    public UInt32 number;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_DEVICE_EVENT {
    public UInt32 flags;
    public LEAP_DEVICE_REF device;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_DEVICE_FAILURE_EVENT {
    public eLeapDeviceStatus status;
    public IntPtr hDevice;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_TRACKING_EVENT {
    public LEAP_FRAME_HEADER info;
    public Int64 tracking_id;
    public LEAP_VECTOR interaction_box_size;
    public LEAP_VECTOR interaction_box_center;
    public UInt32 nHands;
    public IntPtr pHands; //LEAP_HAND*
    public float framerate;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_DROPPED_FRAME_EVENT {
    public Int64 frame_id;
    public eLeapDroppedFrameType reason;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_POINT_MAPPING_CHANGE_EVENT {
    public Int64 frame_id;
    public Int64 timestamp;
    public UInt32 nPoints;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_POINT_MAPPING {
    public Int64 frame_id;
    public Int64 timestamp;
    public UInt32 nPoints;
    public LEAP_VECTOR[] points;
    public UInt32[] ids;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_HEAD_POSE_EVENT {
    public Int64 timestamp;
    public LEAP_VECTOR head_position;
    public LEAP_QUATERNION head_orientation;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_CONNECTION_MESSAGE {
    public UInt32 size;
    public eLeapEventType type;
    public IntPtr eventStructPtr;
  }


  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_DISCONNECTION_EVENT {
    public UInt32 reserved;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
  public struct LEAP_DEVICE_INFO {
    public UInt32 size;
    public UInt32 status;
    public UInt32 caps;
    public eLeapDeviceType type;
    public UInt32 baseline;
    public UInt32 serial_length;
    public IntPtr serial; //char*
    public float h_fov;
    public float v_fov;
    public UInt32 range;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_DISTORTION_MATRIX {
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2 * 64 * 64)]//2floats * 64 width * 64 height
    public float[] matrix_data;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_FRAME_HEADER {
    public IntPtr reserved;
    public Int64 frame_id;
    public Int64 timestamp;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_IMAGE_PROPERTIES {
    public eLeapImageType type;
    public eLeapImageFormat format;
    public UInt32 bpp;
    public UInt32 width;
    public UInt32 height;
    public float x_scale;
    public float y_scale;
    public float x_offset;
    public float y_offset;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_IMAGE {
    public LEAP_IMAGE_PROPERTIES properties;
    public UInt64 matrix_version;
    public IntPtr distortionMatrix; //LEAP_DISTORTION_MATRIX* distortion_matrix[2]
    public IntPtr data; // void* of an allocator-supplied buffer
    public UInt32 offset; // Offset, in bytes, from beginning of buffer to start of image data
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_IMAGE_EVENT {
    public LEAP_FRAME_HEADER info;
    public LEAP_IMAGE leftImage;
    public LEAP_IMAGE rightImage;
    public IntPtr calib; //LEAP_CALIBRATION
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_VECTOR {
    public float x;
    public float y;
    public float z;

    public Leap.Vector ToLeapVector() {
      return new Leap.Vector(x, y, z);
    }

    public LEAP_VECTOR(Leap.Vector leap) {
      x = leap.x;
      y = leap.y;
      z = leap.z;
    }
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_QUATERNION {
    public float x;
    public float y;
    public float z;
    public float w;

    public Leap.LeapQuaternion ToLeapQuaternion() {
      return new Leap.LeapQuaternion(x, y, z, w);
    }

    public LEAP_QUATERNION(Leap.LeapQuaternion q) {
      x = q.x;
      y = q.y;
      z = q.z;
      w = q.w;
    }
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_MATRIX_3x3 {
    public LEAP_VECTOR m1;
    public LEAP_VECTOR m2;
    public LEAP_VECTOR m3;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_BONE {
    public LEAP_VECTOR prev_joint;
    public LEAP_VECTOR next_joint;
    public float width;
    public LEAP_QUATERNION rotation;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_DIGIT {
    public Int32 finger_id;
    public LEAP_BONE metacarpal;
    public LEAP_BONE proximal;
    public LEAP_BONE intermediate;
    public LEAP_BONE distal;
    public LEAP_VECTOR tip_velocity;
    public LEAP_VECTOR stabilized_tip_position;
    public Int32 is_extended;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_PALM {
    public LEAP_VECTOR position;
    public LEAP_VECTOR stabilized_position;
    public LEAP_VECTOR velocity;
    public LEAP_VECTOR normal;
    public float width;
    public LEAP_VECTOR direction;
    public LEAP_QUATERNION orientation;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_HAND {
    public UInt32 id;
    public UInt32 flags;
    public eLeapHandType type;
    public float confidence;
    public UInt64 visible_time;
    public float pinch_distance;
    public float grab_angle;
    public float pinch_strength;
    public float grab_strength;
    public LEAP_PALM palm;
    public LEAP_DIGIT thumb;
    public LEAP_DIGIT index;
    public LEAP_DIGIT middle;
    public LEAP_DIGIT ring;
    public LEAP_DIGIT pinky;
    public LEAP_BONE arm;
  }


  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_TIP {
    public LEAP_VECTOR position;
    public float radius;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
  public struct LEAP_LOG_EVENT {
    public eLeapLogSeverity severity;
    public Int64 timestamp;
    public string message;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
  public struct LEAP_POLICY_EVENT {
    public UInt32 reserved;
    public UInt32 current_policy;
  }

  [StructLayout(LayoutKind.Explicit, Pack = 1)]
  public struct LEAP_VARIANT_VALUE_TYPE {
    [FieldOffset(0)]
    public eLeapValueType type;
    [FieldOffset(4)]
    public Int32 boolValue;
    [FieldOffset(4)]
    public Int32 intValue;
    [FieldOffset(4)]
    public float floatValue;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
  public struct LEAP_VARIANT_REF_TYPE {
    public eLeapValueType type;
    public string stringValue;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_CONFIG_RESPONSE_EVENT {
    public UInt32 requestId;
    public LEAP_VARIANT_VALUE_TYPE value;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
  public struct LEAP_CONFIG_RESPONSE_EVENT_WITH_REF_TYPE {
    public UInt32 requestId;
    public LEAP_VARIANT_REF_TYPE value;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_CONFIG_CHANGE_EVENT {
    public UInt32 requestId;
    public bool status;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
  public struct LEAP_TELEMETRY_DATA {
    public UInt32 threadId;
    public UInt64 startTime;
    public UInt64 endTime;
    public UInt32 zoneDepth;
    public string fileName;
    public UInt32 lineNumber;
    public string zoneName;
  }

  public class LeapC {
    private LeapC() { }
    public static int DistortionSize = 64;

    [DllImport("LeapC", EntryPoint = "LeapGetNow")]
    public static extern long GetNow();

    [DllImport("LeapC", EntryPoint = "LeapCreateClockRebaser")]
    public static extern eLeapRS CreateClockRebaser(out IntPtr phClockRebaser);

    [DllImport("LeapC", EntryPoint = "LeapDestroyClockRebaser")]
    public static extern eLeapRS DestroyClockRebaser(IntPtr hClockRebaser);

    [DllImport("LeapC", EntryPoint = "LeapUpdateRebase")]
    public static extern eLeapRS UpdateRebase(IntPtr hClockRebaser, Int64 userClock, Int64 leapClock);

    [DllImport("LeapC", EntryPoint = "LeapRebaseClock")]
    public static extern eLeapRS RebaseClock(IntPtr hClockRebaser, Int64 userClock, out Int64 leapClock);

    [DllImport("LeapC", EntryPoint = "LeapCreateConnection")]
    public static extern eLeapRS CreateConnection(ref LEAP_CONNECTION_CONFIG pConfig, out IntPtr pConnection);

    //Overrides to allow config to be set to null to use default config
    [DllImport("LeapC", EntryPoint = "LeapCreateConnection")]
    private static extern eLeapRS CreateConnection(IntPtr nulled, out IntPtr pConnection);
    public static eLeapRS CreateConnection(out IntPtr pConnection) {
      return CreateConnection(IntPtr.Zero, out pConnection);
    }

    [DllImport("LeapC", EntryPoint = "LeapGetConnectionInfo")]
    public static extern eLeapRS GetConnectionInfo(IntPtr hConnection, ref LEAP_CONNECTION_INFO pInfo);

    [DllImport("LeapC", EntryPoint = "LeapOpenConnection")]
    public static extern eLeapRS OpenConnection(IntPtr hConnection);

    [DllImport("LeapC", EntryPoint = "LeapSetAllocator")]
    public static extern eLeapRS SetAllocator(IntPtr hConnection, ref LEAP_ALLOCATOR pAllocator);

    [DllImport("LeapC", EntryPoint = "LeapGetDeviceList")]
    public static extern eLeapRS GetDeviceList(IntPtr hConnection, [In, Out] LEAP_DEVICE_REF[] pArray, out UInt32 pnArray);

    [DllImport("LeapC", EntryPoint = "LeapGetDeviceList")]
    private static extern eLeapRS GetDeviceList(IntPtr hConnection, [In, Out] IntPtr pArray, out UInt32 pnArray);
    //Override to allow pArray argument to be set to null (IntPtr.Zero) in order to get the device count
    public static eLeapRS GetDeviceCount(IntPtr hConnection, out UInt32 deviceCount) {
      return GetDeviceList(hConnection, IntPtr.Zero, out deviceCount);
    }

    [DllImport("LeapC", EntryPoint = "LeapOpenDevice")]
    public static extern eLeapRS OpenDevice(LEAP_DEVICE_REF rDevice, out IntPtr pDevice);

    [DllImport("LeapC", EntryPoint = "LeapGetDeviceInfo", CharSet = CharSet.Ansi)]
    public static extern eLeapRS GetDeviceInfo(IntPtr hDevice, ref LEAP_DEVICE_INFO info);

    [DllImport("LeapC", EntryPoint = "LeapSetPolicyFlags")]
    public static extern eLeapRS SetPolicyFlags(IntPtr hConnection, UInt64 set, UInt64 clear);

    [DllImport("LeapC", EntryPoint = "LeapSetDeviceFlags")]
    public static extern eLeapRS SetDeviceFlags(IntPtr hDevice, UInt64 set, UInt64 clear, out UInt64 prior);

    [DllImport("LeapC", EntryPoint = "LeapPollConnection")]
    public static extern eLeapRS PollConnection(IntPtr hConnection, UInt32 timeout, ref LEAP_CONNECTION_MESSAGE msg);

    [DllImport("LeapC", EntryPoint = "LeapGetFrameSize")]
    public static extern eLeapRS GetFrameSize(IntPtr hConnection, Int64 timestamp, out UInt64 pncbEvent);

    [DllImport("LeapC", EntryPoint = "LeapInterpolateFrame")]
    public static extern eLeapRS InterpolateFrame(IntPtr hConnection, Int64 timestamp, IntPtr pEvent, UInt64 ncbEvent);

    [DllImport("LeapC", EntryPoint = "LeapInterpolateFrameFromTime")]
    public static extern eLeapRS InterpolateFrameFromTime(IntPtr hConnection, Int64 timestamp, Int64 sourceTimestamp, IntPtr pEvent, UInt64 ncbEvent);

    [DllImport("LeapC", EntryPoint = "LeapInterpolateHeadPose")]
    public static extern eLeapRS InterpolateHeadPose(IntPtr hConnection, Int64 timestamp, ref LEAP_HEAD_POSE_EVENT headPose);

    [DllImport("LeapC", EntryPoint = "LeapPixelToRectilinear")]
    public static extern LEAP_VECTOR LeapPixelToRectilinear(IntPtr hConnection, eLeapPerspectiveType camera, LEAP_VECTOR pixel);

    [DllImport("LeapC", EntryPoint = "LeapRectilinearToPixel")]
    public static extern LEAP_VECTOR LeapRectilinearToPixel(IntPtr hConnection, eLeapPerspectiveType camera, LEAP_VECTOR rectilinear);

    [DllImport("LeapC", EntryPoint = "LeapCloseDevice")]
    public static extern void CloseDevice(IntPtr pDevice);

    [DllImport("LeapC", EntryPoint = "LeapCloseConnection")]
    public static extern eLeapRS CloseConnection(IntPtr hConnection);

    [DllImport("LeapC", EntryPoint = "LeapDestroyConnection")]
    public static extern void DestroyConnection(IntPtr connection);

    [DllImport("LeapC", EntryPoint = "LeapSaveConfigValue")]
    private static extern eLeapRS SaveConfigValue(IntPtr hConnection, string key, IntPtr value, out UInt32 requestId);

    [DllImport("LeapC", EntryPoint = "LeapRequestConfigValue")]
    public static extern eLeapRS RequestConfigValue(IntPtr hConnection, string name, out UInt32 request_id);

    public static eLeapRS SaveConfigValue(IntPtr hConnection, string key, bool value, out UInt32 requestId) {
      LEAP_VARIANT_VALUE_TYPE valueStruct = new LEAP_VARIANT_VALUE_TYPE(); //This is a C# approximation of a C union
      valueStruct.type = eLeapValueType.eLeapValueType_Boolean;
      valueStruct.boolValue = value ? 1 : 0;
      return LeapC.SaveConfigWithValueType(hConnection, key, valueStruct, out requestId);
    }
    public static eLeapRS SaveConfigValue(IntPtr hConnection, string key, Int32 value, out UInt32 requestId) {
      LEAP_VARIANT_VALUE_TYPE valueStruct = new LEAP_VARIANT_VALUE_TYPE();
      valueStruct.type = eLeapValueType.eLeapValueType_Int32;
      valueStruct.intValue = value;
      return LeapC.SaveConfigWithValueType(hConnection, key, valueStruct, out requestId);
    }
    public static eLeapRS SaveConfigValue(IntPtr hConnection, string key, float value, out UInt32 requestId) {
      LEAP_VARIANT_VALUE_TYPE valueStruct = new LEAP_VARIANT_VALUE_TYPE();
      valueStruct.type = eLeapValueType.eLeapValueType_Float;
      valueStruct.floatValue = value;
      return LeapC.SaveConfigWithValueType(hConnection, key, valueStruct, out requestId);
    }
    public static eLeapRS SaveConfigValue(IntPtr hConnection, string key, string value, out UInt32 requestId) {
      LEAP_VARIANT_REF_TYPE valueStruct;
      valueStruct.type = eLeapValueType.eLeapValueType_String;
      valueStruct.stringValue = value;
      return LeapC.SaveConfigWithRefType(hConnection, key, valueStruct, out requestId);
    }
    private static eLeapRS SaveConfigWithValueType(IntPtr hConnection, string key, LEAP_VARIANT_VALUE_TYPE valueStruct, out UInt32 requestId) {
      IntPtr configValue = Marshal.AllocHGlobal(Marshal.SizeOf(valueStruct));
      eLeapRS callResult = eLeapRS.eLeapRS_UnknownError;
      try {
        Marshal.StructureToPtr(valueStruct, configValue, false);
        callResult = SaveConfigValue(hConnection, key, configValue, out requestId);
      } finally {
        Marshal.FreeHGlobal(configValue);
      }
      return callResult;
    }
    private static eLeapRS SaveConfigWithRefType(IntPtr hConnection, string key, LEAP_VARIANT_REF_TYPE valueStruct, out UInt32 requestId) {
      IntPtr configValue = Marshal.AllocHGlobal(Marshal.SizeOf(valueStruct));
      eLeapRS callResult = eLeapRS.eLeapRS_UnknownError;
      try {
        Marshal.StructureToPtr(valueStruct, configValue, false);
        callResult = SaveConfigValue(hConnection, key, configValue, out requestId);
      } finally {
        Marshal.FreeHGlobal(configValue);
      }
      return callResult;
    }

    [DllImport("LeapC", EntryPoint = "LeapGetPointMappingSize")]
    public static extern eLeapRS GetPointMappingSize(IntPtr hConnection, ref ulong pSize);

    [DllImport("LeapC", EntryPoint = "LeapGetPointMapping")]
    public static extern eLeapRS GetPointMapping(IntPtr hConnection, IntPtr pointMapping, ref ulong pSize);

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct LEAP_RECORDING_PARAMETERS {
      public UInt32 mode;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct LEAP_RECORDING_STATUS {
      public UInt32 mode;
    }

    [DllImport("LeapC", EntryPoint = "LeapRecordingOpen")]
    public static extern eLeapRS RecordingOpen(ref IntPtr ppRecording, string userPath, LEAP_RECORDING_PARAMETERS parameters);

    [DllImport("LeapC", EntryPoint = "LeapRecordingClose")]
    public static extern eLeapRS RecordingClose(ref IntPtr ppRecording);

    [DllImport("LeapC", EntryPoint = "LeapRecordingGetStatus")]
    public static extern eLeapRS LeapRecordingGetStatus(IntPtr pRecording, ref LEAP_RECORDING_STATUS status);

    [DllImport("LeapC", EntryPoint = "LeapRecordingReadSize")]
    public static extern eLeapRS RecordingReadSize(IntPtr pRecording, ref UInt64 pncbEvent);

    [DllImport("LeapC", EntryPoint = "LeapRecordingRead")]
    public static extern eLeapRS RecordingRead(IntPtr pRecording, ref LEAP_TRACKING_EVENT pEvent, UInt64 ncbEvent);

    [DllImport("LeapC", EntryPoint = "LeapRecordingWrite")]
    public static extern eLeapRS RecordingWrite(IntPtr pRecording, ref LEAP_TRACKING_EVENT pEvent, ref UInt64 pnBytesWritten);

    [DllImport("LeapC", EntryPoint = "LeapTelemetryProfiling")]
    public static extern eLeapRS LeapTelemetryProfiling(IntPtr hConnection, ref LEAP_TELEMETRY_DATA telemetryData);

    [DllImport("LeapC", EntryPoint = "LeapTelemetryGetNow")]
    public static extern UInt64 TelemetryGetNow();
  }//end LeapC
} //end LeapInternal namespace
