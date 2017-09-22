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
using System.Collections;
using System.Collections.Generic;
using Leap.Unity.Attributes;

namespace Leap.Unity {

  /// <summary>
  /// Implements spatial alignment of cameras and synchronization with images.
  /// </summary>
  public class LeapVRTemporalWarping : MonoBehaviour {
    private const int DEFAULT_WARP_ADJUSTMENT = 17;
    private const long MAX_LATENCY = 200000;

    #region Inspector

    [SerializeField]
    private LeapServiceProvider provider;

    [Tooltip("The transform that represents the head object.")]
    [SerializeField]
    private Transform _headTransform;

    [Tooltip("The transform that is the anchor that tracking movement is relative to.  Can be null if head motion is in world space.")]
    [SerializeField]
    private Transform _trackingAnchor;

    // Spatial recalibration
    [Tooltip("Key to recenter the VR tracking space.")]
    [SerializeField]
    private KeyCode _recenter = KeyCode.R;

    [Tooltip("Allows smooth enabling or disabling of the Image-Warping feature.  Usually should match rotation warping.")]
    [Range(0, 1)]
    [SerializeField]
    private float _tweenImageWarping = 0f;
    public float tweenImageWarping {
      get {
        return _tweenImageWarping;
      }
      set {
        _tweenImageWarping = Mathf.Clamp01(value);
      }
    }

    [Tooltip("Allows smooth enabling or disabling of the Rotational warping of Leap Space.  Usually should match image warping.")]
    [Range(0, 1)]
    [SerializeField]
    private float _tweenRotationalWarping = 0f;
    public float tweenRotationalWarping {
      get {
        return _tweenRotationalWarping;
      }
      set {
        _tweenRotationalWarping = Mathf.Clamp01(value);
      }
    }

    [Tooltip("Allows smooth enabling or disabling of the Positional warping of Leap Space.  Usually should be disabled when using image warping.")]
    [Range(0, 1)]
    [SerializeField]
    private float _tweenPositionalWarping = 0f;
    public float tweenPositionalWarping {
      get {
        return _tweenPositionalWarping;
      }
      set {
        _tweenPositionalWarping = Mathf.Clamp01(value);
      }
    }

    [Tooltip("Controls when this script synchronizes the time warp of images.  Use LowLatency for VR, and SyncWithImages for AR.")]
    [SerializeField]
    private SyncMode _syncMode = SyncMode.LOW_LATENCY;
    public SyncMode temporalSyncMode {
      get {
        return _syncMode;
      }
      set {
        _syncMode = value;
      }
    }

    [Header("Advanced")]
    [Tooltip("Forces an update of the temporal warping to happen in Late Update using the existing head transform.  Should not be enabled if you " +
             "are using the default Unity VR integration!")]
    [SerializeField]
    private bool _forceCustomUpdate = false;

    // Manual Time Alignment
    [Tooltip("Allow manual adjustment of the rewind time.")]
    [SerializeField]
    private bool _allowManualTimeAlignment;
    
    [Tooltip("Time in milliseconds between the current frame's Leap position (offset from "
           + "the headset) and the time at which the Leap frame was captured. This "
           + "prevents 'swimming' behavior when the headset moves and the user's hands "
           + "don't. This value can be tuned if using a non-standard VR headset.")]
    [SerializeField]
    private int _customWarpAdjustment = DEFAULT_WARP_ADJUSTMENT; //Milliseconds
    public int warpingAdjustment {
      get {
        if (_allowManualTimeAlignment) {
          return _customWarpAdjustment;
        } else {
          return DEFAULT_WARP_ADJUSTMENT;
        }
      }
    }

    [SerializeField]
    private KeyCode _unlockHold = KeyCode.RightShift;

    [SerializeField]
    private KeyCode _moreRewind = KeyCode.LeftArrow;

    [SerializeField]
    private KeyCode _lessRewind = KeyCode.RightArrow;

