/******************************************************************************\
* Copyright (C) 2012-2016 Leap Motion, Inc. All rights reserved.               *
* Leap Motion proprietary and confidential. Not for distribution.              *
* Use subject to the terms of the Leap Motion SDK Agreement available at       *
* https://developer.leapmotion.com/sdk_agreement, or another agreement         *
* between Leap Motion and you, your company or other organization.             *
\******************************************************************************/

namespace LeapInternal
{
  using System;
  using System.Collections.Generic;
  using System.Text;
  using System.Threading;
  using System.Runtime.InteropServices;

  using Leap;

  public class Connection
  {
    private static Dictionary<int, Connection> connectionDictionary = new Dictionary<int, Connection>();

    public static Connection GetConnection(int connectionKey = 0)
    {
      Connection conn;
      if (!Connection.connectionDictionary.TryGetValue(connectionKey, out conn))
      {
        conn = new Connection(connectionKey);
        connectionDictionary.Add(connectionKey, conn);
      }
      return conn;
    }

    public int ConnectionKey { get; private set; }
    public CircularObjectBuffer<Frame> Frames { get; set; }

    private ServiceFrameFactory frameFactory = new ServiceFrameFactory();
    private DeviceList _devices = new DeviceList();
    private FailedDeviceList _failedDevices;

    private PendingImages _pendingImageRequestList = new PendingImages();
    private ObjectPool<ImageData> _imageDataCache;
    private ObjectPool<ImageData> _imageRawDataCache;
    private int _frameBufferLength = 60;
    private int _imageBufferLength = 20 * 4;
    private ulong _standardImageBufferSize = 640 * 240 * 2; //width * heigth * 2
    private ulong _standardRawBufferSize = 640 * 240 * 2 * 8; //width * heigth * 2 images * 8 bpp
    private DistortionData _currentDistortionData = new DistortionData();

    private IntPtr _leapConnection;
    private bool _isRunning = false;
    private Thread _polster;

    //Policy and enabled features
    private UInt64 _requestedPolicies = 0;
    private UInt64 _activePolicies = 0;

    //Config change status
    private Dictionary<uint, string> _configRequests = new Dictionary<uint, string>();

    //Connection events
    private EventHandler<LeapEventArgs> _leapInit;
    public event EventHandler <LeapEventArgs> LeapInit{
      add{
        _leapInit += value;
        if(_leapConnection != IntPtr.Zero)
          value(this, new LeapEventArgs(LeapEvent.EVENT_INIT));
      }
      remove{_leapInit -= value;}
    }
    private EventHandler<ConnectionEventArgs> _leapConnectionEvent;
    public event EventHandler<ConnectionEventArgs> LeapConnection{
      add{
        _leapConnectionEvent += value;
        if(IsServiceConnected)
          value(this, new ConnectionEventArgs());
        }
      remove{_leapConnectionEvent -= value;}
    }
    public EventHandler<ConnectionLostEventArgs> LeapConnectionLost;
    public EventHandler<DeviceEventArgs> LeapDevice;
    public EventHandler<DeviceEventArgs> LeapDeviceLost;
    public EventHandler<DeviceFailureEventArgs> LeapDeviceFailure;
    public EventHandler<PolicyEventArgs> LeapPolicyChange;
    public EventHandler<FrameEventArgs> LeapFrame;
    public EventHandler<ImageEventArgs> LeapImageReady;
    public EventHandler<ImageRequestFailedEventArgs> LeapImageRequestFailed;
    public EventHandler<LogEventArgs> LeapLogEvent;
    public EventHandler<SetConfigResponseEventArgs> LeapConfigResponse;
    public EventHandler<ConfigChangeEventArgs> LeapConfigChange;
    public EventHandler<DistortionEventArgs> LeapDistortionChange;

    private bool _disposed = false;

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    // Protected implementation of Dispose pattern.
    protected virtual void Dispose(bool disposing)
    {
      if (_disposed)
        return;

      if (disposing)
      {
      }

      Stop();
      LeapC.DestroyConnection(_leapConnection);

      _disposed = true;
    }

    ~Connection()
    {
      Dispose(false);
    }

