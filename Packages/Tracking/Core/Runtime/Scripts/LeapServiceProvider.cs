/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2021.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity.Attributes;
using LeapInternal;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity
{
    using Attributes;

    /// <summary>
    /// The LeapServiceProvider provides tracked Leap Hand data and images from the device
    /// via the Leap service running on the client machine.
    /// 
    /// It communicates with the Ultraleap Tracking Service running on your platform, and 
    /// provides Frame objects containing Leap hands to your application. Generally, any 
    /// class that needs hand tracking data from the camera will need a reference to a 
    /// LeapServiceProvider to get that data.
    /// </summary>
    public class LeapServiceProvider : LeapProvider
    {

        #region Constants

        /// <summary>
        /// Converts nanoseconds to seconds.
        /// </summary>
        protected const double NS_TO_S = 1e-6;

        /// <summary>
        /// Converts seconds to nanoseconds.
        /// </summary>
        protected const double S_TO_NS = 1e6;

        /// <summary>
        /// The transform array used for late-latching.
        /// </summary>
        protected const string HAND_ARRAY_GLOBAL_NAME = "_LeapHandTransforms";

        /// <summary>
        /// The maximum number of times the provider will 
        /// attempt to reconnect to the service before giving up.
        /// </summary>
#if UNITY_ANDROID
        protected const int MAX_RECONNECTION_ATTEMPTS = 10;
#else
        protected const int MAX_RECONNECTION_ATTEMPTS = 5;
#endif

        /// <summary>
        /// The number of frames to wait between each
        /// reconnection attempt.
        /// </summary>
#if UNITY_ANDROID
        protected const int RECONNECTION_INTERVAL = 360;
#else
        protected const int RECONNECTION_INTERVAL = 180;
#endif

        #endregion

        #region Inspector

        /// <summary>
        /// The supported interaction volumes that will be visualized in the scene view
        /// </summary>
        public enum InteractionVolumeVisualization
        {
            None,
            LeapMotionController,
            StereoIR170,
            Automatic
        }
        [Tooltip("Displays a representation of the interaction volume in the scene view")]
        [SerializeField]
        protected InteractionVolumeVisualization _interactionVolumeVisualization = InteractionVolumeVisualization.Automatic;

        /// <summary>
        /// Which interaction volume is selected to be visualized in the scene view
        /// </summary>
        public InteractionVolumeVisualization SelectedInteractionVolumeVisualization => _interactionVolumeVisualization;

        /// <summary>
        /// Supported modes to optimize frame updates.
        /// When enabled, the provider will reuse some hand data:
        /// By default the mode is set to None, which implies that we want to use hand tracking in time with Unity's Update loop.
        /// It can be set to ReuseUpdateForPhysics (choose as Android user), or ReusePhysicsForUpdate (reinterpolates the hand data for the 
        /// physics timestep).
        /// </summary>
        public enum FrameOptimizationMode
        {
            /// <summary>
            /// By default the mode is set to None, this implies that we want to use hand tracking in time with Unity's Update loop.
            /// </summary>
            None,
            /// <summary>
            /// Android users should choose Reuse Update for Physics.
            /// </summary>
            ReuseUpdateForPhysics,
            /// <summary>
            /// Provides the option to reinterpolate the hand data for the physics timestep, improving the movement of objects being 
            /// manipulated by hands when using the interaction engine. Enabling this incurs a small time penalty (fraction of a ms).
            /// </summary>
            ReusePhysicsForUpdate,
        }
        [Tooltip("When enabled, the provider will reuse some hand data:\n"
            + "None - By default the mode is set to None, which implies that we want to use hand tracking in time with Unity's Update loop.\n"
            + "ReuseUpdateForPhysics - Android users should choose Reuse Update for Physics.\n"
            + "ReusePhysicsForUpdate - Provides the option to reinterpolate the hand data for the physics timestep, improving the movement of objects being "
            + "manipulated by hands when using the interaction engine. Enabling this incurs a small time penalty (fraction of a ms).")]
        [SerializeField]
        protected FrameOptimizationMode _frameOptimization = FrameOptimizationMode.None;

        /// <summary>
        /// Supported modes to use when extrapolating physics.
        /// </summary>
        public enum PhysicsExtrapolationMode
        {
            /// <summary>
            /// No extrapolation is used at all
            /// </summary>
            None,
            /// <summary>
            /// Extrapolation is chosen based on the fixed timestep
            /// </summary>
            Auto,
            /// <summary>
            /// Extrapolation time is chosen manually by the user
            /// </summary>
            Manual
        }
        [Tooltip("The mode to use when extrapolating physics.\n" +
                 " None - No extrapolation is used at all.\n" +
                 " Auto - Extrapolation is chosen based on the fixed timestep.\n" +
                 " Manual - Extrapolation time is chosen manually by the user.")]
        [SerializeField]
        protected PhysicsExtrapolationMode _physicsExtrapolation = PhysicsExtrapolationMode.Auto;

        [Tooltip("The amount of time (in seconds) to extrapolate the physics data by.")]
        [SerializeField]
        protected float _physicsExtrapolationTime = 1.0f / 90.0f;

        /// <summary>
        /// (Service must be >= 4.9.2)
        /// The tracking mode used by the service. Should be set to match the orientation of the hand tracking hardware.
        /// </summary>
        public enum TrackingOptimizationMode
        {
            Desktop,
            Screentop,
            /// <summary>
            /// The LeapXRServiceProvider should be used for all XR headset based applications, where hand tracking 
            /// devices are mounted on the headset. The LeapXRServiceProvider uses the HMD tracking optimization mode.
            /// </summary>
            HMD
        }
        [Tooltip("[Service must be >= 4.9.2!] " +
          "Which tracking mode to request that the service optimize for. " +
          "(The LeapXRServiceProvider should be used for all XR headset based applications, where hand tracking devices are mounted on the headset)")]
        [SerializeField]
        [EditTimeOnly]
        protected TrackingOptimizationMode _trackingOptimization = TrackingOptimizationMode.Desktop;

        [Tooltip("Enable to prevent changes to tracking mode during initialization. The mode the service is currently set to will be retained.")]
        [SerializeField]
        private bool _preventInitializingTrackingMode;

#if UNITY_2017_3_OR_NEWER
        [Tooltip("When checked, profiling data from the LeapCSharp worker thread will be used to populate the UnityProfiler.")]
        [EditTimeOnly]
#else
        [Tooltip("Worker thread profiling requires a Unity version of 2017.3 or greater.")]
        [Disable]
#endif
        [SerializeField]
        protected bool _workerThreadProfiling = false;

        [Tooltip("Which Leap Service API Endpoint to connect to.  This is configured on the service with the 'api_namespace' argument.")]
        [SerializeField]
        [EditTimeOnly]
        protected string _serverNameSpace = "Leap Service";

        #endregion

        #region Internal Settings & Memory
        protected bool _useInterpolation = true;

#if SVR
        protected IntPtr _clockRebaser;
        protected System.Diagnostics.Stopwatch _stopwatch = new System.Diagnostics.Stopwatch();
#endif

        // Extrapolate on Android to compensate for the latency introduced by its graphics
        // pipeline.
#if UNITY_ANDROID && !UNITY_EDITOR
    protected int ExtrapolationAmount = 0; // 15;
    protected int BounceAmount = 70;
#else
        protected int ExtrapolationAmount = 0;
        protected int BounceAmount = 0;
#endif

        protected Controller _leapController;
        protected bool _isDestroyed;

        protected SmoothedFloat _fixedOffset = new SmoothedFloat();
        protected SmoothedFloat _smoothedTrackingLatency = new SmoothedFloat();
        protected long _unityToLeapOffset;

        protected Frame _untransformedUpdateFrame;
        protected Frame _transformedUpdateFrame;
        protected Frame _untransformedFixedFrame;
        protected Frame _transformedFixedFrame;

        #endregion

        #region Edit-time Frame Data

        private Action<Device> _onDeviceSafe;
        /// <summary>
        /// A utility event to get a callback whenever a new device is connected to the service.
        /// This callback will ALSO trigger a callback upon subscription if a device is already
        /// connected.
        /// 
        /// For situations with multiple devices OnDeviceSafe will be dispatched once for each device.
        /// </summary>
        public event Action<Device> OnDeviceSafe
        {
            add
            {
                if (_leapController != null)
                {
                    _leapController.Device += (a0, a1) =>
                    {
                        value(a1.Device);
                    };
                    if (_leapController.IsConnected)
                    {
                        foreach (var device in _leapController.Devices)
                        {
                            value(device);
                        }
                    }
                }
                _onDeviceSafe += value;
            }
            remove
            {
                _onDeviceSafe -= value;
            }
        }

#if UNITY_EDITOR
        private Frame _backingUntransformedEditTimeFrame = null;
        private Frame _untransformedEditTimeFrame
        {
            get
            {
                if (_backingUntransformedEditTimeFrame == null)
                {
                    _backingUntransformedEditTimeFrame = new Frame();
                }
                return _backingUntransformedEditTimeFrame;
            }
        }
        private Frame _backingEditTimeFrame = null;
        private Frame _editTimeFrame
        {
            get
            {
                if (_backingEditTimeFrame == null)
                {
                    _backingEditTimeFrame = new Frame();
                }
                return _backingEditTimeFrame;
            }
        }

        private Dictionary<TestHandFactory.TestHandPose, Hand> _cachedLeftHands
          = new Dictionary<TestHandFactory.TestHandPose, Hand>();
        private Hand _editTimeLeftHand
        {
            get
            {
                Hand cachedHand = null;
                if (_cachedLeftHands.TryGetValue(editTimePose, out cachedHand))
                {
                    return cachedHand;
                }
                else
                {
                    cachedHand = TestHandFactory.MakeTestHand(isLeft: true, pose: editTimePose);
                    _cachedLeftHands[editTimePose] = cachedHand;
                    return cachedHand;
                }
            }
        }

        private Dictionary<TestHandFactory.TestHandPose, Hand> _cachedRightHands
          = new Dictionary<TestHandFactory.TestHandPose, Hand>();
        private Hand _editTimeRightHand
        {
            get
            {
                Hand cachedHand = null;
                if (_cachedRightHands.TryGetValue(editTimePose, out cachedHand))
                {
                    return cachedHand;
                }
                else
                {
                    cachedHand = TestHandFactory.MakeTestHand(isLeft: false, pose: editTimePose);
                    _cachedRightHands[editTimePose] = cachedHand;
                    return cachedHand;
                }
            }
        }

#endif

        #endregion

        #region LeapProvider Implementation

        /// <summary>
        /// The current frame for this update cycle, in world space. 
        /// 
        /// IMPORTANT!  This frame might be mutable!  If you hold onto a reference
        /// to this frame, or a reference to any object that is a part of this frame,
        /// it might change unexpectedly.  If you want to save a reference, make sure
        /// to make a copy.
        /// </summary>
        public override Frame CurrentFrame
        {
            get
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    _editTimeFrame.Hands.Clear();
                    _untransformedEditTimeFrame.Hands.Clear();
                    _untransformedEditTimeFrame.Hands.Add(_editTimeLeftHand);
                    _untransformedEditTimeFrame.Hands.Add(_editTimeRightHand);
                    transformFrame(_untransformedEditTimeFrame, _editTimeFrame);
                    return _editTimeFrame;
                }
#endif
                if (_frameOptimization == FrameOptimizationMode.ReusePhysicsForUpdate)
                {
                    return _transformedFixedFrame;
                }
                else
                {
                    return _transformedUpdateFrame;
                }
            }
        }

        /// <summary>
        /// The current frame for this fixed update cycle, in world space.
        /// 
        /// IMPORTANT!  This frame might be mutable!  If you hold onto a reference
        /// to this frame, or a reference to any object that is a part of this frame,
        /// it might change unexpectedly.  If you want to save a reference, make sure
        /// to make a copy.
        /// </summary>
        public override Frame CurrentFixedFrame
        {
            get
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    _editTimeFrame.Hands.Clear();
                    _untransformedEditTimeFrame.Hands.Clear();
                    _untransformedEditTimeFrame.Hands.Add(_editTimeLeftHand);
                    _untransformedEditTimeFrame.Hands.Add(_editTimeRightHand);
                    transformFrame(_untransformedEditTimeFrame, _editTimeFrame);
                    return _editTimeFrame;
                }
