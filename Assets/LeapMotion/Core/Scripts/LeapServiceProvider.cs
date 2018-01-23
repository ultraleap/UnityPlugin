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
using System;
using System.Collections;
using Leap.Unity.Attributes;
//using Leap.Unity.Graphing;

namespace Leap.Unity {
  /**LeapServiceProvider creates a Controller and supplies Leap Hands and images */
  public class LeapServiceProvider : LeapProvider {
    /** Conversion factor for nanoseconds to seconds. */
    protected const double NS_TO_S = 1e-6;
    /** Conversion factor for seconds to nanoseconds. */
    protected const double S_TO_NS = 1e6;
    /** Transform Array for Precull Latching **/
    protected const string HAND_ARRAY = "_LeapHandTransforms";

    public enum FrameOptimizationMode {
      None,
      ReuseUpdateForPhysics,
      ReusePhysicsForUpdate,
    }

    public enum PhysicsExtrapolationMode {
      None,
      Auto,
      Manual
    }

    [Tooltip("Set true if the Leap Motion hardware is mounted on an HMD; otherwise, leave false.")]
    [SerializeField]
    protected bool _isHeadMounted = false;

    [SerializeField]
    protected LeapVRTemporalWarping _temporalWarping;

    [Tooltip("When enabled, the provider will only calculate one leap frame instead of two.")]
    [SerializeField]
    protected FrameOptimizationMode _frameOptimization = FrameOptimizationMode.None;

    [Tooltip("The mode to use when extrapolating physics.\n" +
             " None - No extrapolation is used at all.\n" +
             " Auto - Extrapolation is chosen based on the fixed timestep.\n" +
             " Manual - Extrapolation time is chosen manually by the user.")]
    [SerializeField]
    protected PhysicsExtrapolationMode _physicsExtrapolation = PhysicsExtrapolationMode.Auto;

    [Tooltip("The amount of time (in seconds) to extrapolate the phyiscs data by.")]
    [SerializeField]
    protected float _physicsExtrapolationTime = 1.0f / 90.0f;

    [Header("[Experimental]")]
    [Tooltip("Pass updated transform matrices to objects with materials using the VertexOffsetShader.")]
    [SerializeField]
    protected bool _updateHandInPrecull = false;

    protected bool _useInterpolation = true;

    //Extrapolate on Android to compensate for the latency introduced by its graphics pipeline
#if UNITY_ANDROID && !UNITY_EDITOR
    protected int ExtrapolationAmount = 15;
    protected int BounceAmount = 70;
#else
    protected int ExtrapolationAmount = 0;
    protected int BounceAmount = 0;
#endif

    protected Controller leap_controller_;

    protected bool manualUpdateHasBeenCalledSinceUpdate;
    protected Vector3 warpedPosition;
    protected Quaternion warpedRotation;
    protected SmoothedFloat _fixedOffset = new SmoothedFloat();
    protected SmoothedFloat _smoothedTrackingLatency = new SmoothedFloat();
    protected long _unityToLeapOffset;

    protected Frame _untransformedUpdateFrame;
    protected Frame _transformedUpdateFrame;

    protected Frame _untransformedFixedFrame;
    protected Frame _transformedFixedFrame;

    protected Matrix4x4[] _transformArray = new Matrix4x4[2];

    [NonSerialized]
    public long imageTimeStamp = 0;

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

    public bool UpdateHandInPrecull {
      get {
        return _updateHandInPrecull;
      }
      set {
        resetTransforms();
        _updateHandInPrecull = value;
      }
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
          throw new InvalidOperationException("Unexpected physics extrapolation mode: " + _physicsExtrapolation);
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
      return LeapDeviceInfo.GetLeapDeviceInfo();
    }

    public void ReTransformFrames() {
      transformFrame(_untransformedUpdateFrame, _transformedUpdateFrame);
      transformFrame(_untransformedFixedFrame, _transformedFixedFrame);
    }

    protected virtual void Reset() {
      if (checkShouldEnableHeadMounted()) {
        _isHeadMounted = true;
      }

      _temporalWarping = GetComponentInParent<LeapVRTemporalWarping>();
      _frameOptimization = FrameOptimizationMode.None;
      _updateHandInPrecull = false;
    }

    protected virtual void Awake() {
      _fixedOffset.delay = 0.4f;
      _smoothedTrackingLatency.SetBlend(0.99f, 0.0111f);
    }

