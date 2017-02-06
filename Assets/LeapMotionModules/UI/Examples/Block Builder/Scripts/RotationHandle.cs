using Leap;
using Leap.Unity.UI.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotationHandle : TransformHandle {

  private InteractionBehaviour _interactionObj;
  private Rigidbody Body {
    get { return _interactionObj.Rigidbody; }
  }

  void Start() {
    base.Start();

    _interactionObj = GetComponent<InteractionBehaviour>();

    _interactionObj.OnGraspBegin += OnGraspBegin;
    _interactionObj.OnPreHoldingMovement += OnPreHoldingMovement;
    _interactionObj.OnPostHoldingMovement += OnPostHoldingMovement;
    _interactionObj.OnGraspEnd += OnGraspEnd;
  }

  private void OnGraspBegin(Hand hand) {
    RememberToolOffsets();
  }

  private Vector3 _preMoveDirToBody;
  private void OnPreHoldingMovement(Vector3 preSolvePos, Quaternion preSolveRot, Hand hand) {
    _preMoveDirToBody = Vector3.ProjectOnPlane((preSolvePos - _tool.transform.position), Body.rotation * Vector3.up).normalized;
  }

  private void OnPostHoldingMovement(Vector3 solvedPos, Quaternion solvedRot, Hand hand) {
    Vector3 dirToBody = Vector3.ProjectOnPlane((solvedPos - _tool.transform.position), Body.rotation * Vector3.up).normalized;
    Body.position = _tool.transform.position + dirToBody * _distanceFromTool;
    Body.rotation = Quaternion.LookRotation(dirToBody, Body.rotation * Vector3.up);

    float angle = Vector3.Angle(_preMoveDirToBody, dirToBody);
    Vector3 axis = Vector3.Cross(_preMoveDirToBody, dirToBody).normalized;
    Quaternion toolRotation = Quaternion.AngleAxis(angle, axis);
    _tool.MoveTargetRotation(toolRotation);
  }

  private void OnGraspEnd(Hand hand) {
    RestoreToolOffsets();
  }

  private Vector3 _positionFromTool;
  private float _distanceFromTool;
  private Quaternion _rotationFromTool;

  private void RememberToolOffsets() {
    _positionFromTool = Body.position - _tool.transform.position;
    _distanceFromTool = _positionFromTool.magnitude;
    _rotationFromTool = Body.rotation * Quaternion.Inverse(_tool.transform.rotation);
  }

  private void RestoreToolOffsets() {
    Body.position = _tool.transform.position + _positionFromTool;
    Body.rotation = _tool.transform.rotation * _rotationFromTool;
  }

}
