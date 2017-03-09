using Leap;
using Leap.Unity.UI.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TranslationHandle : TransformHandle {

  private InteractionBehaviour _interactionObj;
  private Vector3 _axisDirection;
  private Vector3 _initPosition;
  private Quaternion _initRotation;

  protected override void Start() {
    base.Start();

    _interactionObj = GetComponent<InteractionBehaviour>();
    _interactionObj.OnGraspBegin          += OnGraspBegin;
    _interactionObj.OnGraspedMovement += OnGraspedMovement;

    InitializeAxisAndOrientation();
  }

  private void InitializeAxisAndOrientation() {
    _initPosition = _interactionObj.Rigidbody.position;
    _axisDirection = _interactionObj.Rigidbody.rotation * Vector3.forward;
    _initRotation = _interactionObj.Rigidbody.rotation;
  }

  private void OnGraspBegin(Hand hand) {
    InitializeAxisAndOrientation();
  }

  private void OnGraspedMovement(Vector3 preSolvedPos, Quaternion preSolvedRot, Vector3 solvedPos, Quaternion solvedRot, Hand hand) {
    // Don't actually move the InteractionBehaviour, but calculate where it WOULD have moved.
    _interactionObj.Rigidbody.rotation = _initRotation;
    _interactionObj.Rigidbody.position = _initPosition;
    Vector3 offsetAlongAxis = Vector3.Project(solvedPos - _initPosition, _axisDirection);

    _tool.MoveTargetPosition(offsetAlongAxis);

    InitializeAxisAndOrientation();
  }

}
