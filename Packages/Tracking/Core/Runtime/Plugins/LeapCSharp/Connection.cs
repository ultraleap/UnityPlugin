/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

namespace LeapInternal
{
    using Leap;
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Threading;

    public class Connection
    {
        public struct Key
        {
            public readonly int connectionId;
            public readonly string serverNamespace;

            public Key(int connectionId, string serverNamespace = "Leap Service")
            {
                this.connectionId = connectionId;
                this.serverNamespace = serverNamespace;
            }
        }

        private static Dictionary<Key, Connection> connectionDictionary = new Dictionary<Key, Connection>();

        public static Connection GetConnection(int connectionId = 0)
        {
            return GetConnection(new Key(connectionId));
        }

        public static Connection GetConnection(Key connectionKey)
        {
            Connection conn;
            if (!Connection.connectionDictionary.TryGetValue(connectionKey, out conn))
            {
                conn = new Connection(connectionKey);
                connectionDictionary.Add(connectionKey, conn);
            }
            return conn;
        }

        //Left-right precalculated offsets
        private static long _handIdOffset;
        private static long _handPositionOffset;
        private static long _handOrientationOffset;

        static Connection()
        {
            _handIdOffset = Marshal.OffsetOf(typeof(LEAP_HAND), "id").ToInt64();

            long palmOffset = Marshal.OffsetOf(typeof(LEAP_HAND), "palm").ToInt64();
            _handPositionOffset = Marshal.OffsetOf(typeof(LEAP_PALM), "position").ToInt64() + palmOffset;
            _handOrientationOffset = Marshal.OffsetOf(typeof(LEAP_PALM), "orientation").ToInt64() + palmOffset;
        }

        public Key ConnectionKey { get; private set; }
        public CircularObjectBuffer<LEAP_TRACKING_EVENT> Frames { get; set; }

        private DeviceList _devices = new DeviceList();
        private FailedDeviceList _failedDevices;

        private DistortionData _currentLeftDistortionData = new DistortionData();
        private DistortionData _currentRightDistortionData = new DistortionData();
        private int _frameBufferLength = 60; //TODO, surface this value in LeapC, currently hardcoded!

        private IntPtr _leapConnection;
        private volatile bool _isRunning = false;
        private Thread _polster;

        /// <summary>
        /// Has the connection been set up in multi device aware mode
        /// </summary>
        private bool _multiDeviceAwareConnection = false;

        /// <summary>
        /// Minimum service version that support setting the tracking mode on a per dervice basis
        /// </summary>
        private static LEAP_VERSION MinServiceVersionForMultiModeSupport = new LEAP_VERSION() { major = 5, minor = 4, patch = 4 };

        //Policy and enabled features, indexed by device ID
        private Dictionary<uint, UInt64> _activePolicies = new Dictionary<uint, ulong>();

        //Config change status
        private Dictionary<uint, string> _configRequests = new Dictionary<uint, string>();

        //Connection events
        public SynchronizationContext EventContext { get; set; }

        private EventHandler<LeapEventArgs> _leapInit;
        public event EventHandler<LeapEventArgs> LeapInit
        {
            add
            {
                _leapInit += value;
                if (_leapConnection != IntPtr.Zero)
                    value(this, new LeapEventArgs(LeapEvent.EVENT_INIT));
            }
            remove { _leapInit -= value; }
        }

        private EventHandler<ConnectionEventArgs> _leapConnectionEvent;
        public event EventHandler<ConnectionEventArgs> LeapConnection
        {
            add
            {
                _leapConnectionEvent += value;
                if (IsServiceConnected)
                    value(this, new ConnectionEventArgs());
            }
            remove { _leapConnectionEvent -= value; }
        }
        public EventHandler<ConnectionLostEventArgs> LeapConnectionLost;
        public EventHandler<DeviceEventArgs> LeapDevice;
        public EventHandler<DeviceEventArgs> LeapDeviceLost;
        public EventHandler<DeviceFailureEventArgs> LeapDeviceFailure;
        public EventHandler<PolicyEventArgs> LeapPolicyChange;
        public EventHandler<FrameEventArgs> LeapFrame;
        public EventHandler<InternalFrameEventArgs> LeapInternalFrame;
        public EventHandler<LogEventArgs> LeapLogEvent;
        public EventHandler<SetConfigResponseEventArgs> LeapConfigResponse;
        public EventHandler<ConfigChangeEventArgs> LeapConfigChange;
        public EventHandler<DistortionEventArgs> LeapDistortionChange;
        public EventHandler<DroppedFrameEventArgs> LeapDroppedFrame;
        public EventHandler<ImageEventArgs> LeapImage;
        public EventHandler<PointMappingChangeEventArgs> LeapPointMappingChange;
        public EventHandler<HeadPoseEventArgs> LeapHeadPoseChange;

        public Action<BeginProfilingForThreadArgs> LeapBeginProfilingForThread;
        public Action<EndProfilingForThreadArgs> LeapEndProfilingForThread;
        public Action<BeginProfilingBlockArgs> LeapBeginProfilingBlock;
        public Action<EndProfilingBlockArgs> LeapEndProfilingBlock;

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
            _leapConnection = IntPtr.Zero;

            _disposed = true;
        }

        ~Connection()
        {
            Dispose(false);
        }

        private Connection(Key connectionKey)
        {
            ConnectionKey = connectionKey;
            _leapConnection = IntPtr.Zero;

            Frames = new CircularObjectBuffer<LEAP_TRACKING_EVENT>(_frameBufferLength);
        }

        private LEAP_ALLOCATOR _pLeapAllocator = new LEAP_ALLOCATOR();

        public void Start(string serverNamespace = "Leap Service", bool multiDeviceAware = true)
        {
            LEAP_CONNECTION_CONFIG config = new LEAP_CONNECTION_CONFIG();
            config.server_namespace = Marshal.StringToHGlobalAnsi(serverNamespace);
            config.flags = multiDeviceAware ? (uint)eLeapConnectionFlag.eLeapConnectionFlag_MultipleDevicesAware : 0;
            config.size = (uint)Marshal.SizeOf(config);
            _multiDeviceAwareConnection = multiDeviceAware;
            Start(config);
        }