    private Connection(int connectionKey)
    {
      ConnectionKey = connectionKey;
      _leapConnection = IntPtr.Zero;

      Frames = new CircularObjectBuffer<Frame>(_frameBufferLength);
      _imageDataCache = new ObjectPool<ImageData>(_imageBufferLength, false);
      _imageRawDataCache = new ObjectPool<ImageData>(_imageBufferLength, false);
    }

    public void Start()
    {
      if (_isRunning)
        return;

      if (_leapConnection == IntPtr.Zero) {
        eLeapRS result = LeapC.CreateConnection(out _leapConnection);
        if (result != eLeapRS.eLeapRS_Success || _leapConnection == IntPtr.Zero) {
          reportAbnormalResults("LeapC CreateConnection call was ", result);
          return;
        }
        result = LeapC.OpenConnection(_leapConnection);
        if (result != eLeapRS.eLeapRS_Success) {
          reportAbnormalResults("LeapC OpenConnection call was ", result);
          return;
        }
      }

      _isRunning = true;

      _polster = new Thread(new ThreadStart(this.processMessages));
      _polster.IsBackground = true;
      _polster.Start();
    }

    public void Stop()
    {
      if (!_isRunning)
        return;

      _isRunning = false;
      _polster.Join();
    }

    //Run in Polster thread, fills in object queues
    private void processMessages()
    {
      try
      {
        eLeapRS result;
        _leapInit.Dispatch<LeapEventArgs>(this, new LeapEventArgs(LeapEvent.EVENT_INIT));
        while (_isRunning)
        {
          LEAP_CONNECTION_MESSAGE _msg = new LEAP_CONNECTION_MESSAGE();
          uint timeout = 1000;
          result = LeapC.PollConnection(_leapConnection, timeout, ref _msg);

          if (result != eLeapRS.eLeapRS_Success) {
            reportAbnormalResults("LeapC PollConnection call was ", result);
            continue;
          }

          switch (_msg.type)
          {
            case eLeapEventType.eLeapEventType_Connection:
              LEAP_CONNECTION_EVENT connection_evt;
              StructMarshal<LEAP_CONNECTION_EVENT>.PtrToStruct(_msg.eventStructPtr, out connection_evt);
              handleConnection(ref connection_evt);
              break;
            case eLeapEventType.eLeapEventType_ConnectionLost:
              LEAP_CONNECTION_LOST_EVENT connection_lost_evt;
              StructMarshal<LEAP_CONNECTION_LOST_EVENT>.PtrToStruct(_msg.eventStructPtr, out connection_lost_evt);
              handleConnectionLost(ref connection_lost_evt);
              break;
            case eLeapEventType.eLeapEventType_Device:
              LEAP_DEVICE_EVENT device_evt;
              StructMarshal<LEAP_DEVICE_EVENT>.PtrToStruct(_msg.eventStructPtr, out device_evt);
              handleDevice(ref device_evt);
              break;
            case eLeapEventType.eLeapEventType_DeviceLost:
              LEAP_DEVICE_EVENT device_lost_evt;
              StructMarshal<LEAP_DEVICE_EVENT>.PtrToStruct(_msg.eventStructPtr, out device_lost_evt);
              handleLostDevice(ref device_lost_evt);
              break;
            case eLeapEventType.eLeapEventType_DeviceFailure:
              LEAP_DEVICE_FAILURE_EVENT device_failure_evt;
              StructMarshal<LEAP_DEVICE_FAILURE_EVENT>.PtrToStruct(_msg.eventStructPtr, out device_failure_evt);
              handleFailedDevice(ref device_failure_evt);
              break;
            case eLeapEventType.eLeapEventType_Tracking:
              LEAP_TRACKING_EVENT tracking_evt;
              StructMarshal<LEAP_TRACKING_EVENT>.PtrToStruct(_msg.eventStructPtr, out tracking_evt);
              handleTrackingMessage(ref tracking_evt);
              break;
            case eLeapEventType.eLeapEventType_ImageComplete:
              LEAP_IMAGE_COMPLETE_EVENT image_complete_evt;
              StructMarshal<LEAP_IMAGE_COMPLETE_EVENT>.PtrToStruct(_msg.eventStructPtr, out image_complete_evt);
              handleImageCompletion(ref image_complete_evt);
              break;
            case eLeapEventType.eLeapEventType_ImageRequestError:
              LEAP_IMAGE_FRAME_REQUEST_ERROR_EVENT failed_image_evt;
              StructMarshal<LEAP_IMAGE_FRAME_REQUEST_ERROR_EVENT>.PtrToStruct(_msg.eventStructPtr, out failed_image_evt);
              handleFailedImageRequest(ref failed_image_evt);
              break;
            case eLeapEventType.eLeapEventType_LogEvent:
              LEAP_LOG_EVENT log_evt;
              StructMarshal<LEAP_LOG_EVENT>.PtrToStruct(_msg.eventStructPtr, out log_evt);
              reportLogMessage(ref log_evt);
              break;
            case eLeapEventType.eLeapEventType_PolicyChange:
              LEAP_POLICY_EVENT policy_evt;
              StructMarshal<LEAP_POLICY_EVENT>.PtrToStruct(_msg.eventStructPtr, out policy_evt);
              handlePolicyChange(ref policy_evt);
              break;
            case eLeapEventType.eLeapEventType_ConfigChange:
              LEAP_CONFIG_CHANGE_EVENT config_change_evt;
              StructMarshal<LEAP_CONFIG_CHANGE_EVENT>.PtrToStruct(_msg.eventStructPtr, out config_change_evt);
              handleConfigChange(ref config_change_evt);
              break;
            case eLeapEventType.eLeapEventType_ConfigResponse:
              handleConfigResponse(ref _msg);
              break;
            default:
              //discard unknown message types
              Logger.Log("Unhandled message type " + Enum.GetName(typeof(eLeapEventType), _msg.type));
              break;
          } //switch on _msg.type
        } //while running
      }
      catch (Exception e)
      {
        Logger.Log("Exception: " + e);
        _isRunning = false;
      }
    }

