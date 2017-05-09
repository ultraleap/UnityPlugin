using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Leap.Unity.Examples {

  [AddComponentMenu("")]
  public class SimpleFacingCameraCallbacks : MonoBehaviour {

    public Transform toFaceCamera;

    private bool _isFacingCamera = false;

    public UnityEvent OnBeginFacingCamera;
    public UnityEvent OnEndFacingCamera;

    void Start() {
      // Set "_isFacingCamera" to be whatever the current state ISN'T, so that we are
      // guaranteed to fire a UnityEvent on the first Update().
      _isFacingCamera = !GetIsFacingCamera(toFaceCamera, Camera.main);
    }

    void Update() {
      if (GetIsFacingCamera(toFaceCamera, Camera.main, _isFacingCamera ? 0.77F : 0.82F) != _isFacingCamera) {
        _isFacingCamera = !_isFacingCamera;

        if (_isFacingCamera) {
          OnBeginFacingCamera.Invoke();
        }
        else {
          OnEndFacingCamera.Invoke();
        }
      }
    }

    public static bool GetIsFacingCamera(Transform facingTransform, Camera camera, float minAllowedDotProduct = 0.8F) {
      return Vector3.Dot((camera.transform.position - facingTransform.position).normalized, facingTransform.forward) > minAllowedDotProduct;
    }

  }

}
