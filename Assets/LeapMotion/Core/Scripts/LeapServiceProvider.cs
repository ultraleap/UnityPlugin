/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity {

  /// <summary>
  /// The LeapServiceProvider provides tracked Leap Hand data and images from the device
  /// via the Leap service running on the client machine.
  /// </summary>
  public class LeapServiceProvider : LeapProvider {

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

    #endregion

    #region Inspector

    public enum FrameOptimizationMode {
      None,
      ReuseUpdateForPhysics,
      ReusePhysicsForUpdate,
    }
    [Tooltip("When enabled, the provider will only calculate one leap frame instead of two.")]
    [SerializeField]
    protected FrameOptimizationMode _frameOptimization = FrameOptimizationMode.None;

    public enum PhysicsExtrapolationMode {
      None,
      Auto,
      Manual
    }
    [Tooltip("The mode to use when extrapolating physics.\n" +
             " None - No extrapolation is used at all.\n" +
             " Auto - Extrapolation is chosen based on the fixed timestep.\n" +
             " Manual - Extrapolation time is chosen manually by the user.")]
    [SerializeField]
    protected PhysicsExtrapolationMode _physicsExtrapolation = PhysicsExtrapolationMode.Auto;

    [Tooltip("The amount of time (in seconds) to extrapolate the phyiscs data by.")]
    [SerializeField]
    protected float _physicsExtrapolationTime = 1.0f / 90.0f;

    #endregion

    #region Internal Settings & Memory

    protected bool _useInterpolation = true;

    // Extrapolate on Android to compensate for the latency introduced by its graphics
    // pipeline.
#if UNITY_ANDROID && !UNITY_EDITOR
    protected int ExtrapolationAmount = 15;
    protected int BounceAmount = 70;
#else
    protected int ExtrapolationAmount = 0;
    protected int BounceAmount = 0;
#endif

    protected Controller _leapController;

    protected SmoothedFloat _fixedOffset = new SmoothedFloat();
    protected SmoothedFloat _smoothedTrackingLatency = new SmoothedFloat();
    protected long _unityToLeapOffset;

    protected Frame _untransformedUpdateFrame;
    protected Frame _transformedUpdateFrame;
    protected Frame _untransformedFixedFrame;
    protected Frame _transformedFixedFrame;

    #endregion

    #region Edit-time Frame Data

    #if UNITY_EDITOR

    private Frame _backingUntransformedEditTimeFrame = null;
    private Frame _untransformedEditTimeFrame {
      get {
        if (_backingUntransformedEditTimeFrame == null) {
          _backingUntransformedEditTimeFrame = new Frame();
        }
        return _backingUntransformedEditTimeFrame;
      }
    }
    private Frame _backingEditTimeFrame = null;
    private Frame _editTimeFrame {
      get {
        if (_backingEditTimeFrame == null) {
          _backingEditTimeFrame = new Frame();
        }
        return _backingEditTimeFrame;
      }
    }

    private Dictionary<TestHandFactory.TestHandPose, Hand> _cachedLeftHands
      = new Dictionary<TestHandFactory.TestHandPose, Hand>();
    private Hand _editTimeLeftHand {
      get {
        Hand cachedHand;
        if (_cachedLeftHands.TryGetValue(editTimePose, out cachedHand)) {
          return cachedHand;
        }
        else {
          cachedHand = TestHandFactory.MakeTestHand(isLeft: true, pose: editTimePose);
          _cachedLeftHands[editTimePose] = cachedHand;
          return cachedHand;
        }
      }
    }

    private Dictionary<TestHandFactory.TestHandPose, Hand> _cachedRightHands
      = new Dictionary<TestHandFactory.TestHandPose, Hand>();
    private Hand _editTimeRightHand {
      get {
        Hand cachedHand;
        if (_cachedRightHands.TryGetValue(editTimePose, out cachedHand)) {
          return cachedHand;
        }
        else {
          cachedHand = TestHandFactory.MakeTestHand(isLeft: false, pose: editTimePose);
          _cachedRightHands[editTimePose] = cachedHand;
          return cachedHand;
        }
      }
    }

    #endif

    #endregion

    #region LeapProvider Implementation

    public override Frame CurrentFrame {
      get {
        #if UNITY_EDITOR
        if (!Application.isPlaying) {
          _untransformedEditTimeFrame.Hands.Clear();
          _untransformedEditTimeFrame.Hands.Add(_editTimeLeftHand);
          _untransformedEditTimeFrame.Hands.Add(_editTimeRightHand);
          transformFrame(_untransformedEditTimeFrame, _editTimeFrame);
          return _editTimeFrame;
        }
        #endif
        if (_frameOptimization == FrameOptimizationMode.ReusePhysicsForUpdate) {
          return _transformedFixedFrame;
        } else {
          return _transformedUpdateFrame;
        }
      }
    }

    public override Frame CurrentFixedFrame {
      get {
        #if UNITY_EDITOR
        if (!Application.isPlaying) {
          _editTimeFrame.Hands.Clear();
          _editTimeFrame.Hands.Add(_editTimeLeftHand);
          _editTimeFrame.Hands.Add(_editTimeRightHand);
          return _editTimeFrame;
        }
        #endif
        if (_frameOptimization == FrameOptimizationMode.ReuseUpdateForPhysics) {
          return _transformedUpdateFrame;
        } else {
          return _transformedFixedFrame;
        }
      }
    }

    #endregion

    #region Unity Events

    protected virtual void Reset() {
      editTimePose = TestHandFactory.TestHandPose.DesktopModeA;
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
      if (UnityEditor.EditorApplication.isCompiling) {
        UnityEditor.EditorApplication.isPlaying = false;
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
#if !UNITY_ANDROID || UNITY_EDITOR
        _smoothedTrackingLatency.value = Mathf.Min(_smoothedTrackingLatency.value, 30000f);
        _smoothedTrackingLatency.Update((float)(_leapController.Now() - _leapController.FrameTimestamp()), Time.deltaTime);
#endif
        long timestamp = CalculateInterpolationTime() + (ExtrapolationAmount * 1000);
        _unityToLeapOffset = timestamp - (long)(Time.time * S_TO_NS);

        _leapController.GetInterpolatedFrameFromTime(_untransformedUpdateFrame, timestamp, CalculateInterpolationTime() - (BounceAmount * 1000));
      }
      else {
        _leapController.Frame(_untransformedUpdateFrame);
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

        long timestamp;
        switch (_frameOptimization) {
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
      else {
        _leapController.Frame(_untransformedFixedFrame);
      }

      if (_untransformedFixedFrame != null) {
        transformFrame(_untransformedFixedFrame, _transformedFixedFrame);

        DispatchFixedFrameEvent(_transformedFixedFrame);
      }
    }

    protected virtual void OnDestroy() {
      destroyController();
    }

    protected virtual void OnApplicationPause(bool isPaused) {
      if (_leapController != null) {
        if (isPaused) {
          _leapController.StopConnection();
        }
        else {
          _leapController.StartConnection();
        }
      }
    }

    protected virtual void OnApplicationQuit() {
      destroyController();
    }

    public float CalculatePhysicsExtrapolation() {
      switch (_physicsExtrapolation) {
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
    /// </summary>
    public Controller GetLeapController() {
      #if UNITY_EDITOR
      // Null check to deal with hot reloading.
      if (_leapController == null) {
        createController();
      }
      #endif
      return _leapController;
    }

    /// <summary>
    /// Returns true if the Leap Motion hardware is plugged in and this application is
    /// connected to the Leap Motion service.
    /// </summary>
    public bool IsConnected() {
      return GetLeapController().IsConnected;
    }

    /// <summary>
    /// Returns information describing the device hardware.
    /// </summary>
    public LeapDeviceInfo GetDeviceInfo() {
      return LeapDeviceInfo.GetLeapDeviceInfo();
    }

    /// <summary>
    /// Retransforms hand data from Leap space to the space of the Unity transform.
    /// This is only necessary if you're moving the LeapServiceProvider around in a
    /// custom script and trying to access Hand data from it directly afterward.
    /// </summary>
    public void RetransformFrames() {
      transformFrame(_untransformedUpdateFrame, _transformedUpdateFrame);
      transformFrame(_untransformedFixedFrame, _transformedFixedFrame);
    }

    #endregion

    #region Internal Methods

    protected virtual long CalculateInterpolationTime(bool endOfFrame = false) {
      #if UNITY_ANDROID && !UNITY_EDITOR
      return _leapController.Now() - 16000;
      #else
      if (_leapController != null) {
        return _leapController.Now() - (long)_smoothedTrackingLatency.value;
      } else {
        return 0;
      }
      #endif
    }
    
    /// <summary>
    /// Initializes Leap Motion policy flags.
    /// </summary>
    protected virtual void initializeFlags() {
      if (_leapController == null) {
        return;
      }

      _leapController.ClearPolicy(Controller.PolicyFlag.POLICY_DEFAULT);
    }
    
    /// <summary>
    /// Creates an instance of a Controller, initializing its policy flags and
    /// subscribing to its connection event.
    /// </summary>
    protected void createController() {
      if (_leapController != null) {
        destroyController();
      }

      _leapController = new Controller();
      if (_leapController.IsConnected) {
        initializeFlags();
      } else {
        _leapController.Device += onHandControllerConnect;
      }
    }
    
    /// <summary>
    /// Stops the connection for the existing instance of a Controller, clearing old
    /// policy flags and resetting the Controller to null.
    /// </summary>
    protected void destroyController() {
      if (_leapController != null) {
        if (_leapController.IsConnected) {
          _leapController.ClearPolicy(Controller.PolicyFlag.POLICY_OPTIMIZE_HMD);
        }
        _leapController.StopConnection();
        _leapController = null;
      }
    }

    protected void onHandControllerConnect(object sender, LeapEventArgs args) {
      initializeFlags();
      _leapController.Device -= onHandControllerConnect;
    }

    protected virtual void transformFrame(Frame source, Frame dest) {
      dest.CopyFrom(source).Transform(transform.GetLeapMatrix());
    }

    #endregion

  }

}