    private void handleTrackingMessage(ref LEAP_TRACKING_EVENT trackingMsg)
    {
      Frame newFrame = frameFactory.makeFrame(ref trackingMsg);
      Frames.Put(newFrame);
      this.LeapFrame.Dispatch<FrameEventArgs>(this, new FrameEventArgs(newFrame));
    }

    public UInt64 GetInterpolatedFrameSize(Int64 time){
      UInt64 size = 0;
      eLeapRS result = LeapC.GetFrameSize(_leapConnection, time, out size);
      reportAbnormalResults ("LeapC get interpolated frame call was ", result);
      return size;
    }

    public Frame GetInterpolatedFrame(Int64 time){
      UInt64 size = GetInterpolatedFrameSize(time);
      IntPtr trackingBuffer = Marshal.AllocHGlobal((Int32)size);
      eLeapRS result = LeapC.InterpolateFrame(_leapConnection, time, trackingBuffer, size);
      reportAbnormalResults ("LeapC get interpolated frame call was ", result);
      Frame frame = null;
      if(result == eLeapRS.eLeapRS_Success){
        LEAP_TRACKING_EVENT tracking_evt;
        StructMarshal<LEAP_TRACKING_EVENT>.PtrToStruct(trackingBuffer, out tracking_evt);
        frame = frameFactory.makeFrame(ref tracking_evt);
      }
      Marshal.FreeHGlobal(trackingBuffer);
      return frame;
    }

    public Image RequestImages(Int64 frameId, Image.ImageType imageType)
    {
      ImageData imageData;
      int bufferSize = 0;
      if (imageType == Image.ImageType.DEFAULT)
      {
        imageData = _imageDataCache.CheckOut();
        imageData.type = eLeapImageType.eLeapImageType_Default;
        bufferSize = (int)_standardImageBufferSize;
      }
      else {
        imageData = _imageRawDataCache.CheckOut();
        imageData.type = eLeapImageType.eLeapImageType_Raw;
        bufferSize = (int)_standardRawBufferSize;
      }
      if (imageData.pixelBuffer == null || imageData.pixelBuffer.Length != bufferSize)
      {
        imageData.pixelBuffer = new byte[bufferSize];
      }
      imageData.frame_id = frameId;
      return RequestImages(imageData);
    }

