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
  [ExecuteInEditMode]
  public class LeapVRCameraControl : MonoBehaviour {

    #region IR Viewer Support

    public const string LEAP_IMAGE_LEFT_UV_OFFSET_NAME  = "_LeapImageLeftUVOffset";
    public const string LEAP_IMAGE_RIGHT_UV_OFFSET_NAME = "_LeapImageRightUVOffset";

    private static Vector2 LEFT_IMAGE_UV_OFFSET = new Vector2(0, 0);
    private static Vector2 RIGHT_IMAGE_UV_OFFSET = new Vector2(0, 0.5f);

    /// <summary>
    /// Sets shader globals that define the UV offsets necessary to access only the left
    /// or right images from the combined image retrieved from the Leap device.
    /// </summary>
    private void setLeapImageUVOffsetGlobals() {
      Shader.SetGlobalVector(LEAP_IMAGE_LEFT_UV_OFFSET_NAME, LEFT_IMAGE_UV_OFFSET);
      Shader.SetGlobalVector(LEAP_IMAGE_RIGHT_UV_OFFSET_NAME, RIGHT_IMAGE_UV_OFFSET);
    }

    #endregion

    void Start() {
      //#if UNITY_EDITOR
      //      if (!Application.isPlaying) {
      //        return;
      //      }
      //#endif

      setLeapImageUVOffsetGlobals();

      //_deviceInfo = LeapDeviceInfo.GetLeapDeviceInfo();
    }

    //#region Inspector

    //[SerializeField]
    //private EyeType _eyeType = EyeType.CENTER;

    //[SerializeField]
    //private bool _overrideEyePosition = true;
    //public bool overrideEyePosition { get { return _overrideEyePosition; } set { _overrideEyePosition = value; } }

    //#endregion



    //#region Events

    ///// <summary>
    ///// When using VR, the cameras do not have valid parameters until the first frame
    ///// begins rendering, so if you need valid parameters for initialization, you can use
    ///// this callback to get notified when they become available.
    ///// </summary>
    //public static Action<CameraParams> OnValidCameraParams;

    //private static bool _hasDispatchedValidCameraParams = false;

    ///// <summary>
    ///// Called during the left eye camera's OnPreRender Unity callback.
    ///// </summary>
    //public static Action<Camera> OnLeftPreRender;

    ///// <summary>
    ///// Called during the right eye camera's OnPreRender Unity callback.
    ///// </summary>
    //public static Action<Camera> OnRightPreRender;

    //#endregion

//    void OnPreRender() {
//#if UNITY_EDITOR
//      if (!Application.isPlaying) {
//        return;
//      }
//#endif

//      _eyeType.BeginCamera();

//      if (_eyeType.IsLeftEye) {
//        Shader.SetGlobalVector(GLOBAL_EYE_UV_OFFSET_NAME, LEFT_EYE_UV_OFFSET);
//        if (OnLeftPreRender != null) OnLeftPreRender(_cachedCamera);
//      }
//      else {
//        Shader.SetGlobalVector(GLOBAL_EYE_UV_OFFSET_NAME, RIGHT_EYE_UV_OFFSET);
//        if (OnRightPreRender != null) OnRightPreRender(_cachedCamera);
//      }

//      Matrix4x4 offsetMatrix;

//      if (_overrideEyePosition) {
//        offsetMatrix = _finalCenterMatrix;
//        //Debug.Log(_deviceInfo.baseline);
//        Vector3 ipdOffset = (_eyeType.IsLeftEye ? 1 : -1) * transform.right * _deviceInfo.baseline * 0.5f;
//        Vector3 forwardOffset = -transform.forward * _deviceInfo.focalPlaneOffset;
//        offsetMatrix *= Matrix4x4.TRS(ipdOffset + forwardOffset, Quaternion.identity, Vector3.one);
//      }
//      else {
//        offsetMatrix = _camera.worldToCameraMatrix;
//      }

//      _camera.worldToCameraMatrix = offsetMatrix;
//    }



    //private Camera _cachedCamera;
    //private Camera _camera {
    //  get {
    //    if (_cachedCamera == null) {
    //      _cachedCamera = GetComponent<Camera>();
    //    }
    //    return _cachedCamera;
    //  }
    //}

    //private Matrix4x4 _finalCenterMatrix;

    //private LeapDeviceInfo _deviceInfo;

    //#region Unity Events

    //    void Update() {
    //#if UNITY_EDITOR
    //      _eyeType.UpdateOrderGivenComponent(this);

    //      if (!Application.isPlaying) {
    //        return;
    //      }
    //#endif

    //      _hasDispatchedValidCameraParams = false;
    //    }

    //    void OnPreCull() {
    //#if UNITY_EDITOR
    //      if (!Application.isPlaying) {
    //        return;
    //      }
    //#endif

    //      _camera.ResetWorldToCameraMatrix();
    //      _finalCenterMatrix = _camera.worldToCameraMatrix;

    //      if (!_hasDispatchedValidCameraParams) {
    //        CameraParams cameraParams = new CameraParams(_cachedCamera);

    //        if (OnValidCameraParams != null) {
    //          OnValidCameraParams(cameraParams);
    //        }

    //        _hasDispatchedValidCameraParams = true;
    //      }
    //    }

    //    void OnPreRender() {
    //#if UNITY_EDITOR
    //      if (!Application.isPlaying) {
    //        return;
    //      }
    //#endif

    //      _eyeType.BeginCamera();

    //      Matrix4x4 offsetMatrix;

    //      if (_overrideEyePosition) {
    //        offsetMatrix = _finalCenterMatrix;
    //        //Debug.Log(_deviceInfo.baseline);
    //        Vector3 ipdOffset = (_eyeType.IsLeftEye ? 1 : -1) * transform.right * _deviceInfo.baseline * 0.5f;
    //        Vector3 forwardOffset = -transform.forward * _deviceInfo.focalPlaneOffset;
    //        offsetMatrix *= Matrix4x4.TRS(ipdOffset + forwardOffset, Quaternion.identity, Vector3.one);
    //      } else {
    //        offsetMatrix = _camera.worldToCameraMatrix;
    //      }

    //      _camera.worldToCameraMatrix = offsetMatrix;
    //    }

    //public struct CameraParams {
    //  public readonly Transform CenterEyeTransform;
    //  public readonly Matrix4x4 ProjectionMatrix;
    //  public readonly int Width;
    //  public readonly int Height;

    //  public CameraParams(Camera camera) {
    //    CenterEyeTransform = camera.transform;
    //    ProjectionMatrix = camera.projectionMatrix;

    //    switch (SystemInfo.graphicsDeviceType) {
    //      case GraphicsDeviceType.Direct3D9:
    //      case GraphicsDeviceType.Direct3D11:
    //      case GraphicsDeviceType.Direct3D12:
    //        for (int i = 0; i < 4; i++) {
    //          ProjectionMatrix[1, i] = -ProjectionMatrix[1, i];
    //        }
    //        // Scale and bias from OpenGL -> D3D depth range
    //        for (int i = 0; i < 4; i++) {
    //          ProjectionMatrix[2, i] = ProjectionMatrix[2, i] * 0.5f + ProjectionMatrix[3, i] * 0.5f;
    //        }
    //        break;
    //    }

    //    Width = camera.pixelWidth;
    //    Height = camera.pixelHeight;
    //  }
    //}
  }

  //#region Support

  //[System.Serializable]
  //public enum EyeType {
  //  CENTER = 0,
  //  LEFT = 1,
  //  RIGHT = 2,
  //}

  //#endregion

}
