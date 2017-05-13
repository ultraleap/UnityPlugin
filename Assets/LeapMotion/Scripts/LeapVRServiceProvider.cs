/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using UnityEngine.VR;
using System;

namespace Leap.Unity {
  /**LeapServiceProvider creates a Controller and supplies Leap Hands and images */
  public class LeapVRServiceProvider : LeapServiceProvider {
    public static event Action<Camera> OnLeftPreRender;
    public static event Action<Camera> OnRightPreRender;

    [Header("[Experimental]")]
    [Tooltip("Pass updated transform matrices to objects with materials using the VertexOffsetShader.")]
    [SerializeField]
    protected bool _updateHandInPrecull = false;

    protected TransformHistory transformHistory = new TransformHistory();
    protected bool manualUpdateHasBeenCalledSinceUpdate;
    protected Vector3 warpedPosition = Vector3.zero;
    protected Quaternion warpedRotation = Quaternion.identity;
    protected Matrix4x4[] _transformArray = new Matrix4x4[2];

    private Camera _cachedCamera;
    public const string GLOBAL_EYE_UV_OFFSET_NAME = "_LeapGlobalStereoUVOffset";
    private static Vector2 LEFT_EYE_UV_OFFSET = new Vector2(0, 0);
    private static Vector2 RIGHT_EYE_UV_OFFSET = new Vector2(0, 0.5f);
    private Matrix4x4 _projectionMatrix;
    private bool IsLeftEye = true;

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

    protected override void Start() {
      base.Start();
      _cachedCamera = GetComponent<Camera>();
    }

    protected override void Update() {
      manualUpdateHasBeenCalledSinceUpdate = false;
      base.Update();
      imageTimeStamp = leap_controller_.FrameTimestamp();
    }

    protected override long CalculateInterpolationTime(bool endOfFrame = false) {
#if UNITY_ANDROID
      return leap_controller_.Now() - 16000;
#else
      if (leap_controller_ != null) {
        return leap_controller_.Now() - (long)_smoothedTrackingLatency.value + (_updateHandInPrecull && !endOfFrame ? (long)(Time.smoothDeltaTime * S_TO_NS / Time.timeScale) : 0);
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
        InputTracking.GetLocalPosition(VRNode.CenterEye),
        InputTracking.GetLocalRotation(VRNode.CenterEye),
        leap_controller_.Now());

      LateUpdateHandTransforms(_cachedCamera);
      
      _projectionMatrix = _cachedCamera.projectionMatrix;
      
      switch (SystemInfo.graphicsDeviceType) {
        case UnityEngine.Rendering.GraphicsDeviceType.Direct3D9:
        case UnityEngine.Rendering.GraphicsDeviceType.Direct3D11:
        case UnityEngine.Rendering.GraphicsDeviceType.Direct3D12:
          for (int i = 0; i < 4; i++) {
            _projectionMatrix[1, i] = -_projectionMatrix[1, i];
          }
          // Scale and bias from OpenGL -> D3D depth range
          for (int i = 0; i < 4; i++) {
            _projectionMatrix[2, i] = _projectionMatrix[2, i] * 0.5f + _projectionMatrix[3, i] * 0.5f;
          }
          break;
      }

      //Update Image Warping
      Vector3 position; Quaternion rotation;
      transformHistory.SampleTransform(imageTimeStamp /*- ((long)_smoothedTrackingLatency.value+20000)*/, out position, out rotation);
      Quaternion imageQuatWarp = Quaternion.Inverse(InputTracking.GetLocalRotation(VRNode.CenterEye)) * rotation;
      imageQuatWarp = Quaternion.Euler(imageQuatWarp.eulerAngles.x, imageQuatWarp.eulerAngles.y, -imageQuatWarp.eulerAngles.z);
      Matrix4x4 imageMatWarp = _projectionMatrix * Matrix4x4.TRS(Vector3.zero, imageQuatWarp, Vector3.one) * _projectionMatrix.inverse;
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

    /*
     * Initializes the Leap Motion policy flags.
     * The POLICY_OPTIMIZE_HMD flag improves tracking for head-mounted devices.
     */
    protected override void initializeFlags() {
      if (leap_controller_ == null) {
        return;
      }
      //Optimize for top-down tracking if on head mounted display.
      leap_controller_.SetPolicy(Controller.PolicyFlag.POLICY_OPTIMIZE_HMD);
    }

    protected virtual LeapTransform GetWarpedMatrix(long timestamp, bool updateTemporalCompensation = true) {
      LeapTransform leapTransform;
      if (VRSettings.enabled && VRDevice.isPresent && transformHistory != null) {
        if (updateTemporalCompensation && transformHistory.history.IsFull) {
          transformHistory.SampleTransform(timestamp - ((long)_smoothedTrackingLatency.value), out warpedPosition, out warpedRotation);
        }

        warpedRotation *= Quaternion.Euler(-90f, 180f, 0f);
        warpedPosition += warpedRotation * Vector3.up * 0.07f; //Should be replaced with a device info focal plane, but it looks like the provider just shits out a new device info every time you request one.

        if (transform.parent != null) {
          leapTransform = new LeapTransform(transform.parent.TransformPoint(warpedPosition).ToVector(), (transform.parent.rotation * warpedRotation).ToLeapQuaternion(), transform.lossyScale.ToVector() * 1e-3f);
        } else {
          leapTransform = new LeapTransform(warpedPosition.ToVector(), warpedRotation.ToLeapQuaternion(), transform.lossyScale.ToVector() * 1e-3f);
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