    public Image RequestImages(Int64 frameId, Image.ImageType imageType, byte[] buffer)
    {
      ImageData imageData = new ImageData();
      if (imageType == Image.ImageType.DEFAULT)
        imageData.type = eLeapImageType.eLeapImageType_Default;
      else
        imageData.type = eLeapImageType.eLeapImageType_Raw;

      imageData.frame_id = frameId;
      imageData.pixelBuffer = buffer;
      return RequestImages(imageData);
    }

    private Image RequestImages(ImageData imageData)
    {
      if (!_isRunning)
        return Image.Invalid;

      LEAP_IMAGE_FRAME_DESCRIPTION imageSpecifier = new LEAP_IMAGE_FRAME_DESCRIPTION();
      imageSpecifier.frame_id = imageData.frame_id;
      imageSpecifier.type = imageData.type;
      imageSpecifier.pBuffer = imageData.getPinnedHandle ();
      imageSpecifier.buffer_len = (ulong)imageData.pixelBuffer.LongLength;
      LEAP_IMAGE_FRAME_REQUEST_TOKEN token;
      eLeapRS result = eLeapRS.eLeapRS_UnknownError;

      result = LeapC.RequestImages (_leapConnection, ref imageSpecifier, out token);

      if (result == eLeapRS.eLeapRS_Success) {
        imageData.isComplete = false;
        imageData.index = token.requestID;
        Image futureImage = new Image (imageData);
        _pendingImageRequestList.Add(new ImageFuture (futureImage, imageData, LeapC.GetNow (), token));
        return futureImage;
      } else {
        imageData.unPinHandle ();
        reportAbnormalResults ("LeapC Image Request call was ", result);
        return Image.Invalid;
      }
    }

    private void handleImageCompletion(ref LEAP_IMAGE_COMPLETE_EVENT imageMsg)
    {
      LEAP_IMAGE_PROPERTIES props;
      StructMarshal<LEAP_IMAGE_PROPERTIES>.PtrToStruct(imageMsg.properties, out props);
      ImageFuture pendingImage = _pendingImageRequestList.FindAndRemove(imageMsg.token);

      if (pendingImage == null)
        return;

      //Update distortion data, if changed
      if ((_currentDistortionData.Version != imageMsg.matrix_version) || !_currentDistortionData.IsValid)
      {
        _currentDistortionData = new DistortionData();
        _currentDistortionData.Version = imageMsg.matrix_version;
        _currentDistortionData.Width = LeapC.DistortionSize; //fixed value for now
        _currentDistortionData.Height = LeapC.DistortionSize; //fixed value for now
        if (_currentDistortionData.Data == null || _currentDistortionData.Data.Length != (2 * _currentDistortionData.Width * _currentDistortionData.Height * 2))
          _currentDistortionData.Data = new float[(int)(2 * _currentDistortionData.Width * _currentDistortionData.Height * 2)]; //2 float values per map point
        LEAP_DISTORTION_MATRIX matrix;
        StructMarshal<LEAP_DISTORTION_MATRIX>.PtrToStruct(imageMsg.distortionMatrix, out matrix);
        Array.Copy(matrix.matrix_data, _currentDistortionData.Data, matrix.matrix_data.Length);
        this.LeapDistortionChange.Dispatch<DistortionEventArgs>(this, new DistortionEventArgs(_currentDistortionData));
      }

      pendingImage.imageData.CompleteImageData(props.type,
          props.format,
          props.bpp,
          props.width,
          props.height,
          imageMsg.info.timestamp,
          imageMsg.info.frame_id,
          props.x_offset,
          props.y_offset,
          props.x_scale,
          props.y_scale,
          _currentDistortionData,
          LeapC.DistortionSize,
          imageMsg.matrix_version);

      Image completedImage = pendingImage.imageObject;
      this.LeapImageReady.Dispatch<ImageEventArgs>(this, new ImageEventArgs(completedImage));
    }