        public void Start(LEAP_CONNECTION_CONFIG config)
        {
            if (_isRunning)
                return;

            eLeapRS result;
            if (_leapConnection == IntPtr.Zero)
            {
                if (ConnectionKey.serverNamespace == null)
                {
                    result = LeapC.CreateConnection(out _leapConnection);
                }
                else
                {
                    result = LeapC.CreateConnection(ref config, out _leapConnection);
                }

                if (result != eLeapRS.eLeapRS_Success || _leapConnection == IntPtr.Zero)
                {
                    reportAbnormalResults("LeapC CreateConnection call was ", result);
                    return;
                }
            }
            result = LeapC.OpenConnection(_leapConnection);
            if (result != eLeapRS.eLeapRS_Success)
            {
                reportAbnormalResults("LeapC OpenConnection call was ", result);
                return;
            }
            // The Allocator must persist the lifetime of the connection
            if (_pLeapAllocator.allocate == null)
            {
                _pLeapAllocator.allocate = MemoryManager.Pin;
            }
            if (_pLeapAllocator.deallocate == null)
            {
                _pLeapAllocator.deallocate = MemoryManager.Unpin;
            }
            LeapC.SetAllocator(_leapConnection, ref _pLeapAllocator);

            _isRunning = true;
            AppDomain.CurrentDomain.DomainUnload += (arg1, arg2) => Dispose(true);

            _polster = new Thread(new ThreadStart(this.processMessages));
            _polster.Name = "LeapC Worker";
            _polster.IsBackground = true;
            _polster.Start();
        }

        public void Stop()
        {
            if (!_isRunning)
                return;

            _isRunning = false;

            //Very important to close the connection before we try to join the
            //worker thread!  The call to PollConnection can sometimes block,
            //despite the timeout, causing an attempt to join the thread waiting
            //forever and preventing the connection from stopping.
            //
            //It seems that closing the connection causes PollConnection to 
            //unblock in these cases, so just make sure to close the connection
            //before trying to join the worker thread.
            LeapC.CloseConnection(_leapConnection);

            _polster.Join();
        }

        /// <summary>
        /// Returns the version of the currently installed Tracking Service. 
        /// Might return 0.0.0 if no device is connected or it cannot get the current version.
        /// </summary>
        /// <returns>the current tracking service version</returns>
        public LEAP_VERSION GetCurrentServiceVersion()
        {
            LEAP_VERSION currentVersion = new LEAP_VERSION { major = 0, minor = 0, patch = 0 };
            LeapC.GetVersion(_leapConnection, eLeapVersionPart.eLeapVersionPart_ServerLibrary, ref currentVersion);
            return currentVersion;
        }

        //Run in Polster thread, fills in object queues
        private void processMessages()
        {
            //Only profiling block currently is the Handle Event block
            const string HANDLE_EVENT_PROFILER_BLOCK = "Handle Event";
            bool hasBegunProfilingForThread = false;

            try
            {
                eLeapRS result;
                _leapInit.DispatchOnContext(this, EventContext, new LeapEventArgs(LeapEvent.EVENT_INIT));
                while (_isRunning)
                {
                    if (LeapBeginProfilingForThread != null && !hasBegunProfilingForThread)
                    {
                        LeapBeginProfilingForThread(new BeginProfilingForThreadArgs("Worker Thread",
                                                                                    HANDLE_EVENT_PROFILER_BLOCK));
                        hasBegunProfilingForThread = true;
                    }

                    LEAP_CONNECTION_MESSAGE _msg = new LEAP_CONNECTION_MESSAGE();
                    uint timeout = 150;

                    result = LeapC.PollConnection(_leapConnection, timeout, ref _msg);

                    if (result != eLeapRS.eLeapRS_Success)
                    {
                        reportAbnormalResults("LeapC PollConnection call was ", result);
                        continue;
                    }

                    if (LeapBeginProfilingBlock != null && hasBegunProfilingForThread)
                    {
                        LeapBeginProfilingBlock(new BeginProfilingBlockArgs(HANDLE_EVENT_PROFILER_BLOCK));
                    }

                    switch (_msg.type)
                    {
                        case eLeapEventType.eLeapEventType_None:
                            break;

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

                        // Note that unplugging a device generates an eLeapEventType_DeviceLost event
                        // message, not a failure message. DeviceLost is further down.
                        case eLeapEventType.eLeapEventType_DeviceFailure:
                            LEAP_DEVICE_FAILURE_EVENT device_failure_evt;
                            StructMarshal<LEAP_DEVICE_FAILURE_EVENT>.PtrToStruct(_msg.eventStructPtr, out device_failure_evt);
                            handleFailedDevice(ref device_failure_evt);
                            break;

                        case eLeapEventType.eLeapEventType_Policy:
                            LEAP_POLICY_EVENT policy_evt;
                            StructMarshal<LEAP_POLICY_EVENT>.PtrToStruct(_msg.eventStructPtr, out policy_evt);
                            handlePolicyChange(ref policy_evt, _msg.deviceID);
                            break;
                        case eLeapEventType.eLeapEventType_Tracking:
                            LEAP_TRACKING_EVENT tracking_evt;
                            StructMarshal<LEAP_TRACKING_EVENT>.PtrToStruct(_msg.eventStructPtr, out tracking_evt);
                            handleTrackingMessage(ref tracking_evt, _msg.deviceID);
                            break;
                        case eLeapEventType.eLeapEventType_LogEvent:
                            LEAP_LOG_EVENT log_evt;
                            StructMarshal<LEAP_LOG_EVENT>.PtrToStruct(_msg.eventStructPtr, out log_evt);
                            reportLogMessage(ref log_evt);
                            break;
                        case eLeapEventType.eLeapEventType_DeviceLost:
                            LEAP_DEVICE_EVENT device_lost_evt;
                            StructMarshal<LEAP_DEVICE_EVENT>.PtrToStruct(_msg.eventStructPtr, out device_lost_evt);
                            handleLostDevice(ref device_lost_evt);
                            break;
                        case eLeapEventType.eLeapEventType_ConfigChange:
                            LEAP_CONFIG_CHANGE_EVENT config_change_evt;
                            StructMarshal<LEAP_CONFIG_CHANGE_EVENT>.PtrToStruct(_msg.eventStructPtr, out config_change_evt);
                            handleConfigChange(ref config_change_evt);
                            break;
                        case eLeapEventType.eLeapEventType_ConfigResponse:
                            handleConfigResponse(ref _msg);
                            break;
                        case eLeapEventType.eLeapEventType_DroppedFrame:
                            LEAP_DROPPED_FRAME_EVENT dropped_frame_evt;
                            StructMarshal<LEAP_DROPPED_FRAME_EVENT>.PtrToStruct(_msg.eventStructPtr, out dropped_frame_evt);
                            handleDroppedFrame(ref dropped_frame_evt);
                            break;
                        case eLeapEventType.eLeapEventType_Image:
                            LEAP_IMAGE_EVENT image_evt;
                            StructMarshal<LEAP_IMAGE_EVENT>.PtrToStruct(_msg.eventStructPtr, out image_evt);
                            handleImage(ref image_evt, _msg.deviceID);
                            break;
                        case eLeapEventType.eLeapEventType_PointMappingChange:
                            LEAP_POINT_MAPPING_CHANGE_EVENT point_mapping_change_evt;
                            StructMarshal<LEAP_POINT_MAPPING_CHANGE_EVENT>.PtrToStruct(_msg.eventStructPtr, out point_mapping_change_evt);
                            handlePointMappingChange(ref point_mapping_change_evt);
                            break;
                        case eLeapEventType.eLeapEventType_HeadPose:
                            LEAP_HEAD_POSE_EVENT head_pose_event;
                            StructMarshal<LEAP_HEAD_POSE_EVENT>.PtrToStruct(_msg.eventStructPtr, out head_pose_event);
                            handleHeadPoseChange(ref head_pose_event);
                            break;
                        case eLeapEventType.eLeapEventType_DeviceStatusChange:
                            LEAP_DEVICE_STATUS_CHANGE_EVENT status_evt;
                            StructMarshal<LEAP_DEVICE_STATUS_CHANGE_EVENT>.PtrToStruct(_msg.eventStructPtr, out status_evt);
                            handleDeviceStatusEvent(ref status_evt);
                            break;
                    } //switch on _msg.type

                    if (LeapEndProfilingBlock != null && hasBegunProfilingForThread)
                    {
                        LeapEndProfilingBlock(new EndProfilingBlockArgs(HANDLE_EVENT_PROFILER_BLOCK));
                    }
                } //while running
            }
            catch (Exception e)
            {
                Logger.Log("Exception: " + e);
                _isRunning = false;
            }
            finally
            {
                if (LeapEndProfilingForThread != null && hasBegunProfilingForThread)
                {
                    LeapEndProfilingForThread(new EndProfilingForThreadArgs());
                }
            }
        }

