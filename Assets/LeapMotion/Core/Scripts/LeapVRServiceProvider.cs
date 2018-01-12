/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using UnityEngine.XR;
using System;
using Leap.Unity.Attributes;

namespace Leap.Unity {

  /// <summary>
  /// The LeapVRServiceProvider expands on the standard LeapServiceProvider to
  /// account for the offset of the Leap device with respect to the attached HMD and
  /// warp tracked hand positions based on the motion of the headset to account for the
  /// differing latencies of the two tracking systems.
  /// </summary>
  public class LeapVRServiceProvider : LeapServiceProvider {

    #region Device Offset

    private const float DEFAULT_DEVICE_OFFSET_Y_AXIS = 0f;
    private const float DEFAULT_DEVICE_OFFSET_Z_AXIS = 0.12f;
    private const float DEFAULT_DEVICE_TILT_X_AXIS = 5f;

    [Header("Advanced")]

    [Tooltip("Allow manual adjustment of the Leap device's virtual offset and tilt. These "
           + "settings can be used to match the physical position and orientation of the "
           + "Leap Motion sensor on a tracked device it is mounted on (such as a VR "
           + "headset.)")]
    [SerializeField, OnEditorChange("allowManualDeviceOffset")]
    private bool _allowManualDeviceOffset;
    public bool allowManualDeviceOffset {
      get { return _allowManualDeviceOffset; }
      set {
        _allowManualDeviceOffset = value;
        if (!_allowManualDeviceOffset) {
          deviceOffsetYAxis = DEFAULT_DEVICE_OFFSET_Y_AXIS;
          deviceOffsetZAxis = DEFAULT_DEVICE_OFFSET_Z_AXIS;
          deviceTiltXAxis = DEFAULT_DEVICE_TILT_X_AXIS;
        }
      }
    }

    [Tooltip("Adjusts the Leap Motion device's virtual height offset from the tracked "
           + "headset position. This should match the vertical offset of the physical "
           + "device with respect to the headset in meters.")]
    [SerializeField]
    [Range(-0.50F, 0.50F)]
    private float _deviceOffsetYAxis = DEFAULT_DEVICE_OFFSET_Y_AXIS;
    public float deviceOffsetYAxis {
      get {
        return _deviceOffsetYAxis;
      }
      set {
        _deviceOffsetYAxis = value;
      }
    }

    [Tooltip("Adjusts the Leap Motion device's virtual depth offset from the tracked "
           + "headset position. This should match the forward offset of the physical "
           + "device with respect to the headset in meters.")]
    [SerializeField]
    [Range(-0.50F, 0.50F)]
    private float _deviceOffsetZAxis = DEFAULT_DEVICE_OFFSET_Z_AXIS;
    public float deviceOffsetZAxis {
      get {
        return _deviceOffsetZAxis;
      }
      set {
        _deviceOffsetZAxis = value;
      }
    }

    [Tooltip("Adjusts the Leap Motion device's virtual X axis tilt. This should match "
           + "the tilt of the physical device with respect to the headset in degrees.")]
    [SerializeField]
    [Range(-90.0F, 90.0F)]
    private float _deviceTiltXAxis = DEFAULT_DEVICE_TILT_X_AXIS;
    public float deviceTiltXAxis {
      get {
        return _deviceTiltXAxis;
      }
      set {
        _deviceTiltXAxis = value;
      }
    }

    #endregion

    #region Temporal Warping

    [Header("Temporal Warping")]
    
#if UNITY_STANDALONE
    private const int DEFAULT_WARP_ADJUSTMENT = 17;
#elif UNITY_ANDROID
    private const int DEFAULT_WARP_ADJUSTMENT = 45;
#else
    private const int DEFAULT_WARP_ADJUSTMENT = 17;
#endif

    [Tooltip("Temporal warping prevents the hand coordinate system from 'swimming' or "
           + "'bouncing' when the headset moves and the user's hands stay still. "
           + "This phenomenon is caused by the differing amounts of latencies inherent "
           + "in the two systems. "
           + "For PC VR and Android VR, temporal warping should be enabled, and you "
           + "should NOT use a manual warp alignment value, as the correct value is "
           + "chosen automatically for these platforms.")]
    [SerializeField]
    private bool _disableTemporalWarping = false;
    
    [Tooltip("Keep this option unchecked for PC VR and Android VR target platforms. "
           + "Some non-standard platforms may utilize this value to adjust their "
           + "latency compensation amount for temporal warping.")]
    [SerializeField]
    private bool _allowManualTimeAlignment = false;

    /// <summary>
    /// The time in milliseconds between the current frame's headset position and the
    /// time at which the Leap frame was captured.
    /// </summary>
    [Tooltip("The time in milliseconds between the current frame's headset position and "
           + "the time at which the Leap frame was captured.")]
    [SerializeField]
    private int _customWarpAdjustment = DEFAULT_WARP_ADJUSTMENT;
    public int warpingAdjustment {
      get {
        if (_allowManualTimeAlignment) {
          return _customWarpAdjustment;
        }
        else {
          return DEFAULT_WARP_ADJUSTMENT;
        }
      }
    }