    private void handleFailedImageRequest(ref LEAP_IMAGE_FRAME_REQUEST_ERROR_EVENT failed_image_evt)
    {
       ImageFuture pendingImage =_pendingImageRequestList.FindAndRemove(failed_image_evt.token);

      if (pendingImage != null) {
        pendingImage.imageData.CheckIn ();

        ImageRequestFailedEventArgs errorEventArgs = new ImageRequestFailedEventArgs (failed_image_evt.description.frame_id, pendingImage.imageObject.Type);
        switch (failed_image_evt.error)
        {
          case eLeapImageRequestError.eLeapImageRequestError_InsufficientBuffer:
            errorEventArgs.message = "The buffer specified for the request was too small.";
            errorEventArgs.reason = Image.RequestFailureReason.Insufficient_Buffer;
            if (failed_image_evt.description.type == eLeapImageType.eLeapImageType_Default && _standardImageBufferSize < failed_image_evt.required_buffer_len)
              _standardImageBufferSize = failed_image_evt.required_buffer_len;
            else if (failed_image_evt.description.type == eLeapImageType.eLeapImageType_Raw && _standardRawBufferSize < failed_image_evt.required_buffer_len)
              _standardRawBufferSize = failed_image_evt.required_buffer_len;
            break;
          case eLeapImageRequestError.eLeapImageRequestError_Unavailable:
            errorEventArgs.message = "The image was request too late and is no longer available.";
            errorEventArgs.reason = Image.RequestFailureReason.Image_Unavailable;
            break;
          case eLeapImageRequestError.eLeapImageRequestError_ImagesDisabled:
            errorEventArgs.message = "Images are disabled by the current configuration settings.";
            errorEventArgs.reason = Image.RequestFailureReason.Images_Disabled;
            break;
          default:
            errorEventArgs.message = "The image request failed for an undetermined reason.";
            errorEventArgs.reason = Image.RequestFailureReason.Unknown_Error;
            break;
        }
        errorEventArgs.requiredBufferSize = (long)failed_image_evt.required_buffer_len;

        this.LeapImageRequestFailed.Dispatch<ImageRequestFailedEventArgs> (this, errorEventArgs);
      }
      _pendingImageRequestList.purgeOld(_leapConnection);
    }

    private void handleConnection(ref LEAP_CONNECTION_EVENT connectionMsg)
    {
      this._leapConnectionEvent.Dispatch<ConnectionEventArgs>(this, new ConnectionEventArgs());
    }

    private void handleConnectionLost(ref LEAP_CONNECTION_LOST_EVENT connectionMsg)
    {
      this.LeapConnectionLost.Dispatch<ConnectionLostEventArgs>(this, new ConnectionLostEventArgs());
    }

    private void handleDevice(ref LEAP_DEVICE_EVENT deviceMsg)
    {
      IntPtr deviceHandle = deviceMsg.device.handle;
      if (deviceHandle == IntPtr.Zero)
        return;

      LEAP_DEVICE_INFO deviceInfo = new LEAP_DEVICE_INFO();
      eLeapRS result;

      IntPtr device;
      result = LeapC.OpenDevice(deviceMsg.device, out device);
      uint defaultLength = 14;
      deviceInfo.serial_length = defaultLength;
      deviceInfo.serial = Marshal.AllocCoTaskMem((int)defaultLength);
      deviceInfo.size = (uint)Marshal.SizeOf(deviceInfo);
      result = LeapC.GetDeviceInfo(device, out deviceInfo);

      if (result == eLeapRS.eLeapRS_InsufficientBuffer)
      {
        Marshal.FreeCoTaskMem(deviceInfo.serial);
        deviceInfo.serial = Marshal.AllocCoTaskMem((int)deviceInfo.serial_length);
        deviceInfo.size = (uint)Marshal.SizeOf(deviceInfo);
        result = LeapC.GetDeviceInfo(deviceHandle, out deviceInfo);
      }

      if (result == eLeapRS.eLeapRS_Success)
      {
        Device apiDevice = new Device(deviceHandle,
                               deviceInfo.h_fov, //radians
                               deviceInfo.v_fov, //radians
                               deviceInfo.range / 1000, //to mm
                               deviceInfo.baseline / 1000, //to mm
                               (deviceInfo.caps == (UInt32)eLeapDeviceCaps.eLeapDeviceCaps_Embedded),
                               (deviceInfo.status == (UInt32)eLeapDeviceStatus.eLeapDeviceStatus_Streaming),
                               Marshal.PtrToStringAnsi(deviceInfo.serial));
        Marshal.FreeCoTaskMem(deviceInfo.serial);
        _devices.AddOrUpdate(apiDevice);
        this.LeapDevice.Dispatch(this, new DeviceEventArgs(apiDevice));
      }
    }

