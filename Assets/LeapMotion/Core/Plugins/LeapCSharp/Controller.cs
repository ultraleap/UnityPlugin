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
  using System.Collections.Generic;
  using System.Text;
  using System.Threading;
  using System.Runtime.InteropServices;

  using LeapInternal;

  /**
   * The Controller class is your main interface to the Leap Motion Controller.
   *
   * Create an instance of this Controller class to access frames of tracking
   * data and configuration information. Frame data can be polled at any time
   * using the Controller::frame() function. Call frame() or frame(0) to get the
   * most recent frame. Set the history parameter to a positive integer to access
   * previous frames. A controller stores up to 60 frames in its frame history.
   *
   * Polling is an appropriate strategy for applications which already have an
   * intrinsic update loop, such as a game. You can also subscribe to the FrameReady
   * event to get tracking frames through an event delegate.
   *
   * If the current thread implements a SynchronizationContext that contains a message
   * loop, events are posted to that threads message loop. Otherwise, events are called
   * on an independent thread and applications must perform any needed synchronization
   * or marshalling of data between threads. Note that Unity3D does not create an
   * appropriate SynchronizationContext object. Typically, event handlers cannot access
   * any Unity objects.
   *
   * @since 1.0
   */
  public class Controller:
    IController
  {
    Connection _connection;
    bool _disposed = false;
    Config _config;

    /**
     * The SynchronizationContext used for dispatching events.
     *
     * By default the synchronization context of the thread creating the controller
     * instance is used. You can change the context if desired.
     */
    public SynchronizationContext EventContext
    {
      get
      {
        return _connection.EventContext;
      }
      set
      {
        _connection.EventContext = value;
      }
    }
    /**
     * Dispatched when the connection is initialized (but not necessarily connected).
     *
     * Can be dispatched more than once, if connection is restarted.
     * @since 3.0
     */
    public event EventHandler<LeapEventArgs> Init
    {
      add
      {
        if(_hasInitialized)
          value(this, new LeapEventArgs(LeapEvent.EVENT_INIT));
        _init += value;
      }
      remove{ _init -= value; }
    }
    private bool _hasInitialized = false;
    private EventHandler<LeapEventArgs> _init;
    /**
     * Dispatched when the connection to the service is established.
     * @since 3.0
     */
    public event EventHandler<ConnectionEventArgs> Connect
    {
      add
      {
        if(_hasConnected)
          value(this, new ConnectionEventArgs());
        _connect += value;
      }
      remove{ _connect -= value; }
    }
    private bool _hasConnected = false;
    private EventHandler<ConnectionEventArgs> _connect;
    /**
     * Dispatched if the connection to the service is lost.
     * @since 3.0
     */
    public event EventHandler<ConnectionLostEventArgs> Disconnect
    {
      add
      {
        _connection.LeapConnectionLost += value;
      }
      remove
      {
        _connection.LeapConnectionLost -= value;
      }
    }
    /**
     * Dispatched when a tracking frame is ready.
     * @since 3.0
     */
    public event EventHandler<FrameEventArgs> FrameReady
    {
      add
      {
        _connection.LeapFrame += value;
      }
      remove
      {
        _connection.LeapFrame -= value;
      }
    }
    /**
     * Dispatched when an internal tracking frame is ready.
     * @since 3.0
     */
    public event EventHandler<InternalFrameEventArgs> InternalFrameReady
    {
      add
      {
        _connection.LeapInternalFrame += value;
      }
      remove
      {
        _connection.LeapInternalFrame -= value;
      }
    }
    /**
     * Dispatched when a Leap Motion device is connected.
     * @since 3.0
     */
    public event EventHandler<DeviceEventArgs> Device
    {
      add
      {
        _connection.LeapDevice += value;
      }
      remove
      {
        _connection.LeapDevice -= value;
      }
    }
    /**
     * Dispatched when when a Leap Motion device is disconnected.
     * @since 3.0
     */
    public event EventHandler<DeviceEventArgs> DeviceLost
    {
      add
      {
        _connection.LeapDeviceLost += value;
      }
      remove
      {
        _connection.LeapDeviceLost -= value;
      }
    }
    /**
     * Dispatched when a Leap device fails to initialize.
     * @since 3.0
     */
    public event EventHandler<DeviceFailureEventArgs> DeviceFailure
    {
      add
      {
        _connection.LeapDeviceFailure += value;
      }
      remove
      {
        _connection.LeapDeviceFailure -= value;
      }
    }
    /**
     * Dispatched when the system generates a loggable event.
     * @since 3.0
     */
    public event EventHandler<LogEventArgs> LogMessage
    {
      add
      {
        _connection.LeapLogEvent += value;
      }
      remove
      {
        _connection.LeapLogEvent -= value;
      }
    }
    /**
     * Dispatched when a policy changes.
     * @since 3.0
     */
    public event EventHandler<PolicyEventArgs> PolicyChange
    {
      add
      {
        _connection.LeapPolicyChange += value;
      }
      remove
      {
        _connection.LeapPolicyChange -= value;
      }
    }
    /**
     * Dispatched when a configuration seting changes.
     * @since 3.0
     */
    public event EventHandler<ConfigChangeEventArgs> ConfigChange
    {
      add
      {
        _connection.LeapConfigChange += value;
      }
      remove
      {
        _connection.LeapConfigChange -= value;
      }
    }
    /**
     * Dispatched when the image distortion map changes.
     * The distortion map can change when the Leap device switches orientation,
     * or a new device becomes active.
     * @since 3.0
     */
    public event EventHandler<DistortionEventArgs> DistortionChange
    {
      add
      {
        _connection.LeapDistortionChange += value;
      }
      remove
      {
        _connection.LeapDistortionChange -= value;
      }
    }

    public event EventHandler<DroppedFrameEventArgs> DroppedFrame
    {
      add
      {
        _connection.LeapDroppedFrame += value;
      }
      remove
      {
        _connection.LeapDroppedFrame -= value;
      }
    }
    /**
     * Dispatched when an unrequested image is ready.
     * @since 4.0
     */
    public event EventHandler<ImageEventArgs> ImageReady
    {
      add
      {
        _connection.LeapImage += value;
      }
      remove
      {
        _connection.LeapImage -= value;
      }
    }

    public event Action<BeginProfilingForThreadArgs> BeginProfilingForThread 
    {
      add 
      {
        _connection.LeapBeginProfilingForThread += value;
      }
      remove 
      {
        _connection.LeapBeginProfilingForThread -= value;
      }
    }

    public event Action<EndProfilingForThreadArgs> EndProfilingForThread 
    {
      add 
      {
        _connection.LeapEndProfilingForThread += value;
      }
      remove 
      {
        _connection.LeapEndProfilingForThread -= value;
      }
    }

    public event Action<BeginProfilingBlockArgs> BeginProfilingBlock 
    {
      add 
      {
        _connection.LeapBeginProfilingBlock += value;
      }
      remove 
      {
        _connection.LeapBeginProfilingBlock -= value;
      }
    }

    public event Action<EndProfilingBlockArgs> EndProfilingBlock 
    {
      add 
      {
        _connection.LeapEndProfilingBlock += value;
      }
      remove 
      {
        _connection.LeapEndProfilingBlock -= value;
      }
    }

    /**
     * Dispatched when point mapping change events are generated by the service.
     * @since 4.0
     */
    public event EventHandler<PointMappingChangeEventArgs> PointMappingChange
    {
      add
      {
        _connection.LeapPointMappingChange += value;
      }
      remove
      {
        _connection.LeapPointMappingChange -= value;
      }
    }

    //TODO revisit dispose code
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
        // Free any other managed objects here.
      }

      // Free any unmanaged objects here.
      //
      _disposed = true;
    }

    /**
     * Constructs a Controller object.
     *
     * The default constructor uses a connection key of 0.
     *
     * @since 1.0
     */
    public Controller() : this(0)
    {
    }

    /**
     * Constructs a Controller object using the specified connection key.
     *
     * All controller instances using the same key will use the same connection
     * to the serivce. In general, an application should not use more than one connection
     * for all its controllers. Each connection keeps its own cache of frames and images.
     *
     * @param connectionKey An identifier specifying the connection to use. If a
     * connection with the specified key already exists, that connection is used.
     * Otherwise, a new connection is created.
     * @since 3.0
     */
    public Controller(int connectionKey)
    {
      _connection = Connection.GetConnection(connectionKey);
      _connection.EventContext = SynchronizationContext.Current;

      _connection.LeapInit += OnInit;
      _connection.LeapConnection += OnConnect;
      _connection.LeapConnectionLost += OnDisconnect;

      _connection.Start();
    }

    /**
     * Starts the connection.
     *
     * A connection starts automatically when created, but you can
     * use this function to restart the connection after stopping it.
     *
     * @since 3.0
     */
    public void StartConnection()
    {
      _connection.Start();
    }

    /**
     * Stops the connection.
     *
     * No more frames or other events are received from a stopped connection. You can
     * restart with StartConnection().
     *
     * @since 3.0
     */
    public void StopConnection()
    {
      _connection.Stop();
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
        return _connection.IsServiceConnected;
      }
    }

    /**
     * Requests setting a policy.
     *
     * A request to change a policy is subject to user approval and a policy
     * can be changed by the user at any time (using the Leap Motion settings dialog).
     * The desired policy flags must be set every time an application runs.
     *
     * Policy changes are completed asynchronously and, because they are subject
     * to user approval or system compatibility checks, may not complete successfully. Call
     * Controller::isPolicySet() after a suitable interval to test whether
     * the change was accepted.
     *
     * \include Controller_setPolicy.txt
     *
     * @param policy A PolicyFlag value indicating the policy to request.
     * @since 2.1.6
     */
    public void SetPolicy(Controller.PolicyFlag policy)
    {
      _connection.SetPolicy(policy);
    }

    /**
     * Requests clearing a policy.
     *
     * Policy changes are completed asynchronously and, because they are subject
     * to user approval or system compatibility checks, may not complete successfully. Call
     * Controller::isPolicySet() after a suitable interval to test whether
     * the change was accepted.
     *
     * \include Controller_clearPolicy.txt
     *
     * @param flags A PolicyFlag value indicating the policy to request.
     * @since 2.1.6
     */
    public void ClearPolicy(Controller.PolicyFlag policy)
    {
      _connection.ClearPolicy(policy);
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
      return _connection.IsPolicySet(policy);
    }

    /**
     * @if UNITY
     * In most cases you should get Frame objects using the LeapProvider::CurrentFrame
     * properties. The data in Frame objects taken directly from a Leap.Controller instance
     * is still in the Leap Motion frame of reference and will not match the hands
     * displayed in a Unity scene.
     * @endif
     *
     * Returns a frame of tracking data from the Leap Motion software. Use the optional
     * history parameter to specify which frame to retrieve. Call frame() or
     * frame(0) to access the most recent frame; call frame(1) to access the
     * previous frame, and so on. If you use a history value greater than the
     * number of stored frames, then the controller returns an empty frame.
     *
     * \include Controller_Frame_1.txt
     *
     * @param history The age of the frame to return, counting backwards from
     * the most recent frame (0) into the past and up to the maximum age (59).
     * @returns The specified frame; or, if no history parameter is specified,
     * the newest frame. If a frame is not available at the specified history
     * position, an invalid Frame is returned.
     * @since 1.0
     */
    public Frame Frame(int history)
    {
      Frame frame = new Frame();
      LEAP_TRACKING_EVENT trackingEvent;
      _connection.Frames.Get(out trackingEvent, history);
      frame.CopyFrom(ref trackingEvent);
      return frame;
    }

    /**
     * Identical to Frame(history) but instead of constructing a new frame and returning
     * it, the user provides a frame object to be filled with data instead.
     *
     * @param toFill The frame object to fill with tracking data.
     * @param history The age of the frame to return.
     */
    public void Frame(Frame toFill, int history)
    {
      LEAP_TRACKING_EVENT trackingEvent;
      _connection.Frames.Get(out trackingEvent, history);
      toFill.CopyFrom(ref trackingEvent);
    }

    /**
     * Returns the most recent frame of tracking data.
     *
     * @since 1.0
     */
    public Frame Frame()
    {
      return Frame(0);
    }

    /**
     * Fills a frame object with the most recent tracking data.
     *
     * @param toFill The frame object to fill with tracking data.
     */
    public void Frame(Frame toFill)
    {
      Frame(toFill, 0);
    }

    /**
     * Returns the timestamp of a recent tracking frame.  Use the
     * optional history parameter to specify how many frames in the past
     * to retrieve the timestamp.  Leave the history parameter as
     * it's default value to return the timestamp of the most recent
     * tracked frame.
     *
     * @param history the age of the timestamp to return,
     */
    public long FrameTimestamp(int history = 0)
    {
      LEAP_TRACKING_EVENT trackingEvent;
      _connection.Frames.Get(out trackingEvent, history);
      return trackingEvent.info.timestamp;
    }

    /**
     * Returns the frame object with all hands transformed by the specified
     * transform matrix.
     * @param trs a LeapTransform containing translation, rotation, and scale.
     * @param history The age of the frame to return, counting backwards from
     * the most recent frame (0) into the past and up to the maximum age (59).
     */
    public Frame GetTransformedFrame(LeapTransform trs, int history = 0)
    {
      return new Frame().CopyFrom(Frame(history)).Transform(trs);
    }

    /**
    * Returns the Frame at the specified time, interpolating the data between existing frames, if necessary.
     *
    */
    public Frame GetInterpolatedFrame(Int64 time)
    {
      return _connection.GetInterpolatedFrame(time);
    }

    public void GetInterpolatedFrame(Frame toFill, Int64 time)
    {
      _connection.GetInterpolatedFrame(toFill, time);
    }

    public void TelemetryProfiling(ref LEAP_TELEMETRY_DATA telemetryData) {
      _connection.TelemetryProfiling(ref telemetryData);
    }

    public UInt64 TelemetryGetNow()
    {
      return LeapC.TelemetryGetNow();
    }

    public void GetPointMapping(ref LEAP_POINT_MAPPING pointMapping) {
      _connection.GetPointMapping(ref pointMapping);
    }

    /**
     * This is a special variant of GetInterpolatedFrameFromTime, for use with special
     * features that only require the position and orientation of the palm positions, and do
     * not care about pose data or any other data.
     *
     * You must specify the id of the hand that you wish to get a transform for.  If you specify
     * an id that is not present in the interpolated frame, the output transform will be the
     * identity transform.
     */
    public void GetInterpolatedLeftRightTransform(Int64 time,
                                                  Int64 sourceTime,
                                                  int leftId,
                                                  int rightId,
                                              out LeapTransform leftTransform,
                                              out LeapTransform rightTransform)
    {
      _connection.GetInterpolatedLeftRightTransform(time, sourceTime, leftId, rightId, out leftTransform, out rightTransform);
    }

    public void GetInterpolatedFrameFromTime(Frame toFill, Int64 time, Int64 sourceTime)
    {
      _connection.GetInterpolatedFrameFromTime(toFill, time, sourceTime);
    }

    /**
     * Returns a timestamp value as close as possible to the current time.
     * Values are in microseconds, as with all the other timestamp values.
     *
     * @since 2.2.7
     *
     */
    public long Now()
    {
      return LeapC.GetNow();
    }

    /**
     * Reports whether this Controller is connected to the Leap Motion service and
     * the Leap Motion hardware is plugged in.
     *
     * When you first create a Controller object, isConnected() returns false.
     * After the controller finishes initializing and connects to the Leap Motion
     * software and if the Leap Motion hardware is plugged in, isConnected() returns true.
     *
     * You can either handle the onConnect event using a Listener instance or
     * poll the isConnected() function if you need to wait for your
     * application to be connected to the Leap Motion software before performing some other
     * operation.
     *
     * \include Controller_isConnected.txt
     * @returns True, if connected; false otherwise.
     * @since 1.0
     */
    public bool IsConnected
    {
      get
      {
        return IsServiceConnected && Devices.Count > 0;
      }
    }

    /**
     * Returns a Config object, which you can use to query the Leap Motion system for
     * configuration information.
     *
     * @returns The Controller's Config object.
     * @since 1.0
     */
    public Config Config
    {
      get
      {
        if (_config == null)
          _config = new Config(this._connection.ConnectionKey);
        return _config;
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
        return _connection.Devices;
      }
    }

    /**
     * A list of any Leap Motion hardware devices that are physically connected to
     * the client computer, but are not functioning correctly. The list contains
     * FailedDevice objects containing the pnpID and the reason for failure. No
     * other device information is available.
     *
     * \include Controller_failedDevices.txt
     *
     * @since 3.0
     */
    public FailedDeviceList FailedDevices()
    {
      return _connection.FailedDevices;
    }

    /**
     * The supported controller policies.
     *
     * The supported policy flags are:
     *
     * **POLICY_BACKGROUND_FRAMES** -- requests that your application receives frames
     *   when it is not the foreground application for user input.
     *
     *   The background frames policy determines whether an application
     *   receives frames of tracking data while in the background. By
     *   default, the Leap Motion  software only sends tracking data to the foreground application.
     *   Only applications that need this ability should request the background
     *   frames policy. The "Allow Background Apps" checkbox must be enabled in the
     *   Leap Motion Control Panel or this policy will be denied.
     *
     * **POLICY_OPTIMIZE_HMD** -- request that the tracking be optimized for head-mounted
     *   tracking.
     *
     *   The optimize HMD policy improves tracking in situations where the Leap
     *   Motion hardware is attached to a head-mounted display. This policy is
     *   not granted for devices that cannot be mounted to an HMD, such as
     *   Leap Motion controllers embedded in a laptop or keyboard.
     *
     * Some policies can be denied if the user has disabled the feature on
     * their Leap Motion control panel.
     *
     * @since 1.0
     */
    public enum PolicyFlag
    {
      /**
       * The default policy.
       * @since 1.0
       */
      POLICY_DEFAULT = 0,
      /**
       * Receive background frames.
       * @since 1.0
       */
      POLICY_BACKGROUND_FRAMES = (1 << 0),
      /**
       * Allow streaming images.
       * @since 4.0
       */
      POLICY_IMAGES = (1 << 1),
      /**
       * Optimize the tracking for head-mounted device.
       * @since 2.1.2
       */
      POLICY_OPTIMIZE_HMD = (1 << 2),
      /**
       * Allow pausing and unpausing of the Leap Motion service.
       * @since 3.0
       */
      POLICY_ALLOW_PAUSE_RESUME = (1 << 3),
      /**
       * Allow streaming map point
       * @since 4.0
       */
      POLICY_MAP_POINTS = (1 << 7),
    }

    protected virtual void OnInit(object sender, LeapEventArgs eventArgs)
    {
      _hasInitialized = true;
    }

    protected virtual void OnConnect(object sender, ConnectionEventArgs eventArgs)
    {
      _hasConnected = true;
    }

    protected virtual void OnDisconnect(object sender, ConnectionLostEventArgs eventArgs)
    {
      _hasInitialized = false;
      _hasConnected = false;
    }
  }
}
