using Leap.Unity.Query;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Examples.TransformHandles {

  [AddComponentMenu("")]
  public class TransformTool : MonoBehaviour {

    public GameObject targetObject;

    private HashSet<InteractionTransformHandle> _transformHandles = new HashSet<InteractionTransformHandle>();
    private InteractionTransformHandle _activeHandle;

    void Start() {
      foreach (InteractionTransformHandle handle in GetComponentsInChildren<InteractionTransformHandle>()) {
        _transformHandles.Add(handle);

        // Prevent duplicate subscriptions -- no-op if already subscribed
        handle.onHandleActivated -= onHandleActivated;
        handle.onHandleDeactivated -= onHandleDeactivated;

        handle.onHandleActivated += onHandleActivated;
        handle.onHandleDeactivated += onHandleDeactivated;
      }
    }

    private void onHandleActivated(InteractionTransformHandle handle) {
      _activeHandle = handle;
      foreach (var transformHandle in _transformHandles) {
        if (transformHandle != _activeHandle) {
          transformHandle.gameObject.SetActive(false);
        }
      }
    }

    private void onHandleDeactivated(InteractionTransformHandle handle) {
      _activeHandle = null;
      foreach (var transformHandle in _transformHandles) {
        if (transformHandle != _activeHandle) {
          transformHandle.gameObject.SetActive(true);
        }
      }
    }

    private struct PositionRotationOffset {
      public Vector3 pos; public Quaternion rot;
    }

    public void MoveTargetPosition(InteractionTransformHandle handleCausingMovement, Vector3 deltaPosition) {
      this.transform.position += deltaPosition;
      targetObject.transform.position += deltaPosition;
    }

    public void MoveTargetRotation(InteractionTransformHandle handleCausingMovement, Quaternion deltaRotation) {
      // tool maintains absolute orientation
      targetObject.transform.rotation = deltaRotation * targetObject.transform.rotation;
    }

  }

}