    private void handleLostDevice(ref LEAP_DEVICE_EVENT deviceMsg)
    {
      Device lost = _devices.FindDeviceByHandle(deviceMsg.device.handle);
      if (lost != null)
      {
        _devices.Remove(lost);
        this.LeapDeviceLost.Dispatch(this, new DeviceEventArgs(lost));
      }
    }

    private void handleFailedDevice(ref LEAP_DEVICE_FAILURE_EVENT deviceMsg)
    {
      string failureMessage;
      string failedSerialNumber = "Unavailable";
      switch (deviceMsg.status)
      {
        case eLeapDeviceStatus.eLeapDeviceStatus_BadCalibration:
          failureMessage = "Bad Calibration. Device failed because of a bad calibration record.";
          break;
        case eLeapDeviceStatus.eLeapDeviceStatus_BadControl:
          failureMessage = "Bad Control Interface. Device failed because of a USB control interface error.";
          break;
        case eLeapDeviceStatus.eLeapDeviceStatus_BadFirmware:
          failureMessage = "Bad Firmware. Device failed because of a firmware error.";
          break;
        case eLeapDeviceStatus.eLeapDeviceStatus_BadTransport:
          failureMessage = "Bad Transport. Device failed because of a USB communication error.";
          break;
        default:
          failureMessage = "Device failed for an unknown reason";
          break;
      }
      Device failed = _devices.FindDeviceByHandle(deviceMsg.hDevice);
      if (failed != null)
      {
        _devices.Remove(failed);
      }

      this.LeapDeviceFailure.Dispatch<DeviceFailureEventArgs>(this,
          new DeviceFailureEventArgs((uint)deviceMsg.status, failureMessage, failedSerialNumber));
    }

    private void handleConfigChange(ref LEAP_CONFIG_CHANGE_EVENT configEvent)
    {
      string config_key = "";
      _configRequests.TryGetValue(configEvent.requestId, out config_key);
      if (config_key != null)
        _configRequests.Remove(configEvent.requestId);
      this.LeapConfigChange.Dispatch<ConfigChangeEventArgs>(this,
          new ConfigChangeEventArgs(config_key, configEvent.status != 0, configEvent.requestId));
    }

    private void handleConfigResponse(ref LEAP_CONNECTION_MESSAGE configMsg)
    {
      LEAP_CONFIG_RESPONSE_EVENT config_response_evt;
      StructMarshal<LEAP_CONFIG_RESPONSE_EVENT>.PtrToStruct(configMsg.eventStructPtr, out config_response_evt);
      string config_key = "";
      _configRequests.TryGetValue(config_response_evt.requestId, out config_key);
      if (config_key != null)
        _configRequests.Remove (config_response_evt.requestId);

      Config.ValueType dataType;
      object value;
      uint requestId = config_response_evt.requestId;
      if (config_response_evt.value.type != eLeapValueType.eLeapValueType_String)
      {

        switch (config_response_evt.value.type)
        {
          case eLeapValueType.eLeapValueType_Boolean:
            dataType = Config.ValueType.TYPE_BOOLEAN;
            value = config_response_evt.value.boolValue;
            break;
          case eLeapValueType.eLeapValueType_Int32:
            dataType = Config.ValueType.TYPE_INT32;
            value = config_response_evt.value.intValue;
            break;
          case eLeapValueType.eLeapValueType_Float:
            dataType = Config.ValueType.TYPE_FLOAT;
            value = config_response_evt.value.floatValue;
            break;
          default:
            dataType = Config.ValueType.TYPE_UNKNOWN;
            value = new object();
            break;
        }
      }
      else {
        LEAP_CONFIG_RESPONSE_EVENT_WITH_REF_TYPE config_ref_value;
        StructMarshal<LEAP_CONFIG_RESPONSE_EVENT_WITH_REF_TYPE>.PtrToStruct(configMsg.eventStructPtr, out config_ref_value);
        dataType = Config.ValueType.TYPE_STRING;
        value = config_ref_value.value.stringValue;
      }
      SetConfigResponseEventArgs args = new SetConfigResponseEventArgs(config_key, dataType, value, requestId);

      this.LeapConfigResponse.Dispatch<SetConfigResponseEventArgs>(this, args);
    }

