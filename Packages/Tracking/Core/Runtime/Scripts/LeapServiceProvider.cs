/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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

        [Obsolete("This const is named incorrectly. Use US_TO_S instead.")]
        protected const double NS_TO_S = 1e-6;

        [Obsolete("This const is named incorrectly. Use S_TO_US instead.")]
        protected const double S_TO_NS = 1e6;

        /// <summary>
        /// Converts Microseconds to seconds.
        /// </summary>
        protected const double US_TO_S = 1e-6;

        /// <summary>
        /// Converts seconds to Microseconds.
        /// </summary>
        protected const double S_TO_US = 1e6;

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
            Device_3Di,
            Automatic,
            LeapMotionController2,
        }
        [Tooltip("Displays a representation of the traking device")]
        [SerializeField]
        protected InteractionVolumeVisualization _interactionVolumeVisualization = InteractionVolumeVisualization.Automatic;

        /// <summary>
        /// Which interaction volume is selected to be visualized in the scene view
        /// </summary>
        public InteractionVolumeVisualization SelectedInteractionVolumeVisualization => _interactionVolumeVisualization;

        [Tooltip("Displays a visualization of the Field Of View of the chosen device as a Gizmo")]
        [SerializeField]
        protected bool FOV_Visualization = false;
        [Tooltip("Displays the optimal FOV for tracking")]
        [SerializeField]
        protected bool OptimalFOV_Visualization = true;
        [Tooltip("Displays the maximum FOV for tracking")]
        [SerializeField]
        protected bool MaxFOV_Visualization = true;

        [SerializeField, HideInInspector]
        private VisualFOV _visualFOV;
        private LeapFOVInfos leapFOVInfos;
        private Mesh optimalFOVMesh;
        private Mesh maxFOVMesh;

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

        #region Multiple Device
        public enum MultipleDeviceMode
        {
            Disabled,
            Specific
        }

        [Tooltip("When set to 'Default', provider will receive data from the first connected device. \n" +
            "When set to `Specific`, provider will receive data from the device specified by 'Specific Serial Number'.")]
        [EditTimeOnly]
        [SerializeField]
        protected MultipleDeviceMode _multipleDeviceMode = MultipleDeviceMode.Disabled;

        public MultipleDeviceMode CurrentMultipleDeviceMode
        {
            get { return _multipleDeviceMode; }
        }

        [Tooltip("When Multiple Device Mode is set to `Specific`, the provider will " +
          "receive data from only the devices that contain this in their serial number.")]
        [SerializeField]
        protected string _specificSerialNumber = "";

        /// <summary>
        /// When Multiple Device Mode is set to `Specific`, the provider will
        /// receive data from only the first device that contain this in their serial number.
        /// If no device serial number contains SpecificSerialNumber, the first device in 
        /// the list of connected devices (controller.Devices) is used
        /// </summary>
        public string SpecificSerialNumber
        {
            get { return _specificSerialNumber; }
            set
            {
                _specificSerialNumber = value;
                if (_multipleDeviceMode != MultipleDeviceMode.Specific) Debug.Log("You are trying to set a Specific Serial Number while Multiple Device Mode is not set to 'Specific'. Please change the Multiple Device Mode to 'Specific'");
                else if (_currentDevice == null || _currentDevice.SerialNumber != _specificSerialNumber)
                {
                    updateDevice();
                }
            }
        }

        protected Device _currentDevice;

        /// <summary>
        /// The tracking device currently associated with this provider
        /// </summary>
        public Device CurrentDevice
        {
            get
            {
                if (_currentDevice == null && _multipleDeviceMode == MultipleDeviceMode.Disabled)
                {
                    _currentDevice = GetLeapController().Devices.ActiveDevices.FirstOrDefault();
                }
                return _currentDevice;
            }
        }

        /// <summary>
        /// The list of currently attached and recognized Leap Motion controller devices.
        /// The Device objects in the list describe information such as the range and
        /// tracking volume.
        /// </summary>
        public List<Device> Devices
        {
            get { return _leapController.Devices; }
        }

        #endregion

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
        protected bool _preventInitializingTrackingMode;

        [Tooltip("Which Leap Service API Endpoint to connect to.  This is configured on the service with the 'api_namespace' argument.")]
        [SerializeField]
        [EditTimeOnly]
        protected string _serverNameSpace = "Leap Service";

        public override TrackingSource TrackingDataSource { get { return CheckLeapServiceAvailable(); } }

        /// <summary>
        /// Determines if the service provider should temporally resample frames for smoothness.
        /// </summary>
        [SerializeField]
        public bool _useInterpolation = true;

        [SerializeField, Min(-1), Tooltip("The maximum number of attempts to connect to the service. Infinite attempts if -1.")]
        public int _reconnectionAttempts = MAX_RECONNECTION_ATTEMPTS;

        [SerializeField, Min(1), Tooltip("The interval in frames between service connection attempts.")]
        public int _reconnectionInterval = RECONNECTION_INTERVAL;

        #endregion

        #region Internal Settings & Memory

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

        private protected SmoothedFloat _fixedOffset = new SmoothedFloat();
        private protected SmoothedFloat _smoothedTrackingLatency = new SmoothedFloat();
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

        private Action<Device> _onDeviceChanged;

        /// <summary>
        /// An event that is dispatched whenever the Service provider changes device (eg. when the SpecificSerialNumber changes during runtime) 
        /// the param Device is the new CurrentDevice
        /// </summary>
        public event Action<Device> OnDeviceChanged
        {
            add
            {
                _onDeviceChanged += value;
            }
            remove
            {
                _onDeviceChanged -= value;
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
                    if (_currentDevice == null)
                    {
                        _backingUntransformedEditTimeFrame = new Frame();
                    }
                    else
                    {
                        _backingUntransformedEditTimeFrame = new Frame(_currentDevice.DeviceID);
                    }  
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
                    if (_currentDevice == null)
                    {
                        _backingEditTimeFrame = new Frame();
                    }
                    else
                    {
                        _backingEditTimeFrame = new Frame(_currentDevice.DeviceID);
                    }
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

        protected virtual void OnEnable()
        {
#if !UNITY_EDITOR
            AndroidServiceBinder.Bind();
#endif
        }

        // No longer necessary but would be a breaking change if removed
        protected virtual void OnDisable()
        {
        }

#else
        // No longer necessary but would be a breaking change if removed
        protected virtual void OnEnable()
        {
        }

        // No longer necessary but would be a breaking change if removed
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

        private void Awake()
        {
            _smoothedTrackingLatency.SetBlend(0.99f, 0.111f);
        }

        protected virtual void Start()
        {
            createController();

            if (_currentDevice == null)
            {
                _transformedUpdateFrame = new Frame();
                _transformedFixedFrame = new Frame();
                _untransformedUpdateFrame = new Frame();
                _untransformedFixedFrame = new Frame();
            }
            else
            {
                _transformedUpdateFrame = new Frame(_currentDevice.DeviceID);
                _transformedFixedFrame = new Frame(_currentDevice.DeviceID);
                _untransformedUpdateFrame = new Frame(_currentDevice.DeviceID);
                _untransformedFixedFrame = new Frame(_currentDevice.DeviceID);
            }
        }

        protected virtual void Update()
        {
            if (!checkConnectionIntegrity()) { return; }

#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isCompiling)
            {
                UnityEditor.EditorApplication.isPlaying = false;
                Debug.LogWarning("Unity hot reloading not currently supported. Stopping Editor Playback.");
                return;
            }
#endif

            if (_frameOptimization == FrameOptimizationMode.ReusePhysicsForUpdate)
            {
                DispatchUpdateFrameEvent(_transformedFixedFrame);
                return;
            }

            // if the serial number has changed since the last update(), update the device
            if (_multipleDeviceMode == MultipleDeviceMode.Specific && (_currentDevice == null || _currentDevice.SerialNumber != SpecificSerialNumber))
            {
                updateDevice();
            }

            if (_useInterpolation)
            {
#if !UNITY_ANDROID || UNITY_EDITOR
                _smoothedTrackingLatency.value = Mathf.Min(_smoothedTrackingLatency.value, 30000f);
                _smoothedTrackingLatency.Update((float)(_leapController.Now() - _leapController.FrameTimestamp()), Time.deltaTime);
#endif
                long timestamp = CalculateInterpolationTime() + (ExtrapolationAmount * 1000);
                _unityToLeapOffset = timestamp - (long)(Time.time * S_TO_US);

                _leapController.GetInterpolatedFrameFromTime(_untransformedUpdateFrame, timestamp, CalculateInterpolationTime() - (BounceAmount * 1000), _currentDevice);
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
                        timestamp = (long)(extrapolatedTime * S_TO_US) + _unityToLeapOffset;
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

                _leapController.GetInterpolatedFrame(_untransformedFixedFrame, timestamp, _currentDevice);
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

        protected virtual void OnApplicationPause(bool isPaused)
        {
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
        }

        /// <summary>
        /// Triggers a coroutine that sets appropriate policy flags and wait for them to be set to ensure we've changed mode
        /// </summary>
        /// <param name="trackingMode">Tracking mode to set</param>
        public void ChangeTrackingMode(TrackingOptimizationMode trackingMode)
        {
            _trackingOptimization = trackingMode;
            StartCoroutine(ChangeTrackingMode_Coroutine(trackingMode));
        }

        private IEnumerator ChangeTrackingMode_Coroutine(TrackingOptimizationMode trackingMode)
        {
            yield return new WaitWhile(() => _leapController == null || !_leapController.IsConnected);

            Device deviceToChange = _multipleDeviceMode == MultipleDeviceMode.Disabled ? null : _currentDevice;

            switch (trackingMode)
            {
                case TrackingOptimizationMode.Desktop:
                    _leapController.ClearPolicy(Controller.PolicyFlag.POLICY_OPTIMIZE_SCREENTOP, deviceToChange);
                    _leapController.ClearPolicy(Controller.PolicyFlag.POLICY_OPTIMIZE_HMD, deviceToChange);
                    break;
                case TrackingOptimizationMode.Screentop:
                    _leapController.SetAndClearPolicy(Controller.PolicyFlag.POLICY_OPTIMIZE_SCREENTOP, Controller.PolicyFlag.POLICY_OPTIMIZE_HMD, deviceToChange);
                    break;
                case TrackingOptimizationMode.HMD:
                    _leapController.SetAndClearPolicy(Controller.PolicyFlag.POLICY_OPTIMIZE_HMD, Controller.PolicyFlag.POLICY_OPTIMIZE_SCREENTOP, deviceToChange);
                    break;
            }
        }

        /// <summary>
        /// Gets the current mode by polling policy flags
        /// </summary>
        public TrackingOptimizationMode GetTrackingMode()
        {
            if (_leapController == null) return _trackingOptimization;

            Device deviceToGet = _multipleDeviceMode == MultipleDeviceMode.Disabled ? null : _currentDevice;

            var screenTopPolicySet = _leapController.IsPolicySet(Controller.PolicyFlag.POLICY_OPTIMIZE_SCREENTOP, deviceToGet);
            var headMountedPolicySet = _leapController.IsPolicySet(Controller.PolicyFlag.POLICY_OPTIMIZE_HMD, deviceToGet);

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
            if (_leapController != null)
            {
                return;
            }

            string serialNumber = _multipleDeviceMode != MultipleDeviceMode.Disabled ? SpecificSerialNumber : "";

            _leapController = new Controller(serialNumber.GetHashCode(), _serverNameSpace, _multipleDeviceMode != MultipleDeviceMode.Disabled);

            _leapController.Device += (s, e) =>
            {
                if (_onDeviceSafe != null)
                {
                    _onDeviceSafe(e.Device);
                }
            };

            _leapController.DeviceLost += (s, e) =>
            {
                if (e.Device == _currentDevice)
                {
                    _currentDevice = null;
                }
            };

            _onDeviceSafe += (d) =>
           {
               if (_multipleDeviceMode == MultipleDeviceMode.Specific)
               {
                   if (SpecificSerialNumber != null && SpecificSerialNumber != "" && d.SerialNumber.Contains(SpecificSerialNumber) && _leapController != null)
                   {
                       connectToNewDevice(d);
                   }
               }
               else if (_multipleDeviceMode == MultipleDeviceMode.Disabled)
               {
                   _currentDevice = d;
               }
               else
               {
                   throw new NotImplementedException($"{nameof(MultipleDeviceMode)} case not implemented");
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
        }

        /// <summary>
        /// Only during runtime:
        /// connects to a new device (param d) and subscribes to its device events. 
        /// Also unsubscribes from all device events from the last connected device.
        /// </summary>
        /// <param name="d"></param>
        /// <returns>true if connection was successfull, false if there is no leapController set up correctly 
        /// or application is not playing or already connected to Device d</returns>
        private bool connectToNewDevice(Device d)
        {
            if (_leapController == null || !Application.isPlaying || _currentDevice == d)
            {
                return false;
            }

            if (_currentDevice != null)
            {
                _leapController.UnsubscribeFromDeviceEvents(_currentDevice);
            }

            Debug.Log($"Connecting to Device with Serial: {d.SerialNumber} and ID {d.DeviceID}");
            _leapController.SubscribeToDeviceEvents(d);
            _currentDevice = d;

            if (_onDeviceChanged != null)
            {
                _onDeviceChanged(d);
            }

            return true;
        }

        /// <summary>
        /// update the connected device. This should be called when the serial number has been changed and 
        /// the currently connected device isn't the right one anymore.
        /// searches for a device with matching serial number (same as SpecificSerialNumber) and connects to the first on it finds.
        /// </summary>
        private void updateDevice()
        {
            if (_leapController == null) return;
            if (SpecificSerialNumber == null || SpecificSerialNumber == "") return;

            foreach (Device d in _leapController.Devices)
            {
                if (d.SerialNumber.Contains(SpecificSerialNumber))
                {
                    connectToNewDevice(d);
                    return;
                }
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
                _leapController.UnsubscribeFromAllDevices();
                _leapController.StopConnection();
                _leapController.Dispose();
                _leapController = null;
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
            else if (_numberOfReconnectionAttempts < _reconnectionAttempts || _reconnectionAttempts == -1)
            {
                _framesSinceServiceConnectionChecked++;

                if (_framesSinceServiceConnectionChecked > _reconnectionInterval)
                {
                    _framesSinceServiceConnectionChecked = 0;
                    _numberOfReconnectionAttempts++;

                    Debug.LogWarning("Leap Service not connected; attempting to reconnect for try " +
                                     _numberOfReconnectionAttempts + "/" + _reconnectionAttempts +
                                     "...", this);
                    destroyController();
                    createController();
                }
            }
            return false;
        }

        protected void onHandControllerConnect(object sender, LeapEventArgs args)
        {
            initializeFlags();

            if (_leapController != null)
            {
                _leapController.Device -= onHandControllerConnect;
            }
        }

        protected virtual void transformFrame(Frame source, Frame dest)
        {
            dest.CopyFrom(source).Transform(new LeapTransform(transform));
        }

        private TrackingSource CheckLeapServiceAvailable()
        {
            if (_trackingSource != TrackingSource.NONE)
            {
                return _trackingSource;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            if(AndroidServiceBinder.Bind())
            {
                _trackingSource = TrackingSource.LEAPC;
                return _trackingSource;
            }
#endif

            if (LeapInternal.Connection.IsConnectionAvailable(_serverNameSpace))
            {
                _trackingSource = TrackingSource.LEAPC;
            }
            else
            {
                _trackingSource = TrackingSource.NONE;
            }

            return _trackingSource;
        }

        #endregion

        #region Draw Gizmos

        private void OnDrawGizmos()
        {
            if (_visualFOV == null || leapFOVInfos == null || leapFOVInfos.SupportedDevices.Count == 0)
            {
                LoadFOVData();
            }

            Transform targetTransform = transform;
            LeapXRServiceProvider xrProvider = this as LeapXRServiceProvider;
            if (xrProvider != null)
            {
                targetTransform = xrProvider.mainCamera.transform;

                // deviceOrigin is set if camera follows a transform
                if (xrProvider.deviceOffsetMode == LeapXRServiceProvider.DeviceOffsetMode.Transform && xrProvider.deviceOrigin != null)
                {
                    targetTransform = xrProvider.deviceOrigin;
                }
            }

            switch (_interactionVolumeVisualization)
            {
                case LeapServiceProvider.InteractionVolumeVisualization.LeapMotionController:
                    DrawTrackingDevice(targetTransform, "Leap Motion Controller");
                    break;
                case LeapServiceProvider.InteractionVolumeVisualization.StereoIR170:
                    DrawTrackingDevice(targetTransform, "Stereo IR 170");
                    break;
                case LeapServiceProvider.InteractionVolumeVisualization.Device_3Di:
                    DrawTrackingDevice(targetTransform, "3Di");
                    break;
                case LeapServiceProvider.InteractionVolumeVisualization.LeapMotionController2:
                    DrawTrackingDevice(targetTransform, "Leap Motion Controller 2");
                    break;
                case LeapServiceProvider.InteractionVolumeVisualization.Automatic:
                    DetectConnectedDevice(targetTransform);
                    break;
                default:
                    break;
            }
        }


        private void DetectConnectedDevice(Transform targetTransform)
        {
            if (GetLeapController() != null && GetLeapController().Devices?.Count >= 1)
            {
                Device currentDevice = _currentDevice;
                if (currentDevice == null || (_multipleDeviceMode == LeapServiceProvider.MultipleDeviceMode.Specific && currentDevice.SerialNumber != _specificSerialNumber))
                {
                    foreach (Device d in GetLeapController().Devices)
                    {
                        if (d.SerialNumber.Contains(_specificSerialNumber))
                        {
                            currentDevice = d;
                            break;
                        }
                    }
                }

                if (currentDevice == null && _multipleDeviceMode == MultipleDeviceMode.Disabled)
                {
                    currentDevice = GetLeapController().Devices[0];
                }

                if (currentDevice == null || (_multipleDeviceMode == LeapServiceProvider.MultipleDeviceMode.Specific && currentDevice.SerialNumber != _specificSerialNumber))
                {
                    return;
                }

                Device.DeviceType deviceType = currentDevice.Type;
                if (deviceType == Device.DeviceType.TYPE_RIGEL || deviceType == Device.DeviceType.TYPE_SIR170)
                {
                    DrawTrackingDevice(targetTransform, "Stereo IR 170");
                    return;
                }
                else if (deviceType == Device.DeviceType.TYPE_3DI)
                {
                    DrawTrackingDevice(targetTransform, "3Di");
                    return;
                }
                else if (deviceType == Device.DeviceType.TYPE_PERIPHERAL)
                {
                    DrawTrackingDevice(targetTransform, "Leap Motion Controller");
                    return;
                }
                else if (deviceType == Device.DeviceType.TYPE_LMC2)
                {
                    DrawTrackingDevice(targetTransform, "Leap Motion Controller 2");
                }
            }

            // if no devices connected, no serial number selected or the connected device type isn't matching one of the above,
            // delete any device model that is currently displayed
            if (targetTransform.Find("DeviceModel") != null)
            {
                GameObject.DestroyImmediate(targetTransform.Find("DeviceModel").gameObject);
            }
        }


        private void DrawTrackingDevice(Transform targetTransform, string deviceType)
        {
            Matrix4x4 deviceModelMatrix = targetTransform.localToWorldMatrix;

            LeapXRServiceProvider xrProvider = this as LeapXRServiceProvider;
            if (xrProvider != null && xrProvider.deviceOffsetMode != LeapXRServiceProvider.DeviceOffsetMode.Transform)
            {
                deviceModelMatrix *= Matrix4x4.Translate(new Vector3(0, xrProvider.deviceOffsetYAxis, xrProvider.deviceOffsetZAxis));
                deviceModelMatrix *= Matrix4x4.Rotate(Quaternion.Euler(-90 - xrProvider.deviceTiltXAxis, 180, 0));
            }

            LeapFOVInfo info = null;
            foreach (var leapInfo in leapFOVInfos.SupportedDevices)
            {
                if (leapInfo.Name == deviceType)
                {
                    info = leapInfo;
                    Material mat = Resources.Load("TrackingVolumeVisualization/DeviceModelMat") as Material;
                    mat.SetPass(0);

                    Graphics.DrawMeshNow(Resources.Load<Mesh>("TrackingVolumeVisualization/Meshes/" + deviceType), deviceModelMatrix *
                           Matrix4x4.Scale(Vector3.one * 0.01f));
                    break;
                }
            }

            if (info != null)
            {
                SetDeviceInfo(info);
            }
            else
            {
                Debug.LogError("Tried to load invalid device type: " + deviceType);
                return;
            }

            if (FOV_Visualization)
            {
                DrawInteractionZone(deviceModelMatrix);
            }
        }

        private void DrawInteractionZone(Matrix4x4 deviceModelMatrix)
        {
            _visualFOV.UpdateFOVS();

            optimalFOVMesh = _visualFOV.OptimalFOVMesh;
            maxFOVMesh = _visualFOV.MaxFOVMesh;

            if (OptimalFOV_Visualization && optimalFOVMesh != null)
            {
                Material mat = Resources.Load("TrackingVolumeVisualization/OptimalFOVMat_Volume") as Material;
                mat.SetPass(0);

                Graphics.DrawMeshNow(optimalFOVMesh, deviceModelMatrix *
                       Matrix4x4.Scale(Vector3.one * 0.01f));
            }
            if (MaxFOV_Visualization && maxFOVMesh != null)
            {
                Material mat = Resources.Load("TrackingVolumeVisualization/MaxFOVMat_Volume") as Material;
                mat.SetPass(0);

                Graphics.DrawMeshNow(maxFOVMesh, deviceModelMatrix *
                       Matrix4x4.Scale(Vector3.one * 0.01f));
            }
        }

        private void LoadFOVData()
        {
            leapFOVInfos = JsonUtility.FromJson<LeapFOVInfos>(Resources.Load<TextAsset>("TrackingVolumeVisualization/SupportedTrackingDevices").text);

            if (_visualFOV == null)
            {
                _visualFOV = new VisualFOV();
            }
        }

        public void SetDeviceInfo(LeapFOVInfo leapInfo)
        {
            _visualFOV.HorizontalFOV = leapInfo.HorizontalFOV;
            _visualFOV.VerticalFOV = leapInfo.VerticalFOV;
            _visualFOV.MinDistance = leapInfo.MinDistance;
            _visualFOV.MaxDistance = leapInfo.MaxDistance;
            _visualFOV.OptimalMaxDistance = leapInfo.OptimalDistance;
        }

        [Serializable]
        public class LeapFOVInfos
        {
            public List<LeapFOVInfo> SupportedDevices;
        }

        [Serializable]
        public class LeapFOVInfo
        {
            public string Name;
            public float HorizontalFOV;
            public float VerticalFOV;
            public float OptimalDistance;
            public float MinDistance;
            public float MaxDistance;
        }

        #endregion
    }
}