/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using UnityEngine.Rendering;
using System;

namespace Leap.Unity {

  /// <summary>
  /// Moves the camera to each eye position on pre-render. Only necessary for image
  /// pass-through (IR viewer) scenarios.
  /// 
  /// Note: This script will be removed in a future version of UnityModules.
  /// </summary>
  public class LeapEyeDislocator : MonoBehaviour {

    private Matrix4x4 _finalCenterMatrix;

    private LeapDeviceInfo _deviceInfo;

    void Start() {
      _deviceInfo = LeapDeviceInfo.GetLeapDeviceInfo();
    }

    private Camera _cachedCamera;
    private Camera _camera {
      get {
        if (_cachedCamera == null) {
          _cachedCamera = GetComponent<Camera>();
        }
        return _cachedCamera;
      }
    }

    [SerializeField]
    private EyeType _eyeType = new EyeType(EyeType.OrderType.CENTER);

    void OnPreCull() {
      #if UNITY_EDITOR
      if (!Application.isPlaying) return;
      #endif

      _camera.ResetWorldToCameraMatrix();
      _finalCenterMatrix = _camera.worldToCameraMatrix;
    }

    void OnPreRender() {
      #if UNITY_EDITOR
      if (!Application.isPlaying) return;
      #endif

      _eyeType.BeginCamera(); // swaps eye

      Matrix4x4 offsetMatrix;
      
      offsetMatrix = _finalCenterMatrix;
      Vector3 ipdOffset = (_eyeType.IsLeftEye ? 1 : -1) * transform.right * _deviceInfo.baseline * 0.5f;
      Vector3 forwardOffset = -transform.forward * _deviceInfo.forwardOffset;
      offsetMatrix *= Matrix4x4.TRS(ipdOffset + forwardOffset, Quaternion.identity, Vector3.one);

      _camera.worldToCameraMatrix = offsetMatrix;
    }
  }

}
