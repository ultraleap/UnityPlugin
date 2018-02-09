/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and  confidential.                                 *
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
  /// 
  /// Note: This script will be removed in a future version of UnityModules.
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

    private LeapXRServiceProvider _provider;
    private Maybe<float> _deviceBaseline = Maybe.None;

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
      _provider = GetComponent<LeapXRServiceProvider>();
      if (_provider == null) {
        enabled = false;
        return;
      }

      _provider.OnDeviceSafe += onDevice;
    }

    private void OnDisable() {
      _camera.ResetStereoViewMatrices();

      _provider.OnDeviceSafe -= onDevice;
    }

    private void Update() {
      _camera.ResetStereoViewMatrices();
      Matrix4x4 leftMat = _camera.GetStereoViewMatrix(Camera.StereoscopicEye.Left);
      Matrix4x4 rightMat = _camera.GetStereoViewMatrix(Camera.StereoscopicEye.Right);

      Pose leftPose = leftMat.inverse.GetPose();
      Pose rightPose = rightMat.inverse.GetPose();

      Pose centerPose = new Pose() {
        position = Vector3.Lerp(leftPose.position, rightPose.position, 0.5f),
        rotation = Quaternion.Slerp(leftPose.rotation, rightPose.rotation, 0.5f)
      };

      Maybe<float> baselineToUse = Maybe.None;
      if (_useCustomBaseline) {
        baselineToUse = Maybe.Some(_customBaselineValue);
      } else {
        baselineToUse = _deviceBaseline;
      }

      float baselineValue;
      if (baselineToUse.TryGetValue(out baselineValue)) {
        var centerViewMatrix = Matrix4x4.TRS(centerPose.position, centerPose.rotation, Vector3.one).inverse;

        adjustViewMatrix(Camera.StereoscopicEye.Left, centerViewMatrix, baselineValue);
        adjustViewMatrix(Camera.StereoscopicEye.Right, centerViewMatrix, baselineValue);
      }
    }

    private void adjustViewMatrix(Camera.StereoscopicEye eye, Matrix4x4 centerMatrix, float baseline) {
      float eyeOffset = eye == Camera.StereoscopicEye.Left ? 1 : -1;
      Vector3 ipdOffset = eyeOffset * Vector3.right * baseline * 0.5f * 1e-3f;
      Vector3 providerForwardOffset = Vector3.forward * _provider.deviceOffsetZAxis;
      Vector3 providerVerticalOffset = -Vector3.up * _provider.deviceOffsetYAxis;
      Quaternion providerRotation = Quaternion.AngleAxis(_provider.deviceTiltXAxis, Vector3.right);

      _camera.SetStereoViewMatrix(eye, 
                                       Matrix4x4.Rotate(providerRotation) *
                                       Matrix4x4.Translate(providerForwardOffset + ipdOffset) *
                                       Matrix4x4.Translate(providerVerticalOffset) *
                                       centerMatrix);
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