        private void handleTrackingMessage(ref LEAP_TRACKING_EVENT trackingMsg, UInt32 deviceID)
        {
            Frames.Put(ref trackingMsg);

            if (LeapFrame != null)
            {
                LeapFrame.DispatchOnContext(this, EventContext, new FrameEventArgs(new Frame(deviceID).CopyFrom(ref trackingMsg)));
            }

            if (LeapInternalFrame != null)
            {
                LeapInternalFrame.DispatchOnContext(this, EventContext, new InternalFrameEventArgs(ref trackingMsg));
            }
        }


        public UInt64 GetInterpolatedFrameSize(Int64 time, Device device = null)
        {
            UInt64 size = 0;
            eLeapRS result;

            if (device != null)
            {
                result = LeapC.GetFrameSizeEx(_leapConnection, device.Handle, time, out size);
            }
            else
            {
                result = LeapC.GetFrameSize(_leapConnection, time, out size);
            }

            reportAbnormalResults("LeapC get interpolated frame call was ", result);
            return size;
        }



        public void GetInterpolatedFrame(Frame toFill, Int64 time, Device device = null)
        {
            UInt64 size = GetInterpolatedFrameSize(time, device);
            IntPtr trackingBuffer = Marshal.AllocHGlobal((Int32)size);
            eLeapRS result;

            if (device != null)
            {
                result = LeapC.InterpolateFrameEx(_leapConnection, device.Handle, time, trackingBuffer, size);
            }
            else
            {
                result = LeapC.InterpolateFrame(_leapConnection, time, trackingBuffer, size);
            }

            reportAbnormalResults("LeapC get interpolated frame call was ", result);
            if (result == eLeapRS.eLeapRS_Success)
            {
                LEAP_TRACKING_EVENT tracking_evt;
                StructMarshal<LEAP_TRACKING_EVENT>.PtrToStruct(trackingBuffer, out tracking_evt);
                toFill.CopyFrom(ref tracking_evt);
            }
            Marshal.FreeHGlobal(trackingBuffer);
        }

        public void GetInterpolatedFrameFromTime(Frame toFill, Int64 time, Int64 sourceTime, Device device = null)
        {
            UInt64 size = GetInterpolatedFrameSize(time, device);
            IntPtr trackingBuffer = Marshal.AllocHGlobal((Int32)size);
            eLeapRS result;

            if (device != null)
            {

                result = LeapC.InterpolateFrameFromTimeEx(_leapConnection, device.Handle, time, sourceTime, trackingBuffer, size);
            }
            else
            {
                result = LeapC.InterpolateFrameFromTime(_leapConnection, time, sourceTime, trackingBuffer, size);
            }

            reportAbnormalResults("LeapC get interpolated frame from time call was ", result);
            if (result == eLeapRS.eLeapRS_Success)
            {
                LEAP_TRACKING_EVENT tracking_evt;
                StructMarshal<LEAP_TRACKING_EVENT>.PtrToStruct(trackingBuffer, out tracking_evt);
                toFill.CopyFrom(ref tracking_evt);
            }
            Marshal.FreeHGlobal(trackingBuffer);
        }

        public Frame GetInterpolatedFrame(Int64 time, Device device = null)
        {
            Frame frame;
            if (device == null)
            {
                frame = new Frame();
            }
            else
            {
                frame = new Frame(device.DeviceID);
            }

            GetInterpolatedFrame(frame, time, device);
            return frame;
        }

        public void GetInterpolatedHeadPose(ref LEAP_HEAD_POSE_EVENT toFill, Int64 time)
        {
            eLeapRS result = LeapC.InterpolateHeadPose(_leapConnection, time, ref toFill);
            reportAbnormalResults("LeapC get interpolated head pose call was ", result);
        }

