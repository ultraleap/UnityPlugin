using Leap;
using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Examples.TransformHandles {

  [AddComponentMenu("")]
  public class RotationHandle : InteractionTransformHandle {

    protected virtual void OnEnable() {
      // Parent class stores the InteractionBehaviour component in _intObj.
      _intObj.OnHandGraspBegin += onGraspBegin;
      _intObj.OnGraspedMovement += onGraspedMovement;
      _intObj.OnHandGraspEnd += onGraspEnd;
    }

    protected virtual void OnDisable() {
      // Parent class stores the InteractionBehaviour component in _intObj.
      _intObj.OnHandGraspBegin -= onGraspBegin;
      _intObj.OnGraspedMovement -= onGraspedMovement;
      _intObj.OnHandGraspEnd -= onGraspEnd;
    }

    private void onGraspBegin(List<InteractionHand> hands) {
      rememberToolOffsets();
      onHandleActivated(this);
    }

    private void onGraspedMovement(Vector3 preSolvedPos, Quaternion preSolvedRot, Vector3 solvedPos, Quaternion solvedRot, List<InteractionHand> hand) {
      Vector3 dirToBody = Vector3.ProjectOnPlane((solvedPos - _tool.transform.position), preSolvedRot * Vector3.up).normalized;
      Vector3 constrainedPos = _tool.transform.position + dirToBody * _distanceFromTool;
      Quaternion constrainedRot = Quaternion.LookRotation(dirToBody, preSolvedRot * Vector3.up);
      _intObj.transform.position = constrainedPos;
      _intObj.transform.rotation = constrainedRot;
      _intObj.rigidbody.position = constrainedPos;
      _intObj.rigidbody.rotation = constrainedRot;

      Vector3 preMoveDirToBody = Vector3.ProjectOnPlane((preSolvedPos - _tool.transform.position), preSolvedRot * Vector3.up).normalized;
      float angle = Vector3.Angle(preMoveDirToBody, dirToBody);
      Vector3 axis = Vector3.Cross(preMoveDirToBody, dirToBody).normalized;
      Quaternion toolRotation = Quaternion.AngleAxis(angle, axis);
      _tool.MoveTargetRotation(this, toolRotation);
    }

    private void onGraspEnd(List<InteractionHand> hands) {
      restoreToolOffsets();
      onHandleDeactivated(this);
    }

    private Vector3 _positionFromTool;
    private float _distanceFromTool;
    private Quaternion _rotationFromTool;

    private void rememberToolOffsets() {
      _positionFromTool = _intObj.rigidbody.position - _tool.transform.position;
      _distanceFromTool = _positionFromTool.magnitude;
      _rotationFromTool = _intObj.rigidbody.rotation * Quaternion.Inverse(_tool.transform.rotation);
    }

    private void restoreToolOffsets() {
      _intObj.rigidbody.position = _tool.transform.position + _positionFromTool;
      _intObj.rigidbody.rotation = _tool.transform.rotation * _rotationFromTool;
    }

  }

}