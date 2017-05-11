/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity {
  /**LeapServiceProvider creates a Controller and supplies Leap Hands and images */
  public class LeapServiceProvider : LeapProvider {
    /** Conversion factor for nanoseconds to seconds. */
    protected const float NS_TO_S = 1e-6f;
    /** Conversion factor for seconds to nanoseconds. */
    protected const float S_TO_NS = 1e6f;
    /** Transform Array for Precull Latching **/
    protected const string HAND_ARRAY = "_LeapHandTransforms";

    public enum FrameOptimizationMode {
      None,
      ReuseUpdateForPhysics,
      ReusePhysicsForUpdate,
    }

    [Tooltip("When enabled, the provider will only calculate one leap frame instead of two.")]
    [SerializeField]
    protected FrameOptimizationMode _frameOptimization = FrameOptimizationMode.None;

    [Header("Device Type")]
    [SerializeField]
    protected bool _overrideDeviceType = false;

    [Tooltip("If overrideDeviceType is enabled, the hand controller will return a device of this type.")]
    [SerializeField]
    protected LeapDeviceType _overrideDeviceTypeWith = LeapDeviceType.Peripheral;

    protected bool _useInterpolation = true;

//Extrapolate on Android to compensate for the latency introduced by its graphics pipeline
#if UNITY_ANDROID
    protected int ExtrapolationAmount = 15;
    protected int BounceAmount = 70;
#else
    protected int ExtrapolationAmount = 0;
    protected int BounceAmount = 0;
#endif

    protected Controller leap_controller_;

    protected SmoothedFloat _fixedOffset = new SmoothedFloat();
    protected SmoothedFloat _smoothedTrackingLatency = new SmoothedFloat();

    protected Frame _untransformedUpdateFrame;
    protected Frame _transformedUpdateFrame;
    protected Frame _untransformedFixedFrame;
    protected Frame _transformedFixedFrame;

    public override Frame CurrentFrame {
      get {
        if (_frameOptimization == FrameOptimizationMode.ReusePhysicsForUpdate) {
          return _transformedFixedFrame;
        } else {
          return _transformedUpdateFrame;
        }
      }
    }

    public override Frame CurrentFixedFrame {
      get {
        if (_frameOptimization == FrameOptimizationMode.ReuseUpdateForPhysics) {
          return _transformedUpdateFrame;
        } else {
          return _transformedFixedFrame;
        }
      }
    }

    protected bool UseInterpolation {
      get {
        return _useInterpolation;
      }
      set {
        _useInterpolation = value;
      }
    }

    /** Returns the Leap Controller instance. */
    public Controller GetLeapController() {
#if UNITY_EDITOR
      //Null check to deal with hot reloading
      if (leap_controller_ == null) {
        createController();
      }
#endif
      return leap_controller_;
    }

    /** True, if the Leap Motion hardware is plugged in and this application is connected to the Leap Motion service. */
    public bool IsConnected() {
      return GetLeapController().IsConnected;
    }

    /** Returns information describing the device hardware. */
    public LeapDeviceInfo GetDeviceInfo() {
      if (_overrideDeviceType) {
        return new LeapDeviceInfo(_overrideDeviceTypeWith);
      }

      DeviceList devices = GetLeapController().Devices;
      if (devices.Count == 1) {
        LeapDeviceInfo info = new LeapDeviceInfo(LeapDeviceType.Peripheral);
        // TODO: DeviceList does not tell us the device type. Dragonfly serial starts with "LE" and peripheral starts with "LP"
        if (devices[0].SerialNumber.Length >= 2) {
          switch (devices[0].SerialNumber.Substring(0, 2)) {
            case ("LP"):
              info = new LeapDeviceInfo(LeapDeviceType.Peripheral);
              break;
            case ("LE"):
              info = new LeapDeviceInfo(LeapDeviceType.Dragonfly);
              break;
            default:
              break;
          }
        }

        // TODO: Add baseline & offset when included in API
        // NOTE: Alternative is to use device type since all parameters are invariant
        info.isEmbedded = devices[0].Type != Device.DeviceType.TYPE_PERIPHERAL;
        info.horizontalViewAngle = devices[0].HorizontalViewAngle * Mathf.Rad2Deg;
        info.verticalViewAngle = devices[0].VerticalViewAngle * Mathf.Rad2Deg;
        info.trackingRange = devices[0].Range / 1000f;
        info.serialID = devices[0].SerialNumber;
        return info;
      } else if (devices.Count > 1) {
        return new LeapDeviceInfo(LeapDeviceType.Peripheral);
      }
      return new LeapDeviceInfo(LeapDeviceType.Invalid);
    }

    public void RetransformFrames() {
      transformFrame(_untransformedUpdateFrame, _transformedUpdateFrame);
      transformFrame(_untransformedFixedFrame, _transformedFixedFrame);
    }

    protected virtual void Awake() {
      _fixedOffset.delay = 0.4f;
      _smoothedTrackingLatency.SetBlend(0.99f, 0.0111f);
    }

    protected virtual void Start() {
      createController();
      _transformedUpdateFrame = new Frame();
      _transformedFixedFrame = new Frame();
      _untransformedUpdateFrame = new Frame();
      _untransformedFixedFrame = new Frame();
    }

    protected virtual void Update() {
#if UNITY_EDITOR
      if (EditorApplication.isCompiling) {
        EditorApplication.isPlaying = false;
        Debug.LogWarning("Unity hot reloading not currently supported. Stopping Editor Playback.");
        return;
      }
#endif

      _fixedOffset.Update(Time.time - Time.fixedTime, Time.deltaTime);

      if (_frameOptimization == FrameOptimizationMode.ReusePhysicsForUpdate) {
        DispatchUpdateFrameEvent(_transformedFixedFrame);
        return;
      }

      if (_useInterpolation) {
#if !UNITY_ANDROID
        _smoothedTrackingLatency.value = Mathf.Min(_smoothedTrackingLatency.value, 30000f);
        _smoothedTrackingLatency.Update((float)(leap_controller_.Now() - leap_controller_.FrameTimestamp()), Time.deltaTime);
#endif
        leap_controller_.GetInterpolatedFrameFromTime(_untransformedUpdateFrame, CalculateInterpolationTime() + (ExtrapolationAmount * 1000), CalculateInterpolationTime() - (BounceAmount * 1000));
      } else {
        leap_controller_.Frame(_untransformedUpdateFrame);
      }

      if (_untransformedUpdateFrame != null) {
        transformFrame(_untransformedUpdateFrame, _transformedUpdateFrame);

        DispatchUpdateFrameEvent(_transformedUpdateFrame);
      }
    }

    protected virtual void FixedUpdate() {
      if (_frameOptimization == FrameOptimizationMode.ReuseUpdateForPhysics) {
        DispatchFixedFrameEvent(_transformedUpdateFrame);
        return;
      }

      if (_useInterpolation) {
        leap_controller_.GetInterpolatedFrame(_untransformedFixedFrame, CalculateInterpolationTime());
      } else {
        leap_controller_.Frame(_untransformedFixedFrame);
      }

      if (_untransformedFixedFrame != null) {
        transformFrame(_untransformedFixedFrame, _transformedFixedFrame);

        DispatchFixedFrameEvent(_transformedFixedFrame);
      }
    }

    protected virtual long CalculateInterpolationTime(bool endOfFrame = false) {
#if UNITY_ANDROID
      return leap_controller_.Now() - 16000;
#else
      if (leap_controller_ != null) {
        return leap_controller_.Now() - (long)_smoothedTrackingLatency.value;
      } else {
        return 0;
      }
#endif
    }

    protected virtual void OnDestroy() {
      destroyController();
    }

    protected virtual void OnApplicationPause(bool isPaused) {
      if (leap_controller_ != null) {
        if (isPaused) {
          leap_controller_.StopConnection();
        } else {
          leap_controller_.StartConnection();
        }
      }
    }

    protected virtual void OnApplicationQuit() {
      destroyController();
    }

    /*
     * Initializes the Leap Motion policy flags.
     * The POLICY_OPTIMIZE_HMD flag improves tracking for head-mounted devices.
     */
    protected virtual void initializeFlags() {
      if (leap_controller_ == null) {
        return;
      }

      leap_controller_.ClearPolicy(Controller.PolicyFlag.POLICY_OPTIMIZE_HMD);
    }
    /** Create an instance of a Controller, initialize its policy flags
     * and subscribe to connection event */
    protected void createController() {
      if (leap_controller_ != null) {
        destroyController();
      }

      leap_controller_ = new Controller();
      if (leap_controller_.IsConnected) {
        initializeFlags();
      } else {
        leap_controller_.Device += onHandControllerConnect;
      }
    }

    /** Calling this method stop the connection for the existing instance of a Controller, 
     * clears old policy flags and resets to null */
    protected void destroyController() {
      if (leap_controller_ != null) {
        if (leap_controller_.IsConnected) {
          leap_controller_.ClearPolicy(Controller.PolicyFlag.POLICY_OPTIMIZE_HMD);
        }
        leap_controller_.StopConnection();
        leap_controller_ = null;
      }
    }

    protected void onHandControllerConnect(object sender, LeapEventArgs args) {
      initializeFlags();
      leap_controller_.Device -= onHandControllerConnect;
    }

    protected virtual void transformFrame(Frame source, Frame dest) {
      dest.CopyFrom(source).Transform(transform.GetLeapMatrix());
    }
  }
}