        public LEAP_HEAD_POSE_EVENT GetInterpolatedHeadPose(Int64 time)
        {
            LEAP_HEAD_POSE_EVENT headPoseEvent = new LEAP_HEAD_POSE_EVENT();
            GetInterpolatedHeadPose(ref headPoseEvent, time);
            return headPoseEvent;
        }

        public void GetInterpolatedLeftRightTransform(Int64 time,
                                                      Int64 sourceTime,
                                                      Int64 leftId,
                                                      Int64 rightId,
                                                      Device device,
                                                  out LeapTransform leftTransform,
                                                  out LeapTransform rightTransform)
        {
            leftTransform = LeapTransform.Identity;
            rightTransform = LeapTransform.Identity;

            UInt64 size = GetInterpolatedFrameSize(time, device);
            IntPtr trackingBuffer = Marshal.AllocHGlobal((Int32)size);
            eLeapRS result;

            if (device != null)
            {
                result = LeapC.InterpolateFrameFromTimeEx(_leapConnection, device.Handle, time, sourceTime, trackingBuffer, size);
            }
            else
            {
                result = LeapC.InterpolateFrameFromTime(_leapConnection, time, sourceTime, trackingBuffer, size);
            }

            reportAbnormalResults("LeapC get interpolated frame from time call was ", result);

            if (result == eLeapRS.eLeapRS_Success)
            {
                LEAP_TRACKING_EVENT tracking_evt;
                StructMarshal<LEAP_TRACKING_EVENT>.PtrToStruct(trackingBuffer, out tracking_evt);

                int id;
                LEAP_VECTOR position;
                LEAP_QUATERNION orientation;

                long handPtr = tracking_evt.pHands.ToInt64();
                long idPtr = handPtr + _handIdOffset;
                long posPtr = handPtr + _handPositionOffset;
                long rotPtr = handPtr + _handOrientationOffset;
                int stride = StructMarshal<LEAP_HAND>.Size;

                for (uint i = tracking_evt.nHands; i-- != 0; idPtr += stride, posPtr += stride, rotPtr += stride)
                {
                    id = Marshal.ReadInt32(new IntPtr(idPtr));
                    StructMarshal<LEAP_VECTOR>.PtrToStruct(new IntPtr(posPtr), out position);
                    StructMarshal<LEAP_QUATERNION>.PtrToStruct(new IntPtr(rotPtr), out orientation);

                    LeapTransform transform = new LeapTransform(position.ToVector3(), orientation.ToQuaternion());
                    if (id == leftId)
                    {
                        leftTransform = transform;
                    }
                    else if (id == rightId)
                    {
                        rightTransform = transform;
                    }
                }
            }

            Marshal.FreeHGlobal(trackingBuffer);

        }

        public void GetInterpolatedLeftRightTransform(Int64 time,
                                                      Int64 sourceTime,
                                                      Int64 leftId,
                                                      Int64 rightId,
                                                  out LeapTransform leftTransform,
                                                  out LeapTransform rightTransform)
        {
            GetInterpolatedLeftRightTransform(time, sourceTime, leftId, rightId, null, out leftTransform, out rightTransform);
        }

        private void handleConnection(ref LEAP_CONNECTION_EVENT connectionMsg)
        {
            if (_leapConnectionEvent != null)
            {
                _leapConnectionEvent.DispatchOnContext(this, EventContext, new ConnectionEventArgs());
            }
        }

        private void handleConnectionLost(ref LEAP_CONNECTION_LOST_EVENT connectionMsg)
        {
            if (LeapConnectionLost != null)
            {
                _devices.Clear();
                LeapConnectionLost.DispatchOnContext(this, EventContext, new ConnectionLostEventArgs());
            }
        }
        private void handleDeviceStatusEvent(ref LEAP_DEVICE_STATUS_CHANGE_EVENT statusEvent)
        {
            var device = _devices.FindDeviceByHandle(statusEvent.device.handle);
            if (device == null)
            {
                return;
            }

            device.UpdateStatus(statusEvent.status);
        }

        private void handleDevice(ref LEAP_DEVICE_EVENT deviceMsg)
        {
            IntPtr deviceHandle = deviceMsg.device.handle;
            if (deviceHandle == IntPtr.Zero)
                return;

            IntPtr connectionHandle = deviceMsg.device.handle;
            if (connectionHandle == IntPtr.Zero)
                return;

            LEAP_DEVICE_INFO deviceInfo = new LEAP_DEVICE_INFO();
            eLeapRS result;

            IntPtr device;
            result = LeapC.OpenDevice(deviceMsg.device, out device);
            if (result != eLeapRS.eLeapRS_Success)
                return;

            deviceInfo.serial = IntPtr.Zero;
            deviceInfo.size = (uint)Marshal.SizeOf(deviceInfo);
            result = LeapC.GetDeviceInfo(device, ref deviceInfo); //Query the serial length
            if (result != eLeapRS.eLeapRS_Success)
                return;

            deviceInfo.serial = Marshal.AllocCoTaskMem((int)deviceInfo.serial_length);
            result = LeapC.GetDeviceInfo(device, ref deviceInfo); //Query the serial

            if (result == eLeapRS.eLeapRS_Success)
            {
                Device apiDevice = new Device(device,
                                       deviceHandle,
                                       deviceInfo.h_fov, //radians
                                       deviceInfo.v_fov, //radians
                                       deviceInfo.range / 1000.0f, //to mm
                                       deviceInfo.baseline / 1000.0f, //to mm
                                       (Device.DeviceType)deviceInfo.type,
                                       deviceInfo.status == (uint)eLeapDeviceStatus.eLeapDeviceStatus_Streaming,
                                       deviceInfo.status,
                                       Marshal.PtrToStringAnsi(deviceInfo.serial),
                                       deviceMsg.device.id);

                Marshal.FreeCoTaskMem(deviceInfo.serial);
                _devices.AddOrUpdate(apiDevice);

                if (LeapDevice != null)
                {
                    LeapDevice.DispatchOnContext(this, EventContext, new DeviceEventArgs(apiDevice));
                }
            }
        }

