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
  /// Implements spatial alignment of cameras and synchronization with images
  /// </summary>
  public class LeapVRTemporalWarping : MonoBehaviour {
    private const long MAX_LATENCY = 200000;

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
    private KeyCode recenter = KeyCode.R;

    [Tooltip("Allows smooth enabling or disabling of the Image-Warping feature.  Usually should match rotation warping.")]
    [Range(0, 1)]
    [SerializeField]
    private float tweenImageWarping = 0f;

    [Tooltip("Allows smooth enabling or disabling of the Rotational warping of Leap Space.  Usually should match image warping.")]
    [Range(0, 1)]
    [SerializeField]
    private float tweenRotationalWarping = 0f;

    [Tooltip("Allows smooth enabling or disabling of the Positional warping of Leap Space.  Usually should be disabled when using image warping.")]
    [Range(0, 1)]
    [SerializeField]
    private float tweenPositionalWarping = 0f;

    [Tooltip("Controls when this script synchronizes the time warp of images.  Use LowLatency for VR, and SyncWithImages for AR.")]
    [SerializeField]
    private SyncMode syncMode = SyncMode.LOW_LATENCY;

    // Manual Time Alignment
    [Tooltip("Allow manual adjustment of the rewind time.")]
    [SerializeField]
    private bool allowManualTimeAlignment;

    [Tooltip("Timestamps and other uncertanties can lead to sub-optimal alignment, this value can be tuned to get desired alignment.")]
    [SerializeField]
    private int warpingAdjustment = 60; //Milliseconds

    [SerializeField]
    private KeyCode unlockHold = KeyCode.RightShift;

    [SerializeField]
    private KeyCode moreRewind = KeyCode.LeftArrow;

    [SerializeField]
    private KeyCode lessRewind = KeyCode.RightArrow;

    public float TweenImageWarping {
      get {
        return tweenImageWarping;
      }
      set {
        tweenImageWarping = Mathf.Clamp01(value);
      }
    }

    public float TweenRotationalWarping {
      get {
        return tweenRotationalWarping;
      }
      set {
        tweenRotationalWarping = Mathf.Clamp01(value);
      }
    }

    public float TweenPositionalWarping {
      get {
        return tweenPositionalWarping;
      }
      set {
        tweenPositionalWarping = Mathf.Clamp01(value);
      }
    }

    public SyncMode TemporalSyncMode {
      get {
        return syncMode;
      }
      set {
        syncMode = value;
      }
    }

    public float RewindAdjust {
      get {
        return warpingAdjustment;
      }
    }

    private LeapDeviceInfo deviceInfo;

    private Matrix4x4 _projectionMatrix;
    private List<TransformData> _history = new List<TransformData>();

    /// <summary>
    /// Provides the position of a Leap Anchor at a given Leap Time.  Cannot extrapolate.
    /// </summary>
    public bool TryGetWarpedTransform(WarpedAnchor anchor, out Vector3 rewoundPosition, out Quaternion rewoundRotation, long leapTime) {
      if (_headTransform == null) {
        rewoundPosition = Vector3.one;
        rewoundRotation = Quaternion.identity;
        return false;
      }

      TransformData past = transformAtTime((leapTime - warpingAdjustment * 1000) + (syncMode == SyncMode.SYNC_WITH_IMAGES?20000:0));

      // Rewind position and rotation
      if (_trackingAnchor == null) {
        rewoundRotation = past.localRotation;
        rewoundPosition = past.localPosition + rewoundRotation * Vector3.forward * deviceInfo.focalPlaneOffset;
      } else {
        rewoundRotation = _trackingAnchor.rotation * past.localRotation;
        rewoundPosition = _trackingAnchor.TransformPoint(past.localPosition) + rewoundRotation * Vector3.forward * deviceInfo.focalPlaneOffset;
      }

      switch (anchor) {
        case WarpedAnchor.CENTER:
          break;
        case WarpedAnchor.LEFT:
          rewoundPosition += rewoundRotation * Vector3.left * deviceInfo.baseline * 0.5f;
          break;
        case WarpedAnchor.RIGHT:
          rewoundPosition += rewoundRotation * Vector3.right * deviceInfo.baseline * 0.5F;
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
    public void ManualyUpdateTemporalWarping() {
      if (_trackingAnchor == null) {
        updateHistory(_headTransform.position, _headTransform.rotation);
        updateTemporalWarping(_headTransform.position, _headTransform.rotation);
      } else {
        updateHistory(_trackingAnchor.InverseTransformPoint(_headTransform.position),
                      Quaternion.Inverse(_trackingAnchor.rotation) * _headTransform.rotation);
      }
    }

#if UNITY_EDITOR
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

      if (_headTransform != null && UnityEditor.PlayerSettings.virtualRealitySupported) {
        _trackingAnchor = _headTransform.parent;
      }
    }
#endif

    protected void Start() {
      if (provider.IsConnected()) {
        deviceInfo = provider.GetDeviceInfo();
        _shouldSetLocalPosition = true;
        LeapVRCameraControl.OnValidCameraParams += onValidCameraParams;
        if (deviceInfo.type == LeapDeviceType.Invalid) {
          Debug.LogWarning("Invalid Leap Device -> enabled = false");
          enabled = false;
          return;
        }
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
      deviceInfo = provider.GetDeviceInfo();
      _shouldSetLocalPosition = true;

      if (deviceInfo.type == LeapDeviceType.Invalid) {
        Debug.LogWarning("Invalid Leap Device -> enabled = false");
        enabled = false;
        return;
      }
      //Get a callback right as rendering begins for this frame so we can update the history and warping.
      LeapVRCameraControl.OnValidCameraParams -= onValidCameraParams; //avoid multiple subscription
      LeapVRCameraControl.OnValidCameraParams += onValidCameraParams;
    }

    protected void OnEnable() {
      if (deviceInfo.type != LeapDeviceType.Invalid) {
        LeapVRCameraControl.OnValidCameraParams -= onValidCameraParams; //avoid multiple subscription
        LeapVRCameraControl.OnValidCameraParams += onValidCameraParams;
      }
    }

    protected void OnDisable() {
      LeapVRCameraControl.OnValidCameraParams -= onValidCameraParams;
    }

    protected void OnDestroy() {
      LeapVRCameraControl.OnValidCameraParams -= onValidCameraParams;
    }

    protected void Update() {
      if (_shouldSetLocalPosition) {
        transform.localPosition = transform.forward * deviceInfo.focalPlaneOffset;
        _shouldSetLocalPosition = false;
      }

      if (Input.GetKeyDown(recenter) && VRSettings.enabled && VRDevice.isPresent) {
        InputTracking.Recenter();
      }

      // Manual Time Alignment
      if (allowManualTimeAlignment) {
        if (unlockHold == KeyCode.None || Input.GetKey(unlockHold)) {
          if (Input.GetKeyDown(moreRewind)) {
            warpingAdjustment += 1;
          }
          if (Input.GetKeyDown(lessRewind)) {
            warpingAdjustment -= 1;
          }
        }
      }
    }

    protected void LateUpdate() {
      if (VRSettings.enabled) {
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

        if (syncMode == SyncMode.LOW_LATENCY) {
          updateTemporalWarping(InputTracking.GetLocalPosition(VRNode.CenterEye),
                                InputTracking.GetLocalRotation(VRNode.CenterEye));
        }
      }
    }

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
      Quaternion imageReferenceRotation = Quaternion.Slerp(currCenterRot, imagePastCenterRot, tweenImageWarping);

      Quaternion imageQuatWarp = Quaternion.Inverse(currCenterRot) * imageReferenceRotation;
      imageQuatWarp = Quaternion.Euler(imageQuatWarp.eulerAngles.x, imageQuatWarp.eulerAngles.y, -imageQuatWarp.eulerAngles.z);
      Matrix4x4 imageMatWarp = _projectionMatrix * Matrix4x4.TRS(Vector3.zero, imageQuatWarp, Vector3.one) * _projectionMatrix.inverse;

      Shader.SetGlobalMatrix("_LeapGlobalWarpedOffset", imageMatWarp);

      TransformData past = transformAtTime(rewindTime);
      Vector3 pastCenterPos = _trackingAnchor.TransformPoint(past.localPosition);
      Quaternion pastCenterRot = _trackingAnchor.rotation * past.localRotation;

      transform.position = Vector3.Lerp(currCenterPos, pastCenterPos, tweenPositionalWarping);
      transform.rotation = Quaternion.Slerp(currCenterRot, pastCenterRot, tweenRotationalWarping);

      transform.position += transform.forward * deviceInfo.focalPlaneOffset;
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
  }
}
