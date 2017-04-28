using Leap;
using Leap.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Examples.TransformHandles {

  [AddComponentMenu("")]
  public class TranslationHandle : InteractionTransformHandle {

    private Vector3 _axisDirection;
    private Vector3 _initPosition;
    private Quaternion _initRotation;

    protected virtual void OnEnable() {
      // Parent class stores the InteractionBehaviour component in _intObj.
      _intObj.OnGraspBegin += onGraspBegin;
      _intObj.OnGraspedMovement += onGraspedMovement;
      _intObj.OnGraspEnd += onGraspEnd;
    }

    protected virtual void OnDisable() {
      // Parent class stores the InteractionBehaviour component in _intObj.
      _intObj.OnGraspBegin -= onGraspBegin;
      _intObj.OnGraspedMovement -= onGraspedMovement;
      _intObj.OnGraspEnd -= onGraspEnd;
    }

    protected virtual void Start() {
      initializeAxisAndOrientation();
    }

    private void initializeAxisAndOrientation() {
      _initPosition = _intObj.rigidbody.position;
      _axisDirection = _intObj.rigidbody.rotation * Vector3.forward;
      _initRotation = _intObj.rigidbody.rotation;
    }

    private void onGraspBegin(List<InteractionHand> hands) {
      initializeAxisAndOrientation();
      onHandleActivated(this);
    }

    private void onGraspedMovement(Vector3 preSolvedPos, Quaternion preSolvedRot, Vector3 solvedPos, Quaternion solvedRot, List<InteractionHand> hands) {

      // Don't actually move the InteractionBehaviour, but calculate where it WOULD have moved.
      _intObj.transform.position = _initPosition;
      _intObj.transform.rotation = _initRotation;
      _intObj.rigidbody.position = _initPosition;
      _intObj.rigidbody.rotation = _initRotation;

      Vector3 offsetAlongAxis = Vector3.Project(solvedPos - _initPosition, _axisDirection);
      _tool.MoveTargetPosition(this, offsetAlongAxis);

      initializeAxisAndOrientation();
    }

    private void onGraspEnd(List<InteractionHand> hands) {
      onHandleDeactivated(this);
    }

  }

}