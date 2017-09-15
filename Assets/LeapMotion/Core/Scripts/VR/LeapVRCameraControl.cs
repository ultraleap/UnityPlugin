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
  /// Provides a variety of VR related camera utilities, for example controlling IPD and camera distance.
  /// </summary>
  //[RequireComponent(typeof(Camera))]
  public class LeapVRCameraControl : MonoBehaviour {

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

    /// <summary>
    /// Called during the left eye camera's OnPreRender Unity callback.
    /// </summary>
    public static Action<Camera> OnLeftPreRender;

    /// <summary>
    /// Called during the right eye camera's OnPreRender Unity callback.
    /// </summary>
    public static Action<Camera> OnRightPreRender;

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

      if (_eyeType.IsLeftEye) {
        //Shader.SetGlobalVector(GLOBAL_EYE_UV_OFFSET_NAME, LEFT_EYE_UV_OFFSET);
        if (OnLeftPreRender != null) OnLeftPreRender(_camera);
      }
      else {
        //Shader.SetGlobalVector(GLOBAL_EYE_UV_OFFSET_NAME, RIGHT_EYE_UV_OFFSET);
        if (OnRightPreRender != null) OnRightPreRender(_camera);
      }

      Matrix4x4 offsetMatrix;

      offsetMatrix = _finalCenterMatrix;
      //Debug.Log(_deviceInfo.baseline);
      Vector3 ipdOffset = (_eyeType.IsLeftEye ? 1 : -1) * transform.right * _deviceInfo.baseline * 0.5f;
      Vector3 forwardOffset = -transform.forward * _deviceInfo.focalPlaneOffset;
      offsetMatrix *= Matrix4x4.TRS(ipdOffset + forwardOffset, Quaternion.identity, Vector3.one);

      _camera.worldToCameraMatrix = offsetMatrix;
    }

  }

}
