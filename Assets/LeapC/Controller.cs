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

    using System.IO;

    //debugging

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
   * intrinsic update loop, such as a game. You can also add an instance of a
   * subclass of Leap::Listener to the controller to handle events as they occur.
   * The Controller dispatches events to the listener upon initialization and exiting,
   * on connection changes, when the application gains and loses the OS input focus,
   * and when a new frame of tracking data is available.
   * When these events occur, the controller object invokes the appropriate
   * callback function defined in your subclass of Listener.
   *
   * To access frames of tracking data as they become available:
   *
   * 1. Implement a subclass of the Listener class and override the
   *    Listener::onFrame() function.
   * 2. In your Listener::onFrame() function, call the Controller::frame()
   *    function to access the newest frame of tracking data.
   * 3. To start receiving frames, create a Controller object and add an instance
   *    of the Listener subclass to the Controller::addListener() function.
   *
   * When an instance of a Listener subclass is added to a Controller object,
   * it calls the Listener::onInit() function when the listener is ready for use.
   * When a connection is established between the controller and the Leap Motion software,
   * the controller calls the Listener::onConnect() function. At this point, your
   * application will start receiving frames of data. The controller calls the
   * Listener::onFrame() function each time a new frame is available. If the
   * controller loses its connection with the Leap Motion software or device for any
   * reason, it calls the Listener::onDisconnect() function. If the listener is
   * removed from the controller or the controller is destroyed, it calls the
   * Listener::onExit() function. At that point, unless the listener is added to
   * another controller again, it will no longer receive frames of tracking data.
   *
   * The Controller object is multithreaded and calls the Listener functions on
   * its own thread, not on an application thread.
   * @since 1.0
   */

    public class Controller : IController
    {
        Connection _connection;
        bool _disposed = false;
        Config _config;

        public SynchronizationContext EventContext{ get; set; }

        public event EventHandler<LeapEventArgs> Init;
        public event EventHandler<ConnectionEventArgs> Connect;
        public event EventHandler<ConnectionLostEventArgs> Disconnect;
        public event EventHandler<LeapEventArgs> Exit;
        public event EventHandler<FrameEventArgs> FrameReady;
        public event EventHandler<LeapEventArgs> FocusGained;
        public event EventHandler<LeapEventArgs> FocusLost;
        public event EventHandler<LeapEventArgs> ServiceConnect;
        public event EventHandler<LeapEventArgs> ServiceDisconnect;
        public event EventHandler<DeviceEventArgs> Device;
        public event EventHandler<DeviceEventArgs> DeviceLost;
        public event EventHandler<ImageEventArgs> ImageReady;
        public event EventHandler<ImageRequestFailedEventArgs> ImageRequestFailed;
        public event EventHandler<LeapEventArgs> ServiceChange;
        public event EventHandler<DeviceFailureEventArgs> DeviceFailure;
        public event EventHandler<LogEventArgs> LogMessage;
        public event EventHandler<PolicyEventArgs> PolicyChange;
        public event EventHandler<ConfigChangeEventArgs> ConfigChange;
        public event EventHandler<DistortionEventArgs> DistortionChange;
        public event EventHandler<TrackedQuadEventArgs> TrackedQuadReady;

        //TODO revisit dispose code
        public void Dispose ()
        { 
            Dispose (true);
            GC.SuppressFinalize (this);
        }
        
        // Protected implementation of Dispose pattern.
        protected virtual void Dispose (bool disposing)
        {
            if (_disposed)
                return; 
            
            if (disposing) {
                // Free any other managed objects here.
            }
            
            // Free any unmanaged objects here.
            //
            _disposed = true;
        }

        /**
     * Constructs a Controller object.
     *
     * When creating a Controller object, you may optionally pass in a
     * reference to an instance of a subclass of Leap::Listener. Alternatively,
     * you may add a listener using the Controller::addListener() function.
     *
     * @since 1.0
     */
        public Controller () : this (0)
        {
        }

        public Controller (int connectionKey)
        {
            EventContext = SynchronizationContext.Current;
            _connection = Connection.GetConnection (connectionKey);

            _connection.LeapInit += OnInit;
            _connection.LeapConnection += OnConnect;
            _connection.LeapConnectionLost += OnDisconnect;
            _connection.LeapFrame += OnFrame;
            _connection.LeapImageReady += OnImages;
            _connection.LeapImageRequestFailed += OnFailedImageRequest;
            _connection.LeapPolicyChange += OnPolicyChange;
            _connection.LeapLogEvent += OnLogEvent;
            _connection.LeapTrackedQuad += OnTrackedQuad;
            _connection.LeapConfigChange += OnConfigChange;
            _connection.LeapDevice += OnDevice;
            _connection.LeapDeviceLost += OnDeviceLost;
            _connection.LeapDeviceFailure += OnDeviceFailure;
            _connection.LeapDistortionChange += OnDistortionChange;

            _connection.Start ();
        }

        public void StartConnection ()
        {
            _connection.Start ();
        }

        public void StopConnection ()
        {
            _connection.Stop ();
        }

        public Image RequestImages(Int64 frameId, Image.ImageType type){
            return _connection.RequestImages(frameId, type);
        }
        public Image RequestImages(Int64 frameId, Image.ImageType type, byte[] imageBuffer){
            return _connection.RequestImages(frameId, type, imageBuffer);
        }

        /**
     * Reports whether your application has a connection to the Leap Motion
     * daemon/service. Can be true even if the Leap Motion hardware is not available.
     * @since 1.2
     */
        public bool IsServiceConnected {
            get {
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
        public void SetPolicy (Controller.PolicyFlag policy)
        {
            _connection.SetPolicy (policy);
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
        public void ClearPolicy (Controller.PolicyFlag policy)
        {
            _connection.ClearPolicy (policy);
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
        public bool IsPolicySet (Controller.PolicyFlag policy)
        {
            return _connection.IsPolicySet (policy);
        }


        /**
     * Returns a frame of tracking data from the Leap Motion software. Use the optional
     * history parameter to specify which frame to retrieve. Call frame() or
     * frame(0) to access the most recent frame; call frame(1) to access the
     * previous frame, and so on. If you use a history value greater than the
     * number of stored frames, then the controller returns an invalid frame.
     *
     * \include Controller_Frame_1.txt
     *
     * You can call this function in your Listener implementation to get frames at the
     * Leap Motion frame rate:
     *
     * \include Controller_Listener_onFrame.txt
     * 
     * @param history The age of the frame to return, counting backwards from
     * the most recent frame (0) into the past and up to the maximum age (59).
     * @returns The specified frame; or, if no history parameter is specified,
     * the newest frame. If a frame is not available at the specified history
     * position, an invalid Frame is returned.
     * @since 1.0
     */
        public Frame Frame (int history)
        {
            return _connection.Frames.Get (history);
        }

        /**
     * Returns a frame of tracking data from the Leap Motion software. Use the optional
     * history parameter to specify which frame to retrieve. Call frame() or
     * frame(0) to access the most recent frame; call frame(1) to access the
     * previous frame, and so on. If you use a history value greater than the
     * number of stored frames, then the controller returns an invalid frame.
     *
     * \include Controller_Frame_1.txt
     *
     * You can call this function in your Listener implementation to get frames at the
     * Leap Motion frame rate:
     *
     * \include Controller_Listener_onFrame.txt
     * 
     * @param history The age of the frame to return, counting backwards from
     * the most recent frame (0) into the past and up to the maximum age (59).
     * @returns The specified frame; or, if no history parameter is specified,
     * the newest frame. If a frame is not available at the specified history
     * position, an invalid Frame is returned.
     * @since 1.0
     */
        public Frame Frame ()
        {
            return Frame (0);
        }

        /**
         * Returns the frame object with all hands transformed by the specified
         * transform matrix.
         * @param trs a Matrix containing translation, rotation, and scale.
         * @param history The age of the frame to return, counting backwards from
         * the most recent frame (0) into the past and up to the maximum age (59).
         */
        public Frame GetTransformedFrame (Matrix trs, int history = 0)
        {
            return Frame (history).TransformedCopy (trs);
        }


        /**
     * Returns a timestamp value as close as possible to the current time.
     * Values are in microseconds, as with all the other timestamp values.
     *
     * @since 2.2.7
     *
     */
        public long Now ()
        {
            return LeapC.GetNow ();
        }

        /**
     * Pauses or resumes the Leap Motion service.
     *
     * When the service is paused no applications receive tracking data and the
     * service itself uses minimal CPU time.
     *
     * Before changing the state of the service, you must set the
     * POLICY_ALLOW_PAUSE_RESUME using the Controller::setPolicy() function.
     * Policies must be set every time the application is run.
     *
     * \include Controller_setPaused.txt
     *
     * @param pause Set true to pause the service; false to resume.
     * @since 2.4.0
     */
//        public void SetPaused (bool pause)
//        {
//            _connection.SetPaused (pause);
//        }

        /**
     * Reports whether the Leap Motion service is currently paused.
     *
     * \include Controller_isPaused.txt
     *
     * @returns True, if the service is paused; false, otherwise.
     * @since 2.4.0
     */
//        public bool IsPaused ()
//        {
//            return _connection.IsPaused;
//        }

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
        public bool IsConnected {
            get {
                return IsServiceConnected && Devices.Count > 0;
            } 
        }

        //TODO how is LeapC going to handle focus?
        /**
     * Reports whether this application is the focused, foreground application.
     *
     * By default, your application only receives tracking information from
     * the Leap Motion controller when it has the operating system input focus.
     * To receive tracking data when your application is in the background,
     * the background frames policy flag must be set.
     *
     * \include Controller_hasFocus.txt
     *
     * @returns True, if application has focus; false otherwise.
     *
     * @see Controller::setPolicyFlags()
     * @since 1.0
     */
        public bool HasFocus {
            get {
                return false;
            } 
        }

        /**
     * Returns a Config object, which you can use to query the Leap Motion system for
     * configuration information.
     *
     * \include Controller_config.txt
     *
     * @returns The Controller's Config object.
     * @since 1.0
     */  
        public Config Config {
            get {
                if (_config == null)
                    _config = new Config (this._connection.ConnectionKey);
                return _config;
            } 
        }

        /**
     * The most recent set of images from the Leap Motion cameras.
     *
     * \include Controller_images.txt
     *
     * Depending on timing and the current processing frame rate, the images
     * obtained with this function can be newer than images obtained from
     * the current frame of tracking data.
     *
     * @return An ImageList object containing the most recent camera images.
     * @since 2.2.1
     */
//        public ImageList Images {
//            get {
//                if (_images == null)
//                    _images = new ImageList ();
//
//                _connection.GetLatestImages (ref _images);
//                return _images;
//            } 
//        }

        public enum ImageRequestResult{
            UNKNOWN = 0,
            SUCCESS,
            REQUEST_TOO_LATE,
            INSUFFICIENT_BUFFER,
            REQUEST_FAILED,
        }
        //Images on demand
//        public Image RequestDefaultImages(Int64 frameId, IntPtr buffer, out int bufferSize){
//            return _connection.RequestImages(frameId, buffer, out bufferSize, Image.ImageType.DEFAULT);
//        }
//        public ImageRequestResult RequestRawImages(Int64 frameId, IntPtr buffer, out int bufferSize){
//            return _connection.RequestImages(frameId, buffer, out bufferSize, Image.ImageType.RAW);
//        }
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
        public DeviceList Devices {
            get {
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
    * @since 2.4.0
    */
        public FailedDeviceList FailedDevices ()
        {
            return _connection.FailedDevices;
        }

        /**
     * Note: This class is an experimental API for internal use only. It may be
     * removed without warning.
     *
     * Returns information about the currently detected quad in the scene.
     *
     * \include Controller_trackedQuad.txt
     * If no quad is being tracked, then an invalid TrackedQuad is returned.
     * @since 2.2.6
     **/
        public TrackedQuad TrackedQuad {
            get {

                return _connection.GetLatestQuad ();
            } 
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
       * **POLICY_IMAGES** -- request that your application receives images from the
       *   device cameras. The "Allow Images" checkbox must be enabled in the
       *   Leap Motion Control Panel or this policy will be denied.
       *
       *   The images policy determines whether an application receives image data from
       *   the Leap Motion sensors which each frame of data. By default, this data is
       *   not sent. Only applications that use the image data should request this policy.
       *
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
         * Receive raw images from sensor cameras.
         * @since 2.1.0
         */
            POLICY_IMAGES = (1 << 1),
            /**
         * Optimize the tracking for head-mounted device.
         * @since 2.1.2
         */
            POLICY_OPTIMIZE_HMD = (1 << 2),
            /**
        * Allow pausing and unpausing of the Leap Motion service.
        * @since 2.4.0
        */
            POLICY_ALLOW_PAUSE_RESUME = (1 << 3),
            POLICY_RAW_IMAGES = (1 << 6),
        }


        //Controller events
        protected virtual void OnInit (object sender, LeapEventArgs eventArgs)
        {
            object senderObj = new object ();
            Init.DispatchOnContext<LeapEventArgs> (senderObj, EventContext, eventArgs);
        }

        protected virtual void OnConnect (object sender, ConnectionEventArgs eventArgs)
        {
            Connect.DispatchOnContext<ConnectionEventArgs> (this, EventContext, eventArgs);
        }

        protected virtual void OnDisconnect (object sender, ConnectionLostEventArgs eventArgs)
        {
            Disconnect.DispatchOnContext<ConnectionLostEventArgs> (this, EventContext, eventArgs);
        }

        protected virtual void OnExit (object sender, LeapEventArgs eventArgs)
        {
            Exit.DispatchOnContext<LeapEventArgs> (this, EventContext, eventArgs);
        }

        protected virtual void OnFocusGained (object sender, LeapEventArgs eventArgs)
        {
            FocusGained.DispatchOnContext<LeapEventArgs> (this, EventContext, eventArgs);
        }

        protected virtual void OnFocusLost (object sender, LeapEventArgs eventArgs)
        {
            FocusLost.DispatchOnContext<LeapEventArgs> (this, EventContext, eventArgs);
        }

        protected virtual void OnServiceConnect (object sender, LeapEventArgs eventArgs)
        {
            ServiceConnect.DispatchOnContext<LeapEventArgs> (this, EventContext, eventArgs);
        }

        protected virtual void OnServiceDisconnect (object sender, LeapEventArgs eventArgs)
        {
            ServiceDisconnect.DispatchOnContext<LeapEventArgs> (this, EventContext, eventArgs);
        }

        protected virtual void OnDevice (object sender, DeviceEventArgs eventArgs)
        {
            Device.DispatchOnContext<DeviceEventArgs> (this, EventContext, eventArgs);
        }

        protected virtual void OnDeviceLost (object sender, DeviceEventArgs eventArgs)
        {
            DeviceLost.DispatchOnContext<DeviceEventArgs> (this, EventContext, eventArgs);
        }

        protected virtual void OnServiceChange (object sender, LeapEventArgs eventArgs)
        {
            ServiceChange.DispatchOnContext<LeapEventArgs> (this, EventContext, eventArgs);
        }

        //Rebroadcasted events from connection
        protected virtual void OnFrame (object sender, FrameEventArgs eventArgs)
        {
            FrameReady.DispatchOnContext<FrameEventArgs> (this, EventContext, eventArgs);
        }

        protected virtual void OnImages (object sender, ImageEventArgs eventArgs)
        {
            ImageReady.DispatchOnContext<ImageEventArgs> (this, EventContext, eventArgs);
        }

        protected virtual void OnFailedImageRequest (object sender, ImageRequestFailedEventArgs eventArgs)
        {
            ImageRequestFailed.DispatchOnContext<ImageRequestFailedEventArgs> (this, EventContext, eventArgs);
        }

        protected virtual void OnDistortionChange (object sender, DistortionEventArgs eventArgs)
        {
            DistortionChange.DispatchOnContext<DistortionEventArgs> (this, EventContext, eventArgs);
        }

        protected virtual void OnTrackedQuad (object sender, TrackedQuadEventArgs eventArgs)
        {
            TrackedQuadReady.DispatchOnContext<TrackedQuadEventArgs> (this, EventContext, eventArgs);
        }

        protected virtual void OnLogEvent (object sender, LogEventArgs eventArgs)
        {
            LogMessage.DispatchOnContext<LogEventArgs> (this, EventContext, eventArgs);
        }

        protected virtual void OnPolicyChange (object sender, PolicyEventArgs eventArgs)
        {
            PolicyChange.DispatchOnContext<PolicyEventArgs> (this, EventContext, eventArgs);
        }

        protected virtual void OnConfigChange (object sender, ConfigChangeEventArgs eventArgs)
        {
            ConfigChange.DispatchOnContext<ConfigChangeEventArgs> (this, EventContext, eventArgs);
        }

        protected virtual void OnDeviceFailure (object sender, DeviceFailureEventArgs eventArgs)
        {
            DeviceFailure.DispatchOnContext<DeviceFailureEventArgs> (this, EventContext, eventArgs);
        }


    }

}
