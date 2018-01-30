/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
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
    private EyeType _eyeType = new EyeType(EyeType.OrderType.CENTER);

    [SerializeField]
    private bool _useCustomBaseline = false;

    [MinValue(0), Units("MM"), InspectorName("Baseline")]
    [SerializeField]
    private float _customBaselineValue = 60;

    private LeapXRServiceProvider _provider;
    private Matrix4x4 _finalCenterMatrix;
    private float? _baseline = null;

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
      _baseline = device.Baseline;
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
      _provider.OnDeviceSafe -= onDevice;
    }

    private void Update() {
      _eyeType.Reset();
    }

    private void OnPreCull() {
      _camera.ResetWorldToCameraMatrix();
      _finalCenterMatrix = _camera.worldToCameraMatrix;
    }

    private void OnPreRender() {
      _eyeType.BeginCamera(); // swaps eye

      float? baselineToUse = null;
      if (_useCustomBaseline) {
        baselineToUse = _customBaselineValue;
      } else if (_baseline.HasValue) {
        baselineToUse = _baseline.Value;
      }

      if (baselineToUse.HasValue) {
        Matrix4x4 offsetMatrix;

        offsetMatrix = _finalCenterMatrix;
        Vector3 ipdOffset = (_eyeType.IsLeftEye ? 1 : -1) * transform.right * baselineToUse.Value * 0.5f * 1e-3f;
        Vector3 forwardOffset = -transform.forward * _provider.deviceOffsetZAxis;
        offsetMatrix *= Matrix4x4.TRS(ipdOffset + forwardOffset, Quaternion.identity, Vector3.one);

        _camera.worldToCameraMatrix = offsetMatrix;
      }
    }
  }
}