    protected virtual void Start() {
      checkShouldEnableHeadMounted();

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
      manualUpdateHasBeenCalledSinceUpdate = false;

      _fixedOffset.Update(Time.time - Time.fixedTime, Time.deltaTime);

      if (_frameOptimization == FrameOptimizationMode.ReusePhysicsForUpdate) {
        DispatchUpdateFrameEvent(_transformedFixedFrame);
        return;
      }

      if (_useInterpolation) {
#if !UNITY_ANDROID || UNITY_EDITOR
        _smoothedTrackingLatency.value = Mathf.Min(_smoothedTrackingLatency.value, 30000f);
        _smoothedTrackingLatency.Update((float)(leap_controller_.Now() - leap_controller_.FrameTimestamp()), Time.deltaTime);
#endif
        long timestamp = CalculateInterpolationTime() + (ExtrapolationAmount * 1000);
        _unityToLeapOffset = timestamp - (long)(Time.time * S_TO_NS);

        leap_controller_.GetInterpolatedFrameFromTime(_untransformedUpdateFrame, timestamp, CalculateInterpolationTime() - (BounceAmount * 1000));
      } else {
        leap_controller_.Frame(_untransformedUpdateFrame);
      }

      imageTimeStamp = leap_controller_.FrameTimestamp();

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
            //By default we use Time.fixedTime to ensure that our hands are on the same timeline
            //as Update.  We add an extrapolation value to help compensate for latency.
            float extrapolatedTime = Time.fixedTime + CalculatePhysicsExtrapolation();
            timestamp = (long)(extrapolatedTime * S_TO_NS) + _unityToLeapOffset;
            break;
          case FrameOptimizationMode.ReusePhysicsForUpdate:
            //If we are re-using physics frames for update, we don't even want to care about
            //Time.fixedTime, just grab the most recent interpolated timestamp like we are
            //in Update
            timestamp = CalculateInterpolationTime() + (ExtrapolationAmount * 1000);
            break;
          default:
            throw new InvalidOperationException("Unexpected frame optimization mode: " + _frameOptimization);
        }
        leap_controller_.GetInterpolatedFrame(_untransformedFixedFrame, timestamp);

      } else {
        leap_controller_.Frame(_untransformedFixedFrame);
      }