    // Manual Device Offset
    private const float DEFAULT_DEVICE_OFFSET_Y_AXIS = 0f;
    private const float DEFAULT_DEVICE_OFFSET_Z_AXIS = 0.12f;
    private const float DEFAULT_DEVICE_TILT_X_AXIS = 5f;
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
           + "headset position.")]
    [SerializeField]
    [Range(-0.5F, 0.5F)]
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
           + "headset position.")]
    [SerializeField]
    [Range(-0.5F, 0.5F)]
    private float _deviceOffsetZAxis = DEFAULT_DEVICE_OFFSET_Z_AXIS;
    public float deviceOffsetZAxis {
      get {
        return _deviceOffsetZAxis;
      }
      set {
        _deviceOffsetZAxis = value;
      }
    }

    [Tooltip("Adjusts the Leap Motion device's virtual X axis tilt.")]
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

    #region Public API

    /// <summary>
    /// Provides the position of a Leap Anchor at a given Leap Time.  Cannot extrapolate.
    /// </summary>
    public bool TryGetWarpedTransform(WarpedAnchor anchor, out Vector3 rewoundPosition, out Quaternion rewoundRotation, long leapTime) {
      // Operation is only valid if we have a head transform.
      if (_headTransform == null) {
        rewoundPosition = Vector3.one;
        rewoundRotation = Quaternion.identity;
        return false;
      }

      // Prepare past transform data.
      TransformData past = transformAtTime((leapTime - warpingAdjustment * 1000) + (_syncMode == SyncMode.SYNC_WITH_IMAGES ? 20000 : 0));

      // Prepare device offset parameters.
      Quaternion deviceTilt   = Quaternion.Euler(deviceTiltXAxis, 0f, 0f);
      Vector3    deviceOffset = new Vector3(0f, deviceOffsetYAxis, deviceOffsetZAxis).CompMul(this.transform.lossyScale);

      // TODO: We no longer use deviceInfo.forwardOffset. We should consider removing it
      // entirely or more approrpriately when we collapse the rig hierarchy. 9/1/17

      // Rewind position and rotation.
      if (_trackingAnchor == null) {
        rewoundRotation = past.localRotation * deviceTilt;
        rewoundPosition = past.localPosition + rewoundRotation * deviceOffset;
      } else {
        rewoundRotation = _trackingAnchor.rotation * past.localRotation * deviceTilt;
        rewoundPosition = _trackingAnchor.TransformPoint(past.localPosition) + rewoundRotation * deviceOffset;
      }

      // Move position if we are left-eye-only or right-eye-only.
      switch (anchor) {
        case WarpedAnchor.CENTER:
          break;
        case WarpedAnchor.LEFT:
          rewoundPosition += rewoundRotation * Vector3.left * _deviceInfo.baseline * 0.5f;
          break;
        case WarpedAnchor.RIGHT:
          rewoundPosition += rewoundRotation * Vector3.right * _deviceInfo.baseline * 0.5f;
          break;
        default:
          throw new Exception("Unexpected Rewind Type " + anchor);
      }

      return true;
    }

    public bool TryGetWarpedTransform(WarpedAnchor anchor, out Vector3 rewoundPosition, out Quaternion rewoundRotation) {
      long timestamp = provider.imageTimeStamp;
      if (TryGetWarpedTransform(anchor, out rewoundPosition, out rewoundRotation, timestamp)) {
        return true;
      }

      rewoundPosition = Vector3.zero;
      rewoundRotation = Quaternion.identity;
      return false;
    }

    /// <summary>
    /// Use this method when not using Unity default VR integration.  This method should be called directly after
    /// the head transform has been updated using your input tracking solution.
    /// </summary>
    public void ManuallyUpdateTemporalWarping() {
      if (_trackingAnchor == null) {
        updateHistory(_headTransform.position, _headTransform.rotation);
        updateTemporalWarping(_headTransform.position, _headTransform.rotation);
      } else {
        updateHistory(_trackingAnchor.InverseTransformPoint(_headTransform.position),
                      Quaternion.Inverse(_trackingAnchor.rotation) * _headTransform.rotation);
      }
    }

    #endregion

    #region Unity Events

    protected void Reset() {
      _headTransform = transform.parent;
      if (_headTransform != null) {
        _trackingAnchor = _headTransform.parent;
      }
    }

    protected void OnValidate() {
      if (_headTransform == null) {
        _headTransform = transform.parent;
      }

      if (!_allowManualTimeAlignment) {
        _customWarpAdjustment = DEFAULT_WARP_ADJUSTMENT;
      }

#if UNITY_EDITOR
      if (_headTransform != null && UnityEditor.PlayerSettings.virtualRealitySupported) {
        _trackingAnchor = _headTransform.parent;
      }
#endif
    }

    protected void OnEnable() {
      LeapVRCameraControl.OnValidCameraParams -= onValidCameraParams; //avoid multiple subscription
      LeapVRCameraControl.OnValidCameraParams += onValidCameraParams;
    }

    protected void OnDisable() {
      LeapVRCameraControl.OnValidCameraParams -= onValidCameraParams;
    }

    protected void Start() {
      if (provider.IsConnected()) {
        _deviceInfo = provider.GetDeviceInfo();
        _shouldSetLocalPosition = true;
        LeapVRCameraControl.OnValidCameraParams += onValidCameraParams;
      } else {
        StartCoroutine(waitForConnection());
        Controller controller = provider.GetLeapController();
        controller.Device += OnDevice;
      }
    }

    private IEnumerator waitForConnection() {
      while (!provider.IsConnected()) {
        yield return null;
      }
      LeapVRCameraControl.OnValidCameraParams -= onValidCameraParams; //avoid multiple subscription
      LeapVRCameraControl.OnValidCameraParams += onValidCameraParams;
    }

    private bool _shouldSetLocalPosition = false;
    protected void OnDevice(object sender, DeviceEventArgs args) {
      _deviceInfo = provider.GetDeviceInfo();
      _shouldSetLocalPosition = true;
      
      //Get a callback right as rendering begins for this frame so we can update the history and warping.
      LeapVRCameraControl.OnValidCameraParams -= onValidCameraParams; //avoid multiple subscription
      LeapVRCameraControl.OnValidCameraParams += onValidCameraParams;
    }

    protected void Update() {
      if (_shouldSetLocalPosition) {
        transform.localPosition = transform.forward * _deviceInfo.forwardOffset;
        _shouldSetLocalPosition = false;
      }

      if (Input.GetKeyDown(_recenter) && VRSettings.enabled && VRDevice.isPresent) {
        InputTracking.Recenter();
      }

      // Manual Time Alignment
      if (_allowManualTimeAlignment) {
        if (_unlockHold == KeyCode.None || Input.GetKey(_unlockHold)) {
          if (Input.GetKeyDown(_moreRewind)) {
            _customWarpAdjustment += 1;
          }
          if (Input.GetKeyDown(_lessRewind)) {
            _customWarpAdjustment -= 1;
          }
        }
      }
    }

    protected void LateUpdate() {
      if (_forceCustomUpdate) {
        ManuallyUpdateTemporalWarping();
      } else if (VRSettings.enabled) {
        updateTemporalWarping(InputTracking.GetLocalPosition(VRNode.CenterEye),
                              InputTracking.GetLocalRotation(VRNode.CenterEye));
      }
    }

    private void onValidCameraParams(LeapVRCameraControl.CameraParams cameraParams) {
      _projectionMatrix = cameraParams.ProjectionMatrix;

      if (VRSettings.enabled) {
        if (provider != null) {
          updateHistory(InputTracking.GetLocalPosition(VRNode.CenterEye),
                        InputTracking.GetLocalRotation(VRNode.CenterEye));
        }

        if (_syncMode == SyncMode.LOW_LATENCY) {
          updateTemporalWarping(InputTracking.GetLocalPosition(VRNode.CenterEye),
                                InputTracking.GetLocalRotation(VRNode.CenterEye));
        }
      }
    }

    #endregion

    #region Temporal Warping

    private LeapDeviceInfo _deviceInfo;
    private Matrix4x4 _projectionMatrix;
    private List<TransformData> _history = new List<TransformData>();

    private void updateHistory(Vector3 currLocalPosition, Quaternion currLocalRotation) {
      long leapNow = provider.GetLeapController().Now();
      _history.Add(new TransformData() {
        leapTime = leapNow,
        localPosition = currLocalPosition,
        localRotation = currLocalRotation
      });

      // Reduce history length
      while (_history.Count > 0 &&
             MAX_LATENCY < leapNow - _history[0].leapTime) {
        _history.RemoveAt(0);
      }
    }

    private void updateTemporalWarping(Vector3 currLocalPosition, Quaternion currLocalRotation) {
      if (_trackingAnchor == null || provider.GetLeapController() == null) {
        return;
      }

      Vector3 currCenterPos = _trackingAnchor.TransformPoint(currLocalPosition);
      Quaternion currCenterRot = _trackingAnchor.rotation * currLocalRotation;

      //Get the transform at the time when the latest frame was captured
      long rewindTime = provider.CurrentFrame.Timestamp - warpingAdjustment * 1000;
      long imageRewindTime = provider.imageTimeStamp - warpingAdjustment * 1000;

      TransformData imagePast = transformAtTime(imageRewindTime);
      Quaternion imagePastCenterRot = _trackingAnchor.rotation * imagePast.localRotation;

      //Apply only a rotation ~ assume all objects are infinitely distant
      Quaternion imageReferenceRotation = Quaternion.Slerp(currCenterRot, imagePastCenterRot, _tweenImageWarping);

      Quaternion imageQuatWarp = Quaternion.Inverse(currCenterRot) * imageReferenceRotation;
      imageQuatWarp = Quaternion.Euler(imageQuatWarp.eulerAngles.x, imageQuatWarp.eulerAngles.y, -imageQuatWarp.eulerAngles.z);
      Matrix4x4 imageMatWarp = _projectionMatrix * Matrix4x4.TRS(Vector3.zero, imageQuatWarp, Vector3.one) * _projectionMatrix.inverse;

      Shader.SetGlobalMatrix("_LeapGlobalWarpedOffset", imageMatWarp);

      TransformData past = transformAtTime(rewindTime);
      Vector3 pastCenterPos = _trackingAnchor.TransformPoint(past.localPosition);
      Quaternion pastCenterRot = _trackingAnchor.rotation * past.localRotation;

      transform.position = Vector3.Lerp(currCenterPos, pastCenterPos, _tweenPositionalWarping);
      transform.rotation = Quaternion.Slerp(currCenterRot, pastCenterRot, _tweenRotationalWarping);

      transform.position += transform.forward * deviceOffsetZAxis;
    }

    /* Returns the VR Center Eye Transform information interpolated to the given leap timestamp.  If the desired
     * timestamp is outside of the recorded range, interpolation will fail and the returned transform will not
     * have the desired time.
     */
    private TransformData transformAtTime(long time) {
      if (_history.Count == 0) {
        return new TransformData() {
          leapTime = 0,
          localPosition = Vector3.zero,
          localRotation = Quaternion.identity
        };
      }

      if (_history[0].leapTime >= time) {
        // Expect this when using LOW LATENCY image retrieval, which can yield negative latency estimates due to incorrect clock synchronization
        return _history[0];
      }

      int t = 1;
      while (t < _history.Count &&
             _history[t].leapTime <= time) {
        t++;
      }

      if (!(t < _history.Count)) {
        // Expect this for initial frames which will have a very low frame rate
        return _history[_history.Count - 1];
      }

      return TransformData.Lerp(_history[t - 1], _history[t], time);
    }

    #endregion

    #region Support

    public enum WarpedAnchor {
      CENTER,
      LEFT,
      RIGHT,
    }

    public enum SyncMode {
      /* SyncWithImages causes both Images and the Transform to be updated at the same time during LateUpdate.  This causes
       * the images to line up properly, but the images will lag behind the virtual world, causing drift. */
      SYNC_WITH_IMAGES,
      /* LowLatency causes the Images to be warped directly prior to rendering, causing them to line up better with virtual
       * objects.  Since transforms cannot be modified at this point in the update step, the Transform will still be updated
       * during LateUpdate, causing a misalignment between images and leap space. */
      LOW_LATENCY
    }

    protected struct TransformData {
      public long leapTime; // microseconds
      public Vector3 localPosition; //meters
      public Quaternion localRotation; //magic

      public static TransformData Lerp(TransformData from, TransformData to, long time) {
        if (from.leapTime == to.leapTime) {
          return from;
        }
        float fraction = (float)(time - from.leapTime) / (float)(to.leapTime - from.leapTime);
        return new TransformData() {
          leapTime = time,
          localPosition = Vector3.Lerp(from.localPosition, to.localPosition, fraction),
          localRotation = Quaternion.Slerp(from.localRotation, to.localRotation, fraction)
        };
      }
    }

    #endregion

  }

}
