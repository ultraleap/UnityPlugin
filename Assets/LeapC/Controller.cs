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
    public SynchronizationContext EventContext { get; set; }
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
    public event EventHandler<ConnectionLostEventArgs> Disconnect;
    /**
     * Dispatched when a tracking frame is ready.
     * @since 3.0
     */
    public event EventHandler<FrameEventArgs> FrameReady;
    /**
    * Dispatched when a Leap Motion device is connected.
    * @since 3.0
    */
    public event EventHandler<DeviceEventArgs> Device;
    /**
    * Dispatched when when a Leap Motion device is disconnected.
    * @since 3.0
    */
    public event EventHandler<DeviceEventArgs> DeviceLost;
    /**
    * Dispatched when an image is ready. Call Controller.RequestImage()
    * to request that an image be sent to your application.
    * @since 3.0
    */
    public event EventHandler<ImageEventArgs> ImageReady;
    /**
    * Dispatched when a requested image cannot be sent.
    * @since 3.0
    */
    public event EventHandler<ImageRequestFailedEventArgs> ImageRequestFailed;
    /**
    * Dispatched when a Leap device fails to initialize.
    * @since 3.0
    */
    public event EventHandler<DeviceFailureEventArgs> DeviceFailure;
    /**
    * Dispatched when the system generates a loggable event.
    * @since 3.0
    */
    public event EventHandler<LogEventArgs> LogMessage;
    /**
    * Dispatched when a policy changes.
    * @since 3.0
    */
    public event EventHandler<PolicyEventArgs> PolicyChange;
    /**
    * Dispatched when a configuration seting changes.
    * @since 3.0
    */
    public event EventHandler<ConfigChangeEventArgs> ConfigChange;
    /**
    * Dispatched when the image distortion map changes.
    * The distortion map can change when the Leap device switches orientation,
    * or a new device becomes active.
    * @since 3.0
    */
    public event EventHandler<DistortionEventArgs> DistortionChange;

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
      EventContext = SynchronizationContext.Current;
      _connection = Connection.GetConnection(connectionKey);

      _connection.LeapInit += OnInit;
      _connection.LeapConnection += OnConnect;
      _connection.LeapConnectionLost += OnDisconnect;
      _connection.LeapFrame += OnFrame;
      _connection.LeapImageReady += OnImages;
      _connection.LeapImageRequestFailed += OnFailedImageRequest;
      _connection.LeapPolicyChange += OnPolicyChange;
      _connection.LeapLogEvent += OnLogEvent;
      _connection.LeapConfigChange += OnConfigChange;
      _connection.LeapDevice += OnDevice;
      _connection.LeapDeviceLost += OnDeviceLost;
      _connection.LeapDeviceFailure += OnDeviceFailure;
      _connection.LeapDistortionChange += OnDistortionChange;

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
     * Requests an image pair from the service.
     *
     * Image data is stacked in a single byte array, with the left camera image first, followed by the right camera image.
     * Two types of image are supported. ImageType.TYPE_DEFAULT is the normal, IR format image.
     * ImageType.RAW is the unmodified, raw sensor pixels. The format of this image type depends on the device. For
     * the publically available Leap Motion devices, the raw image type is also IR and is identical to the default image type
     * except when in robust mode. In robust mode, the default image type is processed to subtract the unilluminated background;
     * the raw image is not.
     *
     * Images are not sent automatically. You must request each image. This function returns an Image
     * object. However, that object does not contain image data until its IsComplete property
     * becomes true.
     *
     * Image requests will fail if the request is made after the image has been disposed by the service. The service only
     * keeps images for a few frames, so applications that use images must make the image request as soon as possible
     * after a tracking frame is received.
     *
     * The controller dispatches an ImageReady event when a requested image is recevied from the service
     * and is ready for use. The controller dispatches an ImageRequestFailed event if the request does not succeed.
     *
     * Image requests can fail for the following reasons:
     *
     * * The requested image is no longer available.
     * * The frame id does not match an actual tracking frame.
     * * Images are disabled by the client's configuration settings.
     * * The buffer supplied for the image was too small.
     * * An internal service error occurs.
     *
     * In addition, if the returned image is invalid, then the request call itself failed. Typically this
     * will occur when the connection itself is not running. Such errors are reported in a LogEvent event.
     *
     * @param frameId The Id value of the tracking frame.
     * @param type The type of image desired. A member of the Image.ImageType enumeration.
     * @returns An incomplete Image object that will contain the image data when the request is fulfilled.
     * If the request call itself fails, an invalid image is returned.
     * @since 3.0
     */
    public Image RequestImages(Int64 frameId, Image.ImageType type)
    {
      return _connection.RequestImages(frameId, type);
    }

    /**
     * Requests an image from the service. The pixels of the image are written to the specified byte array.
     *
     * If the specified byte array is too small, an ImageRequestFailed event is dispatched. The arguments of this event
     * include the required buffer size. For the publically available Leap device, the buffer size must be:
     * width * height * bytes-per-pixel * #cameras, i.e: 640 * 240 * 1 * 2 = 307,200 bytes.
     *
     * The Image object returned by this function contains a reference to the supplied buffer. When the service sets
     * the IsComplete property to true when the buffer is filled in. An ImageReady event is also dispatched.
     *
     * @param frameId The Id value of the tracking frame.
     * @param type The type of image desired. A member of the Image.ImageType enumeration.
     * @param imageBuffer A byte array large enough to hold the requested image pair.
     * @returns An incomplete Image object that will contain the image data when the request is fulfilled.
     * If the request call itself fails, an invalid image is returned.
     * @since 3.0
     */
    public Image RequestImages(Int64 frameId, Image.ImageType type, byte[] imageBuffer)
    {
      return _connection.RequestImages(frameId, type, imageBuffer);
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
      return _connection.Frames.Get(history);
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
     * Returns the frame object with all hands transformed by the specified
     * transform matrix.
     * @param trs a LeapTransform containing translation, rotation, and scale.
     * @param history The age of the frame to return, counting backwards from
     * the most recent frame (0) into the past and up to the maximum age (59).
     */
    public Frame GetTransformedFrame(LeapTransform trs, int history = 0)
    {
      return Frame(history).TransformedCopy(trs);
    }

    /**
    * Returns the Frame at the specified time, interpolating the data between existing frames, if necessary.
    * 
    * 
    */
    public Frame GetInterpolatedFrame(Int64 time){
      return _connection.GetInterpolatedFrame(time);
    }

    /**
    * Returns the Frame at the specified time, interpolating the data between existing frames, if necessary,
    * and transforms the data using the specified transform matrix.
    * 
    * 
    */
    public Frame GetTransformedInterpolatedFrame(LeapTransform trs, Int64 time){
      return _connection.GetInterpolatedFrame(time).TransformedCopy(trs);
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
       * Optimize the tracking for head-mounted device.
       * @since 2.1.2
       */
      POLICY_OPTIMIZE_HMD = (1 << 2),
      /**
      * Allow pausing and unpausing of the Leap Motion service.
      * @since 3.0
      */
      POLICY_ALLOW_PAUSE_RESUME = (1 << 3),
    }

    protected virtual void OnInit(object sender, LeapEventArgs eventArgs)
    {
      _hasInitialized = true;
      _init.DispatchOnContext<LeapEventArgs>(this, EventContext, eventArgs);
    }

    protected virtual void OnConnect(object sender, ConnectionEventArgs eventArgs)
    {
      _hasConnected = true;
      _connect.DispatchOnContext<ConnectionEventArgs>(this, EventContext, eventArgs);
    }

    protected virtual void OnDisconnect(object sender, ConnectionLostEventArgs eventArgs)
    {
      _hasInitialized = false;
      _hasConnected = false;
      Disconnect.DispatchOnContext<ConnectionLostEventArgs>(this, EventContext, eventArgs);
    }

    protected virtual void OnDevice(object sender, DeviceEventArgs eventArgs)
    {
      Device.DispatchOnContext<DeviceEventArgs>(this, EventContext, eventArgs);
    }

    protected virtual void OnDeviceLost(object sender, DeviceEventArgs eventArgs)
    {
      DeviceLost.DispatchOnContext<DeviceEventArgs>(this, EventContext, eventArgs);
    }

    //Rebroadcasted events from connection
    protected virtual void OnFrame(object sender, FrameEventArgs eventArgs)
    {
      FrameReady.DispatchOnContext<FrameEventArgs>(this, EventContext, eventArgs);
    }

    protected virtual void OnImages(object sender, ImageEventArgs eventArgs)
    {
      ImageReady.DispatchOnContext<ImageEventArgs>(this, EventContext, eventArgs);
    }

    protected virtual void OnFailedImageRequest(object sender, ImageRequestFailedEventArgs eventArgs)
    {
      ImageRequestFailed.DispatchOnContext<ImageRequestFailedEventArgs>(this, EventContext, eventArgs);
    }

    protected virtual void OnDistortionChange(object sender, DistortionEventArgs eventArgs)
    {
      DistortionChange.DispatchOnContext<DistortionEventArgs>(this, EventContext, eventArgs);
    }

    protected virtual void OnLogEvent(object sender, LogEventArgs eventArgs)
    {
      LogMessage.DispatchOnContext<LogEventArgs>(this, EventContext, eventArgs);
    }

    protected virtual void OnPolicyChange(object sender, PolicyEventArgs eventArgs)
    {
      PolicyChange.DispatchOnContext<PolicyEventArgs>(this, EventContext, eventArgs);
    }

    protected virtual void OnConfigChange(object sender, ConfigChangeEventArgs eventArgs)
    {
      ConfigChange.DispatchOnContext<ConfigChangeEventArgs>(this, EventContext, eventArgs);
    }

    protected virtual void OnDeviceFailure(object sender, DeviceFailureEventArgs eventArgs)
    {
      DeviceFailure.DispatchOnContext<DeviceFailureEventArgs>(this, EventContext, eventArgs);
    }
  }
}