      if (_untransformedFixedFrame != null) {
        transformFrame(_untransformedFixedFrame, _transformedFixedFrame);

        DispatchFixedFrameEvent(_transformedFixedFrame);
      }
    }

    long CalculateInterpolationTime(bool endOfFrame = false) {
#if UNITY_ANDROID && !UNITY_EDITOR
      return leap_controller_.Now() - 16000;
#else
      if (leap_controller_ != null) {
        return leap_controller_.Now() - (long)_smoothedTrackingLatency.value + (_updateHandInPrecull && !endOfFrame ? (long)(Time.smoothDeltaTime * S_TO_NS / Time.timeScale) : 0);
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

    protected virtual void OnEnable() {
      Camera.onPreCull -= LateUpdateHandTransforms;
      Camera.onPreCull += LateUpdateHandTransforms;
      resetTransforms();
    }

    protected virtual void OnDisable() {
      Camera.onPreCull -= LateUpdateHandTransforms;
      resetTransforms();
    }

    private bool checkShouldEnableHeadMounted() {
      if (XRSupportUtil.IsXREnabled()) {
        var parentCamera = GetComponentInParent<Camera>();
        if (parentCamera != null && parentCamera.stereoTargetEye != StereoTargetEyeMask.None) {

          if (!_isHeadMounted) {
            if (Application.isPlaying) {
              Debug.LogError("VR is enabled and the LeapServiceProvider is the child of a "
                           + "camera targeting one or both stereo eyes; You should "
                           + "check the isHeadMounted option on the LeapServiceProvider "
                           + "if the Leap is mounted or attached to your VR headset!",
                             this);
            }
            return true;
          }
        }
      }
      return false;
    }

    /*
     * Resets the Global Hand Transform Shader Matrices
     */
    protected void resetTransforms() {
      _transformArray[0] = Matrix4x4.identity;
      _transformArray[1] = Matrix4x4.identity;
      Shader.SetGlobalMatrixArray(HAND_ARRAY, _transformArray);
    }

    /*
     * Initializes the Leap Motion policy flags.
     * The POLICY_OPTIMIZE_HMD flag improves tracking for head-mounted devices.
     */
    protected void initializeFlags() {
      if (leap_controller_ == null) {
        return;
      }
      //Optimize for top-down tracking if on head mounted display.
      if (_isHeadMounted) {
        leap_controller_.SetPolicy(Controller.PolicyFlag.POLICY_OPTIMIZE_HMD);
      } else {
        leap_controller_.ClearPolicy(Controller.PolicyFlag.POLICY_OPTIMIZE_HMD);
      }
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

    protected void transformFrame(Frame source, Frame dest, bool resampleTemporalWarping = true) {
      LeapTransform leapTransform;
      if (_temporalWarping != null) {
        if (resampleTemporalWarping) {
          _temporalWarping.TryGetWarpedTransform(LeapVRTemporalWarping.WarpedAnchor.CENTER, out warpedPosition, out warpedRotation, source.Timestamp);
          warpedRotation = warpedRotation * transform.localRotation;
        }

        leapTransform = new LeapTransform(warpedPosition.ToVector(), warpedRotation.ToLeapQuaternion(), transform.lossyScale.ToVector() * 1e-3f);
        leapTransform.MirrorZ();
      } else {
        leapTransform = transform.GetLeapMatrix();
      }

      dest.CopyFrom(source).Transform(leapTransform);
    }

    protected void transformHands(ref LeapTransform LeftHand, ref LeapTransform RightHand) {
      LeapTransform leapTransform;
      if (_temporalWarping != null) {
        leapTransform = new LeapTransform(warpedPosition.ToVector(), warpedRotation.ToLeapQuaternion(), transform.lossyScale.ToVector() * 1e-3f);
        leapTransform.MirrorZ();
      } else {
        leapTransform = transform.GetLeapMatrix();
      }

      LeftHand = new LeapTransform(leapTransform.TransformPoint(LeftHand.translation), leapTransform.TransformQuaternion(LeftHand.rotation));
      RightHand = new LeapTransform(leapTransform.TransformPoint(RightHand.translation), leapTransform.TransformQuaternion(RightHand.rotation));
    }

    public void LateUpdateHandTransforms(Camera camera) {
      if (_updateHandInPrecull) {
#if UNITY_EDITOR
        //Hard-coded name of the camera used to generate the pre-render view
        if (camera.gameObject.name == "PreRenderCamera") {
          return;
        }

        bool isScenePreviewCamera = camera.gameObject.hideFlags == HideFlags.HideAndDontSave;
        if (isScenePreviewCamera) {
          return;
        }
#endif

        if (Application.isPlaying && !manualUpdateHasBeenCalledSinceUpdate && leap_controller_ != null) {
          manualUpdateHasBeenCalledSinceUpdate = true;
          //Find the Left and/or Right Hand(s) to Latch
          Hand leftHand = null, rightHand = null;
          LeapTransform PrecullLeftHand = LeapTransform.Identity, PrecullRightHand = LeapTransform.Identity;
          for (int i = 0; i < CurrentFrame.Hands.Count; i++) {
            Hand updateHand = CurrentFrame.Hands[i];
            if (updateHand.IsLeft && leftHand == null) {
              leftHand = updateHand;
            } else if (updateHand.IsRight && rightHand == null) {
              rightHand = updateHand;
            }
          }

          //Determine their new Transforms
          leap_controller_.GetInterpolatedLeftRightTransform(CalculateInterpolationTime() + (ExtrapolationAmount * 1000), CalculateInterpolationTime() - (BounceAmount * 1000), (leftHand != null ? leftHand.Id : 0), (rightHand != null ? rightHand.Id : 0), out PrecullLeftHand, out PrecullRightHand);
          bool LeftValid = PrecullLeftHand.translation != Vector.Zero; bool RightValid = PrecullRightHand.translation != Vector.Zero;
          transformHands(ref PrecullLeftHand, ref PrecullRightHand);

          //Calculate the Delta Transforms
          if (rightHand != null && RightValid) {
            _transformArray[0] =
                               Matrix4x4.TRS(PrecullRightHand.translation.ToVector3(), PrecullRightHand.rotation.ToQuaternion(), Vector3.one) *
             Matrix4x4.Inverse(Matrix4x4.TRS(rightHand.PalmPosition.ToVector3(), rightHand.Rotation.ToQuaternion(), Vector3.one));
          }
          if (leftHand != null && LeftValid) {
            _transformArray[1] =
                               Matrix4x4.TRS(PrecullLeftHand.translation.ToVector3(), PrecullLeftHand.rotation.ToQuaternion(), Vector3.one) *
             Matrix4x4.Inverse(Matrix4x4.TRS(leftHand.PalmPosition.ToVector3(), leftHand.Rotation.ToQuaternion(), Vector3.one));
          }

          //Apply inside of the vertex shader
          Shader.SetGlobalMatrixArray(HAND_ARRAY, _transformArray);
        }
      }
    }
  }
}