        private void handleLostDevice(ref LEAP_DEVICE_EVENT deviceMsg)
        {
            IntPtr deviceHandle;
            eLeapRS result = LeapC.OpenDevice(deviceMsg.device, out deviceHandle);
            if (result != eLeapRS.eLeapRS_Success)
                return;

            //UnityEngine.Debug.Log("handleLostDevice: " + deviceHandle);
            Device lost = _devices.FindDeviceByHandle(deviceHandle);
            if (lost != null)
            {
                _devices.Remove(lost);

                if (_activePolicies.ContainsKey(deviceMsg.device.id))
                {
                    _activePolicies.Remove(deviceMsg.device.id);
                }

                if (LeapDeviceLost != null)
                {
                    LeapDeviceLost.DispatchOnContext(this, EventContext, new DeviceEventArgs(lost));
                }
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

            if (LeapDeviceFailure != null)
            {
                LeapDeviceFailure.DispatchOnContext(this, EventContext,
                  new DeviceFailureEventArgs((uint)deviceMsg.status, failureMessage, failedSerialNumber));
            }
        }

        private void handleConfigChange(ref LEAP_CONFIG_CHANGE_EVENT configEvent)
        {
            string config_key = "";
            _configRequests.TryGetValue(configEvent.requestId, out config_key);
            if (config_key != null)
                _configRequests.Remove(configEvent.requestId);
            if (LeapConfigChange != null)
            {
                LeapConfigChange.DispatchOnContext(this, EventContext,
                  new ConfigChangeEventArgs(config_key, configEvent.status != false, configEvent.requestId));
            }
        }

        private void handleConfigResponse(ref LEAP_CONNECTION_MESSAGE configMsg)
        {
            LEAP_CONFIG_RESPONSE_EVENT config_response_evt;
            StructMarshal<LEAP_CONFIG_RESPONSE_EVENT>.PtrToStruct(configMsg.eventStructPtr, out config_response_evt);
            string config_key = "";
            _configRequests.TryGetValue(config_response_evt.requestId, out config_key);
            if (config_key != null)
                _configRequests.Remove(config_response_evt.requestId);

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
            else
            {
                LEAP_CONFIG_RESPONSE_EVENT_WITH_REF_TYPE config_ref_value;
                StructMarshal<LEAP_CONFIG_RESPONSE_EVENT_WITH_REF_TYPE>.PtrToStruct(configMsg.eventStructPtr, out config_ref_value);
                dataType = Config.ValueType.TYPE_STRING;
                value = config_ref_value.value.stringValue;
            }
            SetConfigResponseEventArgs args = new SetConfigResponseEventArgs(config_key, dataType, value, requestId);

            if (LeapConfigResponse != null)
            {
                LeapConfigResponse.DispatchOnContext(this, EventContext, args);
            }
        }

        private void reportLogMessage(ref LEAP_LOG_EVENT logMsg)
        {
            if (LeapLogEvent != null)
            {
                LeapLogEvent.DispatchOnContext(this, EventContext, new LogEventArgs(publicSeverity(logMsg.severity), logMsg.timestamp, Marshal.PtrToStringAnsi(logMsg.message)));
            }
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

        private void handlePointMappingChange(ref LEAP_POINT_MAPPING_CHANGE_EVENT pointMapping)
        {
            if (LeapPointMappingChange != null)
            {
                LeapPointMappingChange.DispatchOnContext(this, EventContext, new PointMappingChangeEventArgs(pointMapping.frame_id, pointMapping.timestamp, pointMapping.nPoints));
            }
        }

        private void handleDroppedFrame(ref LEAP_DROPPED_FRAME_EVENT droppedFrame)
        {
            if (LeapDroppedFrame != null)
            {
                LeapDroppedFrame.DispatchOnContext(this, EventContext, new DroppedFrameEventArgs(droppedFrame.frame_id, droppedFrame.reason));
            }
        }

        private void handleHeadPoseChange(ref LEAP_HEAD_POSE_EVENT headPose)
        {
            if (LeapHeadPoseChange != null)
            {
                LeapHeadPoseChange.DispatchOnContext(this, EventContext, new HeadPoseEventArgs(headPose.head_position, headPose.head_orientation));
            }
        }

        private DistortionData createDistortionData(LEAP_IMAGE image, Image.CameraType camera)
        {
            DistortionData distortionData = new DistortionData();
            distortionData.Version = image.matrix_version;
            distortionData.Width = LeapC.DistortionSize; //fixed value for now
            distortionData.Height = LeapC.DistortionSize; //fixed value for now

            //Visit LeapC.h for more details.  We need to marshal the float data manually
            //since the distortion struct cannot be represented safely in c#
            distortionData.Data = new float[(int)(distortionData.Width * distortionData.Height * 2)]; //2 float values per map point
            Marshal.Copy(image.distortionMatrix, distortionData.Data, 0, distortionData.Data.Length);
            distortionData.OnDataChanged();

            if (LeapDistortionChange != null)
            {
                LeapDistortionChange.DispatchOnContext(this, EventContext, new DistortionEventArgs(distortionData, camera));
            }
            return distortionData;
        }

        private void handleImage(ref LEAP_IMAGE_EVENT imageMsg, UInt32 deviceID)
        {
            if (LeapImage != null)
            {
                //Update distortion data, if changed
                if ((_currentLeftDistortionData.Version != imageMsg.leftImage.matrix_version) || !_currentLeftDistortionData.IsValid)
                {
                    _currentLeftDistortionData = createDistortionData(imageMsg.leftImage, Image.CameraType.LEFT);
                }
                if ((_currentRightDistortionData.Version != imageMsg.rightImage.matrix_version) || !_currentRightDistortionData.IsValid)
                {
                    _currentRightDistortionData = createDistortionData(imageMsg.rightImage, Image.CameraType.RIGHT);
                }

                ImageData leftImage = new ImageData(Image.CameraType.LEFT, imageMsg.leftImage, _currentLeftDistortionData);
                ImageData rightImage = new ImageData(Image.CameraType.RIGHT, imageMsg.rightImage, _currentRightDistortionData);
                Image stereoImage = new Image(imageMsg.info.frame_id, imageMsg.info.timestamp, leftImage, rightImage, deviceID);
                LeapImage.DispatchOnContext(this, EventContext, new ImageEventArgs(stereoImage));
            }
        }

        private void handlePolicyChange(ref LEAP_POLICY_EVENT policyMsg, UInt32 deviceID)
        {
            // Avoid raising spurious policy change signals.
            if (_activePolicies.ContainsKey(deviceID))
            {
                if (policyMsg.current_policy == _activePolicies[deviceID])
                {
                    return;
                }
            }

            if (LeapPolicyChange != null)
            {
                if (_activePolicies.ContainsKey(deviceID))
                {
                    LeapPolicyChange.DispatchOnContext(this, EventContext,
                        new PolicyEventArgs(policyMsg.current_policy, _activePolicies[deviceID], true, _devices.FindDeviceByID(deviceID)));
                }
                else
                {
                    // We should get a policy flags event on device connection. This tells us the current policy. From our perspective we don't
                    // have a record of a 'previous' policy, so assume it's zero and raise an event
                    LeapPolicyChange.DispatchOnContext(this, EventContext,
                        new PolicyEventArgs(policyMsg.current_policy, 0, false, _devices.FindDeviceByID(deviceID)));
                }
            }

            _activePolicies[deviceID] = policyMsg.current_policy;
        }

        public void SetAndClearPolicy(Controller.PolicyFlag set, Controller.PolicyFlag clear, Device device = null)
        {
            UInt64 setFlags = (ulong)FlagForPolicy(set);
            UInt64 clearFlags = (ulong)FlagForPolicy(clear);
            eLeapRS result;

            if (device == null || !_multiDeviceAwareConnection)
            {
                result = LeapC.SetPolicyFlags(_leapConnection, setFlags, clearFlags);
            }
            else
            {
                if (!Controller.CheckRequiredServiceVersion(MinServiceVersionForMultiModeSupport, this))
                {
                    UnityEngine.Debug.LogWarning(String.Format("Your current tracking service does not support setting policy flags on a per device basis (min version is {0}.{1}.{2}). Please update your service: https://developer.leapmotion.com/tracking-software-download",
                        MinServiceVersionForMultiModeSupport.major,
                        MinServiceVersionForMultiModeSupport.minor,
                        MinServiceVersionForMultiModeSupport.patch));

                    return;
                }

                result = LeapC.SetPolicyFlagsEx(_leapConnection, device.Handle, setFlags, clearFlags);
            }

            reportAbnormalResults("LeapC SetAndClearPolicy call was ", result);
        }

        public void SetPolicy(Controller.PolicyFlag policy, Device device = null)
        {
            UInt64 setFlags = (ulong)FlagForPolicy(policy);

            eLeapRS result;

            if (device == null || !_multiDeviceAwareConnection)
            {
                result = LeapC.SetPolicyFlags(_leapConnection, setFlags, 0);
            }
            else
            {
                if (!Controller.CheckRequiredServiceVersion(MinServiceVersionForMultiModeSupport, this))
                {
                    UnityEngine.Debug.LogWarning(String.Format("Your current tracking service does not support setting policy flags on a per device basis (min version is {0}.{1}.{2}). Please update your service: https://developer.leapmotion.com/tracking-software-download",
                        MinServiceVersionForMultiModeSupport.major,
                        MinServiceVersionForMultiModeSupport.minor,
                        MinServiceVersionForMultiModeSupport.patch));

                    return;
                }

                result = LeapC.SetPolicyFlagsEx(_leapConnection, device.Handle, setFlags, 0);
            }

            reportAbnormalResults("LeapC SetPolicyFlags call was ", result);
        }

        public void ClearPolicy(Controller.PolicyFlag policy, Device device = null)
        {
            UInt64 clearFlags = (ulong)FlagForPolicy(policy);

            eLeapRS result;

            if (device == null || !_multiDeviceAwareConnection)
            {
                result = LeapC.SetPolicyFlags(_leapConnection, 0, clearFlags);
            }
            else
            {
                if (!Controller.CheckRequiredServiceVersion(MinServiceVersionForMultiModeSupport, this))
                {
                    UnityEngine.Debug.LogWarning(String.Format("Your current tracking service does not support clearing policy flags on a per device basis (min version is {0}.{1}.{2}). Please update your service: https://developer.leapmotion.com/tracking-software-download",
                        MinServiceVersionForMultiModeSupport.major,
                        MinServiceVersionForMultiModeSupport.minor,
                        MinServiceVersionForMultiModeSupport.patch));

                    return;
                }

                result = LeapC.SetPolicyFlagsEx(_leapConnection, device.Handle, 0, clearFlags);
            }

            reportAbnormalResults("LeapC SetPolicyFlags call was ", result);
        }

        static public eLeapPolicyFlag FlagForPolicy(Controller.PolicyFlag singlePolicy)
        {
            switch (singlePolicy)
            {
                case Controller.PolicyFlag.POLICY_BACKGROUND_FRAMES:
                    return eLeapPolicyFlag.eLeapPolicyFlag_BackgroundFrames;
                case Controller.PolicyFlag.POLICY_IMAGES:
                    return eLeapPolicyFlag.eLeapPolicyFlag_Images;
                case Controller.PolicyFlag.POLICY_OPTIMIZE_HMD:
                    return eLeapPolicyFlag.eLeapPolicyFlag_OptimizeHMD;
                case Controller.PolicyFlag.POLICY_ALLOW_PAUSE_RESUME:
                    return eLeapPolicyFlag.eLeapPolicyFlag_AllowPauseResume;
                case Controller.PolicyFlag.POLICY_MAP_POINTS:
                    return eLeapPolicyFlag.eLeapPolicyFlag_MapPoints;
                case Controller.PolicyFlag.POLICY_OPTIMIZE_SCREENTOP:
                    return eLeapPolicyFlag.eLeapPolicyFlag_ScreenTop;
                case Controller.PolicyFlag.POLICY_DEFAULT:
                    return 0;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Temporarily makes a connection to determine if a Service is available.
        /// Returns the result and closes the temporary connection upon completion.
        /// </summary>
        public static bool IsConnectionAvailable(string serverNamespace = "Leap Service")
        {
            LEAP_CONNECTION_CONFIG config = new LEAP_CONNECTION_CONFIG();
            config.server_namespace = Marshal.StringToHGlobalAnsi(serverNamespace);
            config.flags = 0;
            config.size = (uint)Marshal.SizeOf(config);

            IntPtr tempConnection;

            eLeapRS result;

            result = LeapC.CreateConnection(ref config, out tempConnection);

            if (result != eLeapRS.eLeapRS_Success || tempConnection == IntPtr.Zero)
            {
                LeapC.CloseConnection(tempConnection);
                return false;
            }

            result = LeapC.OpenConnection(tempConnection);

            if (result != eLeapRS.eLeapRS_Success)
            {
                LeapC.CloseConnection(tempConnection);
                return false;
            }

            LEAP_CONNECTION_MESSAGE _msg = new LEAP_CONNECTION_MESSAGE();
            uint timeout = 150;
            result = LeapC.PollConnection(tempConnection, timeout, ref _msg);

            LEAP_CONNECTION_INFO pInfo = new LEAP_CONNECTION_INFO();
            pInfo.size = (uint)Marshal.SizeOf(pInfo);
            result = LeapC.GetConnectionInfo(tempConnection, ref pInfo);

            if (pInfo.status == eLeapConnectionStatus.eLeapConnectionStatus_Connected)
            {
                LeapC.CloseConnection(tempConnection);
                return true;
            }

            LeapC.CloseConnection(tempConnection);

            return false;
        }

        /// <summary>
        /// Gets the active setting for a specific policy.
        ///
        /// Keep in mind that setting a policy flag is asynchronous, so changes are
        /// not effective immediately after calling setPolicyFlag(). In addition, a
        /// policy request can be declined by the user. You should always set the
        /// policy flags required by your application at startup and check that the
        /// policy change request was successful after an appropriate interval.
        ///
        /// If the controller object is not connected to the Leap Motion software, then the default
        /// state for the selected policy is returned.
        ///
        ///
        /// @since 2.1.6
        /// </summary>
        public bool IsPolicySet(Controller.PolicyFlag policy, Device device = null)
        {
            UInt64 policyToCheck = (ulong)FlagForPolicy(policy);

            uint deviceID = 0;
            if (device != null)
            {
                deviceID = device.DeviceID;
            }

            if (_activePolicies.ContainsKey(deviceID))
            {
                return (_activePolicies[deviceID] & policyToCheck) == policyToCheck;
            }
            else
            {
                Logger.Log("Warning: an attempt has been made to check whether a policy flag is set for an unknown device");
            }

            return false;
        }

        public bool IsDeviceAvailable(Device device = null)
        {
            uint deviceID = 0;
            if (device != null)
            {
                deviceID = device.DeviceID;
            }

            return _activePolicies.ContainsKey(deviceID);
        }

        public uint GetConfigValue(string config_key)
        {
            uint requestId = 0;
            eLeapRS result = LeapC.RequestConfigValue(_leapConnection, config_key, out requestId);
            reportAbnormalResults("LeapC RequestConfigValue call was ", result);
            _configRequests[requestId] = config_key;
            return requestId;
        }

        public uint SetConfigValue<T>(string config_key, T value) where T : IConvertible
        {
            uint requestId = 0;
            eLeapRS result;
            Type dataType = value.GetType();
            if (dataType == typeof(bool))
            {
                result = LeapC.SaveConfigValue(_leapConnection, config_key, Convert.ToBoolean(value), out requestId);
            }
            else if (dataType == typeof(Int32))
            {
                result = LeapC.SaveConfigValue(_leapConnection, config_key, Convert.ToInt32(value), out requestId);
            }
            else if (dataType == typeof(float))
            {
                result = LeapC.SaveConfigValue(_leapConnection, config_key, Convert.ToSingle(value), out requestId);
            }
            else if (dataType == typeof(string))
            {
                result = LeapC.SaveConfigValue(_leapConnection, config_key, Convert.ToString(value), out requestId);
            }
            else
            {
                throw new ArgumentException("Only boolean, Int32, float, and string types are supported.");
            }
            reportAbnormalResults("LeapC SaveConfigValue call was ", result);
            _configRequests[requestId] = config_key;
            return requestId;
        }

        /// <summary>
        /// Reports whether your application has a connection to the Leap Motion
        /// daemon/service. Can be true even if the Leap Motion hardware is not available.
        /// @since 1.2
        /// </summary>
        public bool IsServiceConnected
        {
            get
            {
                if (_leapConnection == IntPtr.Zero)
                    return false;

                LEAP_CONNECTION_INFO pInfo = new LEAP_CONNECTION_INFO();
                pInfo.size = (uint)Marshal.SizeOf(pInfo);
                eLeapRS result = LeapC.GetConnectionInfo(_leapConnection, ref pInfo);
                reportAbnormalResults("LeapC GetConnectionInfo call was ", result);

                if (pInfo.status == eLeapConnectionStatus.eLeapConnectionStatus_Connected)
                    return true;

                return false;
            }
        }

        /// <summary>
        /// The list of currently attached and recognized Leap Motion controller devices.
        ///
        /// The Device objects in the list describe information such as the range and
        /// tracking volume.
        ///
        ///
        /// Currently, the Leap Motion Controller only allows a single active device at a time,
        /// however there may be multiple devices physically attached and listed here.  Any active
        /// device(s) are guaranteed to be listed first, however order is not determined beyond that.
        ///
        /// @since 1.0
        /// </summary>
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

        /// <summary>
        /// Subscribes to the events coming from an individual device
        /// 
        /// If this is not called, only the primary device will be subscribed.
        /// Will automatically unsubscribe the primary device if this is called 
        /// on a secondary device, but not a primary one.  
        /// 
        /// @since 4.1
        /// </summary>
        public void SubscribeToDeviceEvents(Device device)
        {
            eLeapRS result = LeapC.LeapSubscribeEvents(_leapConnection, device.Handle);
            reportAbnormalResults("LeapC SubscribeEvents call was ", result);
        }

        /// <summary>
        /// Unsubscribes from the events coming from an individual device
        /// 
        /// This can be called safely, even if the device has not been subscribed.
        /// 
        /// @since 4.1
        /// </summary>
        public void UnsubscribeFromDeviceEvents(Device device)
        {
            eLeapRS result = LeapC.LeapUnsubscribeEvents(_leapConnection, device.Handle);
            reportAbnormalResults("LeapC UnsubscribeEvents call was ", result);
        }

        /// <summary>
        /// Converts from image-space pixel coordinates to camera-space rectilinear coordinates
        /// </summary>
        public UnityEngine.Vector3 PixelToRectilinear(Image.CameraType camera, UnityEngine.Vector3 pixel)
        {
            LEAP_VECTOR pixelStruct = new LEAP_VECTOR(pixel);
            LEAP_VECTOR ray = LeapC.LeapPixelToRectilinear(_leapConnection,
                   (camera == Image.CameraType.LEFT ?
                   eLeapPerspectiveType.eLeapPerspectiveType_stereo_left :
                   eLeapPerspectiveType.eLeapPerspectiveType_stereo_right),
                   pixelStruct);
            return new UnityEngine.Vector3(ray.x, ray.y, ray.z);
        }

        /// <summary>
        /// Converts from image-space pixel coordinates to camera-space rectilinear coordinates
        /// 
        /// Also allows specifying a specific device handle and calibration type.
        /// </summary>
        public UnityEngine.Vector3 PixelToRectilinearEx(IntPtr deviceHandle,
                                           Image.CameraType camera, Image.CalibrationType calibType, UnityEngine.Vector3 pixel)
        {
            LEAP_VECTOR pixelStruct = new LEAP_VECTOR(pixel);
            LEAP_VECTOR ray = LeapC.LeapPixelToRectilinearEx(_leapConnection,
                   deviceHandle,
                   (camera == Image.CameraType.LEFT ?
                   eLeapPerspectiveType.eLeapPerspectiveType_stereo_left :
                   eLeapPerspectiveType.eLeapPerspectiveType_stereo_right),
                   (calibType == Image.CalibrationType.INFRARED ?
                   eLeapCameraCalibrationType.eLeapCameraCalibrationType_infrared :
                   eLeapCameraCalibrationType.eLeapCameraCalibrationType_visual),
                   pixelStruct);
            return new UnityEngine.Vector3(ray.x, ray.y, ray.z);
        }

        /// <summary>
        /// Converts from camera-space rectilinear coordinates to image-space pixel coordinates
        /// </summary>
        public UnityEngine.Vector3 RectilinearToPixel(Image.CameraType camera, UnityEngine.Vector3 ray)
        {
            LEAP_VECTOR rayStruct = new LEAP_VECTOR(ray);
            LEAP_VECTOR pixel = LeapC.LeapRectilinearToPixel(_leapConnection,
                   (camera == Image.CameraType.LEFT ?
                   eLeapPerspectiveType.eLeapPerspectiveType_stereo_left :
                   eLeapPerspectiveType.eLeapPerspectiveType_stereo_right),
                   rayStruct);
            return new UnityEngine.Vector3(pixel.x, pixel.y, pixel.z);
        }

        /// <summary>
        /// Converts from camera-space rectilinear coordinates to image-space pixel coordinates
        /// 
        /// Also allows specifying a specific device handle and calibration type.
        /// </summary>
        public UnityEngine.Vector3 RectilinearToPixelEx(IntPtr deviceHandle,
                                           Image.CameraType camera, UnityEngine.Vector3 ray)
        {
            LEAP_VECTOR rayStruct = new LEAP_VECTOR(ray);
            LEAP_VECTOR pixel = LeapC.LeapRectilinearToPixelEx(_leapConnection,
                   deviceHandle,
                   (camera == Image.CameraType.LEFT ?
                   eLeapPerspectiveType.eLeapPerspectiveType_stereo_left :
                   eLeapPerspectiveType.eLeapPerspectiveType_stereo_right),
                   rayStruct);
            return new UnityEngine.Vector3(pixel.x, pixel.y, pixel.z);
        }

        public void TelemetryProfiling(ref LEAP_TELEMETRY_DATA telemetryData)
        {
            eLeapRS result = LeapC.LeapTelemetryProfiling(_leapConnection, ref telemetryData);
            reportAbnormalResults("LeapC TelemetryProfiling call was ", result);
        }

        public void GetPointMapping(ref PointMapping pm)
        {
            UInt64 size = 0;
            IntPtr buffer = IntPtr.Zero;
            while (true)
            {
                eLeapRS result = LeapC.GetPointMapping(_leapConnection, buffer, ref size);
                if (result == eLeapRS.eLeapRS_InsufficientBuffer)
                {
                    if (buffer != IntPtr.Zero)
                        Marshal.FreeHGlobal(buffer);
                    buffer = Marshal.AllocHGlobal((Int32)size);
                    continue;
                }
                reportAbnormalResults("LeapC get point mapping call was ", result);
                if (result != eLeapRS.eLeapRS_Success)
                {
                    pm.points = null;
                    pm.ids = null;
                    return;
                }
                break;
            }
            LEAP_POINT_MAPPING pmi;
            StructMarshal<LEAP_POINT_MAPPING>.PtrToStruct(buffer, out pmi);
            Int32 nPoints = (Int32)pmi.nPoints;

            pm.frameId = pmi.frame_id;
            pm.timestamp = pmi.timestamp;
            pm.points = new UnityEngine.Vector3[nPoints];
            pm.ids = new UInt32[nPoints];

            float[] points = new float[3 * nPoints];
            Int32[] ids = new Int32[nPoints];
            Marshal.Copy(pmi.points, points, 0, 3 * nPoints);
            Marshal.Copy(pmi.ids, ids, 0, nPoints);

            int j = 0;
            for (int i = 0; i < nPoints; i++)
            {
                pm.points[i].x = points[j++];
                pm.points[i].y = points[j++];
                pm.points[i].z = points[j++];
                pm.ids[i] = unchecked((UInt32)ids[i]);
            }
            Marshal.FreeHGlobal(buffer);
        }

        private eLeapRS _lastResult; //Used to avoid repeating the same log message, ie. for events like time out
        private void reportAbnormalResults(string context, eLeapRS result)
        {
            if (result != eLeapRS.eLeapRS_Success &&
               result != _lastResult)
            {
                string msg = context + " " + result;
                if (LeapLogEvent != null)
                {
                    LeapLogEvent.DispatchOnContext(this, EventContext,
                      new LogEventArgs(MessageSeverity.MESSAGE_CRITICAL,
                          LeapC.GetNow(),
                          msg));
                }
            }
            _lastResult = result;
        }
    }
}