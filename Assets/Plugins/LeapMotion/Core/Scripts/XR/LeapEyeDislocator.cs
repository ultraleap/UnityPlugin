/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity {
  using Attributes;

  /// <summary>
  /// Moves the camera to each eye position on pre-render. Only necessary for image
  /// pass-through (IR viewer) scenarios.
  /// </summary>
  [RequireComponent(typeof(LeapXRServiceProvider))]
  public class LeapEyeDislocator : MonoBehaviour {

    [SerializeField]
    private bool _useCustomBaseline = false;

    [MinValue(0), Units("MM"), InspectorName("Baseline")]
    [SerializeField]
    private float _customBaselineValue = 64;

    [SerializeField]
    private bool _showEyePositions = false;

    private LeapServiceProvider _provider;
    private Maybe<float> _deviceBaseline = Maybe.None;
    private bool _hasVisitedPreCull = false;

    private Camera _cachedCamera;
    private Camera _camera {
      get {
        if (_cachedCamera == null) {
          _cachedCamera = GetComponent<Camera>();
        }
        return _cachedCamera;
      }
    }

    private void onDevice(Device device) {
      _deviceBaseline = Maybe.Some(device.Baseline);
    }

    private void OnEnable() {
      _provider = GetComponent<LeapServiceProvider>();
      if (_provider == null) {
        _provider = GetComponentInChildren<LeapServiceProvider>();
        if (_provider == null) {
          enabled = false;
          return;
        }
      }

      _provider.OnDeviceSafe += onDevice;
    }

    private void OnDisable() {
      _camera.ResetStereoViewMatrices();

      _provider.OnDeviceSafe -= onDevice;
    }

    private void Update() {
      _camera.ResetStereoViewMatrices();
      _hasVisitedPreCull = false;
    }

    private void OnPreCull() {
      if (_hasVisitedPreCull) {
        return;
      }
      _hasVisitedPreCull = true;

      Maybe<float> baselineToUse = Maybe.None;
      if (_useCustomBaseline) {
        baselineToUse = Maybe.Some(_customBaselineValue);
      } else {
        baselineToUse = _deviceBaseline;
      }

      float baselineValue;
      if (baselineToUse.TryGetValue(out baselineValue)) {
        baselineValue *= 1e-3f;

        Matrix4x4 leftMat = _camera.GetStereoViewMatrix(Camera.StereoscopicEye.Left);
        Matrix4x4 rightMat = _camera.GetStereoViewMatrix(Camera.StereoscopicEye.Right);

        Vector3 leftPos = leftMat.inverse.MultiplyPoint3x4(Vector3.zero);
        Vector3 rightPos = rightMat.inverse.MultiplyPoint3x4(Vector3.zero);
        float existingBaseline = Vector3.Distance(leftPos, rightPos);

        float baselineAdjust = baselineValue - existingBaseline;

        adjustViewMatrix(Camera.StereoscopicEye.Left, baselineAdjust);
        adjustViewMatrix(Camera.StereoscopicEye.Right, baselineAdjust);
      }
    }

    private void adjustViewMatrix(Camera.StereoscopicEye eye, float baselineAdjust) {
      float eyeOffset = eye == Camera.StereoscopicEye.Left ? 1 : -1;
      Vector3 ipdOffset = eyeOffset * Vector3.right * baselineAdjust * 0.5f;
      Vector3 providerForwardOffset = Vector3.zero, 
              providerVerticalOffset = Vector3.zero;
      Quaternion providerRotation = Quaternion.Euler(0f, 180f, 0f);
      if (_provider is LeapXRServiceProvider) {
        LeapXRServiceProvider _xrProvider = _provider as LeapXRServiceProvider;
        providerForwardOffset = Vector3.forward * _xrProvider.deviceOffsetZAxis;
        providerVerticalOffset = -Vector3.up * _xrProvider.deviceOffsetYAxis;
        providerRotation = Quaternion.AngleAxis(_xrProvider.deviceTiltXAxis, Vector3.right);
      } else {
        Matrix4x4 imageMatWarp = _camera.projectionMatrix
                                   * Matrix4x4.TRS(Vector3.zero, providerRotation, Vector3.one)
                                   * _camera.projectionMatrix.inverse;
        Shader.SetGlobalMatrix("_LeapGlobalWarpedOffset", imageMatWarp);
      }

      var existingMatrix = _camera.GetStereoViewMatrix(eye);
      _camera.SetStereoViewMatrix(eye, Matrix4x4.TRS(Vector3.zero, providerRotation, Vector3.one) *
                                       Matrix4x4.Translate(providerForwardOffset + ipdOffset) *
                                       Matrix4x4.Translate(providerVerticalOffset) *
                                       existingMatrix);
    }

    private void OnDrawGizmos() {
      if (_showEyePositions && Application.isPlaying) {
        Matrix4x4 leftMat = _camera.GetStereoViewMatrix(Camera.StereoscopicEye.Left);
        Matrix4x4 rightMat = _camera.GetStereoViewMatrix(Camera.StereoscopicEye.Right);

        Vector3 leftPos = leftMat.inverse.MultiplyPoint3x4(Vector3.zero);
        Vector3 rightPos = rightMat.inverse.MultiplyPoint3x4(Vector3.zero);

        Gizmos.color = Color.white;
        Gizmos.DrawSphere(leftPos, 0.02f);
        Gizmos.DrawSphere(rightPos, 0.02f);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(leftPos, rightPos);
      }
    }
  }
}
