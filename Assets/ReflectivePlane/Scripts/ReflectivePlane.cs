using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;

[ExecuteInEditMode]
public class ReflectivePlane : MonoBehaviour {

  [SerializeField]
  private Camera _targetCamera;

  [SerializeField]
  private Transform _mirror;

  [SerializeField]
  private bool clipMirrorObjectsAtSurface = false;

  private Camera _cachedCamera;
  private Camera _camera {
    get {
      if (_cachedCamera == null) {
        _cachedCamera = GetComponent<Camera>();
      }
      return _cachedCamera;
    }
  }

  void OnEnable() {
    if (_targetCamera == null) {
      _targetCamera = Camera.main;
    }
    if (!Application.isPlaying) {
      #if UNITY_EDITOR
        Undo.RecordObject(_camera, "Enabled camera");
        Undo.RecordObject(_targetCamera, "Changed clear flags");
      #endif
    }else{
      _camera.clearFlags = _targetCamera.clearFlags;
      _camera.backgroundColor = _targetCamera.backgroundColor;
      _targetCamera.clearFlags = clipMirrorObjectsAtSurface ? CameraClearFlags.Depth : CameraClearFlags.Nothing;
      _camera.enabled = true;
      _camera.nearClipPlane = _targetCamera.nearClipPlane;
      _camera.farClipPlane = _targetCamera.farClipPlane;
      _camera.transform.parent = _targetCamera.transform.parent;
      _camera.transform.position = _targetCamera.transform.position;
      _camera.transform.rotation = _targetCamera.transform.rotation;
      _camera.depth = _targetCamera.depth - 1f;
    }
  }

  void OnDisable() {
#if UNITY_EDITOR
    if (!Application.isPlaying) {
      Undo.RecordObject(_camera, "Disabled camera");
      Undo.RecordObject(_targetCamera, "Changed clear flags");
    }
#endif

    if (_camera != null) _camera.enabled = false;
    if (_targetCamera != null) _targetCamera.clearFlags = CameraClearFlags.Skybox;
  }

  void OnPreCull() {
    _camera.ResetWorldToCameraMatrix();
    _camera.worldToCameraMatrix *= CalculateReflectionMatrix(getPlane());

    center = _camera.worldToCameraMatrix;
    if (hasLeft && hasRight) {
      Matrix4x4 left = center * leftOffset;
      Matrix4x4 right = center * rightOffset;

      if (clipMirrorObjectsAtSurface) {
         Vector4 leftPlane = CameraSpacePlane(left, _mirror.position, _mirror.forward, 1, 0f);
         _camera.projectionMatrix = leftProj;
         Matrix4x4 clippedLeft = _camera.CalculateObliqueMatrix(leftPlane);

         Vector4 rightPlane = CameraSpacePlane(right, _mirror.position, _mirror.forward, 1, 0f);
         _camera.projectionMatrix = rightProj;
         Matrix4x4 clippedRight = _camera.CalculateObliqueMatrix(rightPlane);

        _camera.SetStereoProjectionMatrices(clippedLeft, clippedRight);
      }

      hasLeft = false;
      hasRight = false;
    }
  }

  bool hasLeft = false, hasRight = false;
  Matrix4x4 center;
  Matrix4x4 leftProj, rightProj;
  Matrix4x4 leftOffset, rightOffset;

  void OnPreRender() {
    GL.invertCulling = true;
    //_camera.ResetWorldToCameraMatrix();
    _camera.worldToCameraMatrix *= CalculateReflectionMatrix(getPlane());

    if (!hasLeft) {
      leftProj = _camera.projectionMatrix;
      leftOffset = center.inverse * _camera.worldToCameraMatrix;
      hasLeft = true;
    } else if (!hasRight) {
      rightProj = _camera.projectionMatrix;
      rightOffset = center.inverse * _camera.worldToCameraMatrix;
      hasRight = true;
    }
  }

  void OnPostRender() {
    GL.invertCulling = false;
  }

  private Vector4 getPlane() {
    Plane plane = new Plane(_mirror.forward, _mirror.position);
    return new Vector4(plane.normal.x, plane.normal.y, plane.normal.z, plane.distance);
  }

  public static Vector4 CameraSpacePlane(Matrix4x4 worldToCameraMatrix, Vector3 pos, Vector3 normal, float sideSign, float clipPlaneOffset) {
    Vector3 offsetPos = pos + normal * clipPlaneOffset;
    Vector3 cpos = worldToCameraMatrix.MultiplyPoint(offsetPos);
    Vector3 cnormal = worldToCameraMatrix.MultiplyVector(normal).normalized * sideSign;
    return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
  }

  private static Matrix4x4 CalculateReflectionMatrix(Vector4 plane) {
    Matrix4x4 reflectionMat = new Matrix4x4();
    reflectionMat.m00 = (1F - 2F * plane[0] * plane[0]);
    reflectionMat.m01 = (-2F * plane[0] * plane[1]);
    reflectionMat.m02 = (-2F * plane[0] * plane[2]);
    reflectionMat.m03 = (-2F * plane[3] * plane[0]);

    reflectionMat.m10 = (-2F * plane[1] * plane[0]);
    reflectionMat.m11 = (1F - 2F * plane[1] * plane[1]);
    reflectionMat.m12 = (-2F * plane[1] * plane[2]);
    reflectionMat.m13 = (-2F * plane[3] * plane[1]);

    reflectionMat.m20 = (-2F * plane[2] * plane[0]);
    reflectionMat.m21 = (-2F * plane[2] * plane[1]);
    reflectionMat.m22 = (1F - 2F * plane[2] * plane[2]);
    reflectionMat.m23 = (-2F * plane[3] * plane[2]);

    reflectionMat.m30 = 0F;
    reflectionMat.m31 = 0F;
    reflectionMat.m32 = 0F;
    reflectionMat.m33 = 1F;
    return reflectionMat;
  }


}