    #endregion

    [Header("[Experimental]")]
    [Tooltip("Pass updated transform matrices to objects with materials using the "
           + "VertexOffsetShader.")]
    [SerializeField]
    protected bool _updateHandInPrecull = false;

    protected TransformHistory transformHistory = new TransformHistory();
    protected bool manualUpdateHasBeenCalledSinceUpdate;
    protected Vector3    warpedPosition = Vector3.zero;
    protected Quaternion warpedRotation = Quaternion.identity;
    protected Matrix4x4[] _transformArray = new Matrix4x4[2];

    private Camera _cachedCamera;

    [NonSerialized]
    public long imageTimeStamp = 0;

    public bool UpdateHandInPrecull {
      get {
        return _updateHandInPrecull;
      }
      set {
        resetTransforms();
        _updateHandInPrecull = value;
      }
    }

    protected virtual void Reset() {
      _customWarpAdjustment = DEFAULT_WARP_ADJUSTMENT;
    }

    protected override void Start() {
      base.Start();
      _cachedCamera = GetComponent<Camera>();
    }

    protected override void Update() {
      manualUpdateHasBeenCalledSinceUpdate = false;
      base.Update();
      imageTimeStamp = _leapController.FrameTimestamp();
    }

    protected override long CalculateInterpolationTime(bool endOfFrame = false) {
#if UNITY_ANDROID
      return leap_controller_.Now() - 16000;
#else
      if (_leapController != null) {
        return _leapController.Now()
               - (long)_smoothedTrackingLatency.value
               + ((_updateHandInPrecull && !endOfFrame)?
                    (long)(Time.smoothDeltaTime * S_TO_NS / Time.timeScale)
                  : 0);
      } else {
        return 0;
      }
#endif
    }

    void OnPreCull() {
#if UNITY_EDITOR
      if (!Application.isPlaying) {
        return;
      }
#endif

      transformHistory.UpdateDelay(
        InputTracking.GetLocalPosition(XRNode.CenterEye),
        InputTracking.GetLocalRotation(XRNode.CenterEye),
        _leapController.Now());

      LateUpdateHandTransforms(_cachedCamera);

      var projectionMatrix = _cachedCamera.projectionMatrix;

      switch (SystemInfo.graphicsDeviceType) {
#if !UNITY_2017_2_OR_NEWER
        case UnityEngine.Rendering.GraphicsDeviceType.Direct3D9:
#endif
        case UnityEngine.Rendering.GraphicsDeviceType.Direct3D11:
        case UnityEngine.Rendering.GraphicsDeviceType.Direct3D12:
          for (int i = 0; i < 4; i++) {
            projectionMatrix[1, i] = -projectionMatrix[1, i];
          }
          // Scale and bias from OpenGL -> D3D depth range
          for (int i = 0; i < 4; i++) {
            projectionMatrix[2, i] = projectionMatrix[2, i] * 0.5f
                                   + projectionMatrix[3, i] * 0.5f;
          }
          break;
      }

      // Update Image Warping
      Vector3 pastPosition; Quaternion pastRotation;
      transformHistory.SampleTransform(imageTimeStamp
                                         - (long)(warpingAdjustment * 1000f)
                                         - (long)(_smoothedTrackingLatency.value),
                                       out pastPosition, out pastRotation);

      // Use _tweenImageWarping
      var currCenterRotation = InputTracking.GetLocalRotation(XRNode.CenterEye);
      var imageReferenceRotation = _disableTemporalWarping ? currCenterRotation
                                                           : pastRotation;

      Quaternion imageQuatWarp = Quaternion.Inverse(currCenterRotation)
                                 * imageReferenceRotation;
      imageQuatWarp = Quaternion.Euler(imageQuatWarp.eulerAngles.x,
                                       imageQuatWarp.eulerAngles.y,
                                      -imageQuatWarp.eulerAngles.z);
      Matrix4x4 imageMatWarp = projectionMatrix
                               * Matrix4x4.TRS(Vector3.zero, imageQuatWarp, Vector3.one)
                               * projectionMatrix.inverse;
      Shader.SetGlobalMatrix("_LeapGlobalWarpedOffset", imageMatWarp);
    }

    protected virtual void OnEnable() {
      resetTransforms();
    }

    protected virtual void OnDisable() {
      resetTransforms();
    }

    /*
    * Resets the Global Hand Transform Shader Matrices
    */
    protected void resetTransforms() {
      _transformArray[0] = Matrix4x4.identity;
      _transformArray[1] = Matrix4x4.identity;
      Shader.SetGlobalMatrixArray(HAND_ARRAY, _transformArray);
    }
    
    /// <summary>
    /// Initializes the Leap Motion policy flags.
    /// The POLICY_OPTIMIZE_HMD flag improves tracking for head-mounted devices.
    /// </summary>
    protected override void initializeFlags() {
      if (_leapController == null) {
        return;
      }

      // Optimize for top-down tracking if on head mounted display.
      _leapController.SetPolicy(Controller.PolicyFlag.POLICY_OPTIMIZE_HMD);
    }