    private void reportLogMessage(ref LEAP_LOG_EVENT logMsg)
    {
      this.LeapLogEvent.Dispatch<LogEventArgs>(this, new LogEventArgs(publicSeverity(logMsg.severity), logMsg.timestamp, logMsg.message));
    }

    private MessageSeverity publicSeverity(eLeapLogSeverity leapCSeverity)
    {
      switch (leapCSeverity)
      {
        case eLeapLogSeverity.eLeapLogSeverity_Unknown:
          return MessageSeverity.MESSAGE_UNKNOWN;
        case eLeapLogSeverity.eLeapLogSeverity_Information:
          return MessageSeverity.MESSAGE_INFORMATION;
        case eLeapLogSeverity.eLeapLogSeverity_Warning:
          return MessageSeverity.MESSAGE_WARNING;
        case eLeapLogSeverity.eLeapLogSeverity_Critical:
          return MessageSeverity.MESSAGE_CRITICAL;
        default:
          return MessageSeverity.MESSAGE_UNKNOWN;
      }
    }

    private void handlePolicyChange(ref LEAP_POLICY_EVENT policyMsg)
    {
      this.LeapPolicyChange.Dispatch<PolicyEventArgs>(this, new PolicyEventArgs(policyMsg.current_policy, _activePolicies));

      _activePolicies = policyMsg.current_policy;

      if (_activePolicies != _requestedPolicies)
      {
        // This could happen when config is turned off, or
        // this is the policy change event from the last SetPolicy, after that, the user called SetPolicy again
        //TODO handle failure to set desired policy -- maybe a PolicyDenied event
      }
    }

    public void SetPolicy(Controller.PolicyFlag policy)
  {
      UInt64 setFlags = (ulong)flagForPolicy(policy);
      _requestedPolicies = _requestedPolicies | setFlags;
      setFlags = _requestedPolicies;
      UInt64 clearFlags = ~_requestedPolicies; //inverse of desired policies

      eLeapRS result = eLeapRS.eLeapRS_UnknownError;
      result = LeapC.SetPolicyFlags(_leapConnection, setFlags, clearFlags);
      reportAbnormalResults("LeapC SetPolicyFlags call was ", result);
    }

    public void ClearPolicy(Controller.PolicyFlag policy)
    {
      UInt64 clearFlags = (ulong)flagForPolicy(policy);
      _requestedPolicies = _requestedPolicies & ~clearFlags;
      eLeapRS result = eLeapRS.eLeapRS_UnknownError;
      result = LeapC.SetPolicyFlags(_leapConnection, 0, clearFlags);
      reportAbnormalResults("LeapC SetPolicyFlags call was ", result);
    }

    private eLeapPolicyFlag flagForPolicy(Controller.PolicyFlag singlePolicy)
    {
      switch (singlePolicy)
      {
        case Controller.PolicyFlag.POLICY_BACKGROUND_FRAMES:
          return eLeapPolicyFlag.eLeapPolicyFlag_BackgroundFrames;
        case Controller.PolicyFlag.POLICY_OPTIMIZE_HMD:
          return eLeapPolicyFlag.eLeapPolicyFlag_OptimizeHMD;
        case Controller.PolicyFlag.POLICY_DEFAULT:
          return 0;
        default:
          return 0;
      }
    }