#endif
                if (_frameOptimization == FrameOptimizationMode.ReuseUpdateForPhysics)
                {
                    return _transformedUpdateFrame;
                }
                else
                {
                    return _transformedFixedFrame;
                }
            }
        }

        #endregion

        #region Android Support

#if UNITY_ANDROID
        private AndroidJavaObject _serviceBinder;
        AndroidJavaClass unityPlayer;
        AndroidJavaObject activity;
        AndroidJavaObject context;
        ServiceCallbacks serviceCallbacks;

        protected virtual void OnEnable()
        {
            CreateAndroidBinding();
        }

        public bool CreateAndroidBinding()
        {
            try
            {
                if (_serviceBinder != null)
                {
                    //Check binding status before calling rebind
                    bool bindStatus = _serviceBinder.Call<bool>("isBound");
                    Debug.Log("CreateAndroidBinding - Current service binder status " + bindStatus);
                    if (bindStatus)
                    {
                        return true;
                    }
                    else
                    {
                        _serviceBinder = null;
                    }
                }

                if (_serviceBinder == null)
                {
                    //Get activity and context
                    if (unityPlayer == null)
                    {
                        Debug.Log("CreateAndroidBinding - Getting activity and context");
                        unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                        activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                        context = activity.Call<AndroidJavaObject>("getApplicationContext");
                        serviceCallbacks = new ServiceCallbacks();
                    }

                    //Create a new service binding
                    Debug.Log("CreateAndroidBinding - Creating a new service binder");
                    _serviceBinder = new AndroidJavaObject("com.ultraleap.tracking.service_binder.ServiceBinder", context, serviceCallbacks);
                    bool success = _serviceBinder.Call<bool>("bind");
                    if (success)
                    {
                        Debug.Log("CreateAndroidBinding - Binding of service binder complete");
                    }
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("CreateAndroidBinding - Failed to bind service: " + e.Message);
                _serviceBinder = null;
            }
            return false;
        }

        protected virtual void OnDisable()
        {
            if (_serviceBinder != null)
            {
                Debug.Log("ServiceBinder.unbind...");
                _serviceBinder.Call("unbind");
            }
        }

#else
        protected virtual void OnEnable()
        {
        }

        protected virtual void OnDisable()
        {
        }

#endif
        #endregion

        #region Unity Events

        protected virtual void Reset()
        {
            editTimePose = TestHandFactory.TestHandPose.DesktopModeA;
        }

        protected virtual void Awake()
        {
            _fixedOffset.delay = 0.4f;
            _smoothedTrackingLatency.SetBlend(0.99f, 0.0111f);
        }

        protected virtual void Start()
        {
            createController();
            _transformedUpdateFrame = new Frame();
            _transformedFixedFrame = new Frame();
            _untransformedUpdateFrame = new Frame();
            _untransformedFixedFrame = new Frame();
        }

        protected virtual void Update()
        {
            if (_workerThreadProfiling)
            {
                LeapProfiling.Update();
            }

            if (!checkConnectionIntegrity()) { return; }

#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isCompiling)
            {
                UnityEditor.EditorApplication.isPlaying = false;
                Debug.LogWarning("Unity hot reloading not currently supported. Stopping Editor Playback.");
                return;
            }
#endif

            _fixedOffset.Update(Time.time - Time.fixedTime, Time.deltaTime);

            if (_frameOptimization == FrameOptimizationMode.ReusePhysicsForUpdate)
            {
                DispatchUpdateFrameEvent(_transformedFixedFrame);
                return;
            }

#if SVR
            if (_clockRebaser != IntPtr.Zero)
            {
                eLeapRS result = LeapC.UpdateRebase(_clockRebaser, _stopwatch.ElapsedMilliseconds, LeapC.GetNow());
                if (result != eLeapRS.eLeapRS_Success)
                {
                    Debug.LogWarning("UpdateRebase call failed");
                }
            }
#endif

            if (_useInterpolation)
            {
#if !UNITY_ANDROID || UNITY_EDITOR
                _smoothedTrackingLatency.value = Mathf.Min(_smoothedTrackingLatency.value, 30000f);
                _smoothedTrackingLatency.Update((float)(_leapController.Now() - _leapController.FrameTimestamp()), Time.deltaTime);
#endif
                long timestamp = CalculateInterpolationTime() + (ExtrapolationAmount * 1000);
                _unityToLeapOffset = timestamp - (long)(Time.time * S_TO_NS);

                _leapController.GetInterpolatedFrameFromTime(_untransformedUpdateFrame, timestamp, CalculateInterpolationTime() - (BounceAmount * 1000));
            }
            else
            {
                _leapController.Frame(_untransformedUpdateFrame);
            }

            if (_untransformedUpdateFrame != null)
            {
                transformFrame(_untransformedUpdateFrame, _transformedUpdateFrame);

                DispatchUpdateFrameEvent(_transformedUpdateFrame);
            }
        }

        protected virtual void FixedUpdate()
        {
            if (_frameOptimization == FrameOptimizationMode.ReuseUpdateForPhysics)
            {
                DispatchFixedFrameEvent(_transformedUpdateFrame);
                return;
            }

            if (_useInterpolation)
            {

                long timestamp;
                switch (_frameOptimization)
                {
                    case FrameOptimizationMode.None:
                        // By default we use Time.fixedTime to ensure that our hands are on the same
                        // timeline as Update.  We add an extrapolation value to help compensate
                        // for latency.
                        float extrapolatedTime = Time.fixedTime + CalculatePhysicsExtrapolation();
                        timestamp = (long)(extrapolatedTime * S_TO_NS) + _unityToLeapOffset;
                        break;
                    case FrameOptimizationMode.ReusePhysicsForUpdate:
                        // If we are re-using physics frames for update, we don't even want to care
                        // about Time.fixedTime, just grab the most recent interpolated timestamp
                        // like we are in Update.
                        timestamp = CalculateInterpolationTime() + (ExtrapolationAmount * 1000);
                        break;
                    default:
                        throw new System.InvalidOperationException(
                          "Unexpected frame optimization mode: " + _frameOptimization);
                }
                _leapController.GetInterpolatedFrame(_untransformedFixedFrame, timestamp);

            }
            else
            {
                _leapController.Frame(_untransformedFixedFrame);
            }

            if (_untransformedFixedFrame != null)
            {
                transformFrame(_untransformedFixedFrame, _transformedFixedFrame);

                DispatchFixedFrameEvent(_transformedFixedFrame);
            }
        }

        protected virtual void OnDestroy()
        {
            destroyController();
            _isDestroyed = true;
        }

        protected virtual void OnApplicationFocus(bool hasFocus)
        {
#if UNITY_ANDROID
            if (hasFocus)
            {
                CreateAndroidBinding();
            }
#endif
        }

        protected virtual void OnApplicationPause(bool isPaused)
        {
#if UNITY_ANDROID
            if (_leapController != null)
            {
                if (isPaused)
                {
                    _serviceBinder.Call("unbind");
                }
            }
#endif
            if (_leapController != null)
            {
                if (isPaused)
                {
                    _leapController.StopConnection();
                }
                else
                {
                    _leapController.StartConnection();
                }
            }
        }

        protected virtual void OnApplicationQuit()
        {
            destroyController();
            _isDestroyed = true;
        }

        /// <summary>
        /// Calculates the physics extrapolation time depending on the PhysicsExtrapolationMode.
        /// </summary>
        /// <returns>A float that can be used to compensate for latency when ensuring that our 
        /// hands are on the same timeline as Update.</returns>
        public float CalculatePhysicsExtrapolation()
        {
            switch (_physicsExtrapolation)
            {
                case PhysicsExtrapolationMode.None:
                    return 0;
                case PhysicsExtrapolationMode.Auto:
                    return Time.fixedDeltaTime;
                case PhysicsExtrapolationMode.Manual:
                    return _physicsExtrapolationTime;
                default:
                    throw new System.InvalidOperationException(
                      "Unexpected physics extrapolation mode: " + _physicsExtrapolation);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Returns the Leap Controller instance.
        /// If not found, it creates a new controller instance.
        /// </summary>
        public Controller GetLeapController()
        {
#if UNITY_EDITOR
            // Null check to deal with hot reloading.
            if (!_isDestroyed && _leapController == null)
            {
                createController();
            }
#endif
            return _leapController;
        }

        /// <summary>
        /// Returns true if the Leap Motion hardware is plugged in and this application is
        /// connected to the Leap Motion service.
        /// </summary>
        public bool IsConnected()
        {
            return GetLeapController().IsConnected;
        }

        /// <summary>
        /// Retransforms hand data from Leap space to the space of the Unity transform.
        /// This is only necessary if you're moving the LeapServiceProvider around in a
        /// custom script and trying to access Hand data from it directly afterward.
        /// </summary>
        public void RetransformFrames()
        {
            transformFrame(_untransformedUpdateFrame, _transformedUpdateFrame);
            transformFrame(_untransformedFixedFrame, _transformedFixedFrame);
        }

        /// <summary>
        /// Copies property settings from this LeapServiceProvider to the target
        /// LeapXRServiceProvider where applicable. Does not modify any XR-specific settings
        /// that only exist on the LeapXRServiceProvider.
        /// </summary>
        public void CopySettingsToLeapXRServiceProvider(LeapXRServiceProvider leapXRServiceProvider)
        {
            leapXRServiceProvider._interactionVolumeVisualization = _interactionVolumeVisualization;
            leapXRServiceProvider._frameOptimization = _frameOptimization;
            leapXRServiceProvider._physicsExtrapolation = _physicsExtrapolation;
            leapXRServiceProvider._physicsExtrapolationTime = _physicsExtrapolationTime;
            leapXRServiceProvider._workerThreadProfiling = _workerThreadProfiling;
        }

        /// <summary>
        /// Triggers a coroutine that sets appropriate policy flags and wait for them to be set to ensure we've changed mode
        /// </summary>
        /// <param name="trackingMode">Tracking mode to set</param>
        public void ChangeTrackingMode(TrackingOptimizationMode trackingMode)
        {
            _trackingOptimization = trackingMode;

            if (_leapController == null) return;

            switch (trackingMode)
            {
                case TrackingOptimizationMode.Desktop:
                    _leapController.ClearPolicy(Controller.PolicyFlag.POLICY_OPTIMIZE_SCREENTOP);
                    _leapController.ClearPolicy(Controller.PolicyFlag.POLICY_OPTIMIZE_HMD);
                    break;
                case TrackingOptimizationMode.Screentop:
                    _leapController.SetAndClearPolicy(Controller.PolicyFlag.POLICY_OPTIMIZE_SCREENTOP, Controller.PolicyFlag.POLICY_OPTIMIZE_HMD);
                    break;
                case TrackingOptimizationMode.HMD:
                    _leapController.SetAndClearPolicy(Controller.PolicyFlag.POLICY_OPTIMIZE_HMD, Controller.PolicyFlag.POLICY_OPTIMIZE_SCREENTOP);
                    break;
            }
        }

        /// <summary>
        /// Gets the current mode by polling policy flags
        /// </summary>
        public TrackingOptimizationMode GetTrackingMode()
        {
            if (_leapController == null) return _trackingOptimization;

            var screenTopPolicySet = _leapController.IsPolicySet(Controller.PolicyFlag.POLICY_OPTIMIZE_SCREENTOP);
            var headMountedPolicySet = _leapController.IsPolicySet(Controller.PolicyFlag.POLICY_OPTIMIZE_HMD);

            var desktopMode = !screenTopPolicySet && !headMountedPolicySet;
            if (desktopMode)
            {
                return TrackingOptimizationMode.Desktop;
            }

            var headMountedMode = !screenTopPolicySet && headMountedPolicySet;
            if (headMountedMode)
            {
                return TrackingOptimizationMode.HMD;
            }

            var screenTopMode = screenTopPolicySet && !headMountedPolicySet;
            if (screenTopMode)
            {
                return TrackingOptimizationMode.Screentop;
            }

            throw new Exception("Unknown tracking optimization mode");
        }

        #endregion

        #region Internal Methods

        protected virtual long CalculateInterpolationTime(bool endOfFrame = false)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
      return _leapController.Now() - 16000;
#else
            if (_leapController != null)
            {
                return _leapController.Now() - (long)_smoothedTrackingLatency.value;
            }
            else
            {
                return 0;
            }
#endif
        }

        /// <summary>
        /// Initializes the policy flags.
        /// </summary>
        protected virtual void initializeFlags()
        {
            if (_preventInitializingTrackingMode) return;

            ChangeTrackingMode(_trackingOptimization);
        }

        /// <summary>
        /// Creates an instance of a Controller, initializing its policy flags and
        /// subscribing to its connection event.
        /// </summary>
        protected void createController()
        {
#if SVR
            var bindStatus = CreateAndroidBinding();
            if (!bindStatus)
                return;

            InitClockRebaser();
#endif

            if (_leapController != null)
            {
                return;
            }

            _leapController = new Controller(0, _serverNameSpace);
            _leapController.Device += (s, e) =>
            {
                if (_onDeviceSafe != null)
                {
                    _onDeviceSafe(e.Device);
                }
            };

            if (_leapController.IsConnected)
            {
                initializeFlags();
            }
            else
            {
                _leapController.Device += onHandControllerConnect;
            }

            if (_workerThreadProfiling)
            {
                //A controller will report profiling statistics for the duration of it's lifetime
                //so these events will never be unsubscribed from.
                _leapController.EndProfilingBlock += LeapProfiling.EndProfilingBlock;
                _leapController.BeginProfilingBlock += LeapProfiling.BeginProfilingBlock;

                _leapController.EndProfilingForThread += LeapProfiling.EndProfilingForThread;
                _leapController.BeginProfilingForThread += LeapProfiling.BeginProfilingForThread;
            }
        }

        /// <summary>
        /// Stops the connection for the existing instance of a Controller, clearing old
        /// policy flags and resetting the Controller to null.
        /// </summary>
    	public void destroyController()
        {
            if (_leapController != null)
            {
                _leapController.StopConnection();
                _leapController.Dispose();
                _leapController = null;

#if SVR
                if (_clockRebaser != IntPtr.Zero)
                {
                    LeapC.DestroyClockRebaser(_clockRebaser);
                    _stopwatch.Stop();
                }
#endif
            }
        }

        private int _framesSinceServiceConnectionChecked = 0;
        private int _numberOfReconnectionAttempts = 0;
        /// <summary>
        /// Checks whether this provider is connected to a service;
        /// If it is not, attempt to reconnect at regular intervals
        /// for MAX_RECONNECTION_ATTEMPTS
        /// </summary>
        protected bool checkConnectionIntegrity()
        {
            if (_leapController != null && _leapController.IsServiceConnected)
            {
                _framesSinceServiceConnectionChecked = 0;
                _numberOfReconnectionAttempts = 0;
                return true;
            }
            else if (_numberOfReconnectionAttempts < MAX_RECONNECTION_ATTEMPTS)
            {
                _framesSinceServiceConnectionChecked++;

                if (_framesSinceServiceConnectionChecked > RECONNECTION_INTERVAL)
                {
                    _framesSinceServiceConnectionChecked = 0;
                    _numberOfReconnectionAttempts++;

                    Debug.LogWarning("Leap Service not connected; attempting to reconnect for try " +
                                     _numberOfReconnectionAttempts + "/" + MAX_RECONNECTION_ATTEMPTS +
                                     "...", this);
                    using (new ProfilerSample("Reconnection Attempt"))
                    {
                        destroyController();
                        createController();
                    }
                }
            }
            return false;
        }

        protected void onHandControllerConnect(object sender, LeapEventArgs args)
        {
            initializeFlags();

#if SVR
            InitClockRebaser();
#endif

            if (_leapController != null)
            {
                _leapController.Device -= onHandControllerConnect;
            }
        }

        protected virtual void transformFrame(Frame source, Frame dest)
        {
            dest.CopyFrom(source).Transform(transform.GetLeapMatrix());
        }

#if SVR
        private void InitClockRebaser()
        {
            _stopwatch.Start();
            eLeapRS result = LeapC.CreateClockRebaser(out _clockRebaser);

            if (result != eLeapRS.eLeapRS_Success)
            {
                Debug.LogError("Failed to create clock rebaser");
            }

            if (_clockRebaser == IntPtr.Zero)
            {
                Debug.LogError("Clock rebaser is null");
            }
        }
#endif

        #endregion

    }
}