    protected virtual LeapTransform GetWarpedMatrix(long timestamp,
                                                    bool updateTemporalCompensation = true) {
      LeapTransform leapTransform;
      if (XRSettings.enabled && XRDevice.isPresent && transformHistory != null) {
        if (updateTemporalCompensation && transformHistory.history.IsFull) {
          transformHistory.SampleTransform(timestamp
                                           - (long)(warpingAdjustment * 1000f)
                                           - (long)(_smoothedTrackingLatency.value),
                                           out warpedPosition, out warpedRotation);
        }
        
        Vector3    currentPosition;
        Quaternion currentRotation;
        transformHistory.SampleTransform(timestamp, out currentPosition,
                                                    out currentRotation);

        warpedPosition = _disableTemporalWarping ? currentPosition : warpedPosition;
        warpedRotation = _disableTemporalWarping ? currentRotation : warpedRotation;

        warpedRotation *= Quaternion.Euler(deviceTiltXAxis, 0f, 0f);
        warpedRotation *= Quaternion.Euler(-90f, 180f, 0f);

        // Yes, up corresponds to Z and forward corresponds to Y post-rotation.
        warpedPosition += warpedRotation * Vector3.up * deviceOffsetZAxis
                        + warpedRotation * Vector3.forward * deviceOffsetYAxis;

        if (transform.parent != null) {
          leapTransform = new LeapTransform(
            transform.parent.TransformPoint(warpedPosition).ToVector(),
            (transform.parent.rotation * warpedRotation).ToLeapQuaternion(),
            transform.lossyScale.ToVector() * 1e-3f);
        } else {
          leapTransform = new LeapTransform(warpedPosition.ToVector(),
                                            warpedRotation.ToLeapQuaternion(),
                                            transform.lossyScale.ToVector() * 1e-3f);
        }
        leapTransform.MirrorZ();
      } else {
        leapTransform = transform.GetLeapMatrix();
      }
      return leapTransform;
    }

    protected override void transformFrame(Frame source, Frame dest) {
      LeapTransform leapTransform = GetWarpedMatrix(source.Timestamp);
      dest.CopyFrom(source).Transform(leapTransform);
    }

    protected void transformHands(ref LeapTransform LeftHand, ref LeapTransform RightHand) {
      LeapTransform leapTransform = GetWarpedMatrix(0, false);
      LeftHand = new LeapTransform(leapTransform.TransformPoint(LeftHand.translation),
                                   leapTransform.TransformQuaternion(LeftHand.rotation));
      RightHand = new LeapTransform(leapTransform.TransformPoint(RightHand.translation),
                                    leapTransform.TransformQuaternion(RightHand.rotation));
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

        if (Application.isPlaying
            && !manualUpdateHasBeenCalledSinceUpdate
            && _leapController != null) {
          manualUpdateHasBeenCalledSinceUpdate = true;

          //Find the left and/or right hand(s) to latch
          Hand leftHand = null, rightHand = null;
          LeapTransform precullLeftHand = LeapTransform.Identity;
          LeapTransform precullRightHand = LeapTransform.Identity;
          for (int i = 0; i < CurrentFrame.Hands.Count; i++) {
            Hand updateHand = CurrentFrame.Hands[i];
            if (updateHand.IsLeft && leftHand == null) {
              leftHand = updateHand;
            } else if (updateHand.IsRight && rightHand == null) {
              rightHand = updateHand;
            }
          }

          //Determine their new Transforms
          var interpolationTime = CalculateInterpolationTime();
          _leapController.GetInterpolatedLeftRightTransform(
                            interpolationTime + (ExtrapolationAmount * 1000),
                            interpolationTime - (BounceAmount * 1000),
                            (leftHand != null ? leftHand.Id : 0),
                            (rightHand != null ? rightHand.Id : 0),
                            out precullLeftHand,
                            out precullRightHand);
          bool leftValid = precullLeftHand.translation != Vector.Zero;
          bool rightValid = precullRightHand.translation != Vector.Zero;
          transformHands(ref precullLeftHand, ref precullRightHand);

          //Calculate the delta Transforms
          if (rightHand != null && rightValid) {
            _transformArray[0] =
              Matrix4x4.TRS(precullRightHand.translation.ToVector3(),
                            precullRightHand.rotation.ToQuaternion(),
                            Vector3.one)
              * Matrix4x4.Inverse(Matrix4x4.TRS(rightHand.PalmPosition.ToVector3(),
                                                rightHand.Rotation.ToQuaternion(),
                                                Vector3.one));
          }
          if (leftHand != null && leftValid) {
            _transformArray[1] =
              Matrix4x4.TRS(precullLeftHand.translation.ToVector3(),
                            precullLeftHand.rotation.ToQuaternion(),
                            Vector3.one)
              * Matrix4x4.Inverse(Matrix4x4.TRS(leftHand.PalmPosition.ToVector3(),
                                                leftHand.Rotation.ToQuaternion(),
                                                Vector3.one));
          }

          //Apply inside of the vertex shader
          Shader.SetGlobalMatrixArray(HAND_ARRAY, _transformArray);
        }
      }
    }
  }
}