    /**
     * Gets the active setting for a specific policy.
     *
     * Keep in mind that setting a policy flag is asynchronous, so changes are
     * not effective immediately after calling setPolicyFlag(). In addition, a
     * policy request can be declined by the user. You should always set the
     * policy flags required by your application at startup and check that the
     * policy change request was successful after an appropriate interval.
     *
     * If the controller object is not connected to the Leap Motion software, then the default
     * state for the selected policy is returned.
     *
     * \include Controller_isPolicySet.txt
     *
     * @param flags A PolicyFlag value indicating the policy to query.
     * @returns A boolean indicating whether the specified policy has been set.
     * @since 2.1.6
     */
    public bool IsPolicySet(Controller.PolicyFlag policy)
    {
      UInt64 policyToCheck = (ulong)flagForPolicy(policy);
      return (_activePolicies & policyToCheck) == policyToCheck;
    }

    public uint GetConfigValue(string config_key)
    {
      uint requestId = 0;
      eLeapRS result = LeapC.RequestConfigValue (_leapConnection, config_key, out requestId);
      reportAbnormalResults ("LeapC RequestConfigValue call was ", result);
      _configRequests [requestId] = config_key;
      return requestId;
    }

    public uint SetConfigValue<T> (string config_key, T value) where T : IConvertible
    {
      uint requestId = 0;
      eLeapRS result;
      Type dataType = value.GetType ();
      if (dataType == typeof(bool)) {
        result = LeapC.SaveConfigValue (_leapConnection, config_key, Convert.ToBoolean (value), out requestId);
      } else if (dataType == typeof(Int32)) {
        result = LeapC.SaveConfigValue (_leapConnection, config_key, Convert.ToInt32 (value), out requestId);
      } else if (dataType == typeof(float)) {
        result = LeapC.SaveConfigValue (_leapConnection, config_key, Convert.ToSingle (value), out requestId);
      } else if (dataType == typeof(string)) {
        result = LeapC.SaveConfigValue (_leapConnection, config_key, Convert.ToString (value), out requestId);
      } else {
        throw new ArgumentException ("Only boolean, Int32, float, and string types are supported.");
      }
      reportAbnormalResults ("LeapC SaveConfigValue call was ", result);
      _configRequests [requestId] = config_key;
      return requestId;
    }

    /**
     * Reports whether your application has a connection to the Leap Motion
     * daemon/service. Can be true even if the Leap Motion hardware is not available.
     * @since 1.2
     */
    public bool IsServiceConnected
    {
      get
      {
        if (_leapConnection == IntPtr.Zero)
          return false;

        LEAP_CONNECTION_INFO pInfo = new LEAP_CONNECTION_INFO();
        pInfo.size = (uint)Marshal.SizeOf (pInfo);
        eLeapRS result = LeapC.GetConnectionInfo(_leapConnection, out pInfo);
        reportAbnormalResults("LeapC GetConnectionInfo call was ", result);

        if (pInfo.status == eLeapConnectionStatus.eLeapConnectionStatus_Connected)
          return true;

        return false;
      }
    }

    /**
     * The list of currently attached and recognized Leap Motion controller devices.
     *
     * The Device objects in the list describe information such as the range and
     * tracking volume.
     *
     * \include Controller_devices.txt
     *
     * Currently, the Leap Motion Controller only allows a single active device at a time,
     * however there may be multiple devices physically attached and listed here.  Any active
     * device(s) are guaranteed to be listed first, however order is not determined beyond that.
     *
     * @returns The list of Leap Motion controllers.
     * @since 1.0
     */
    public DeviceList Devices
    {
      get
      {
        if (_devices == null)
        {
          _devices = new DeviceList();
        }

        return _devices;
      }
    }

    public FailedDeviceList FailedDevices
    {
      get
      {
        if (_failedDevices == null)
        {
          _failedDevices = new FailedDeviceList();
        }

        return _failedDevices;
      }
    }

    private eLeapRS _lastResult; //Used to avoid repeating the same log message, ie. for events like time out
    private void reportAbnormalResults(string context, eLeapRS result)
    {
      if (result != eLeapRS.eLeapRS_Success &&
         result != _lastResult)
      {
        string msg = context + " " + result;
        this.LeapLogEvent.Dispatch<LogEventArgs>(this,
            new LogEventArgs(MessageSeverity.MESSAGE_CRITICAL,
                LeapC.GetNow(),
                msg)
        );
      }
      _lastResult = result;
    }
  }
}
