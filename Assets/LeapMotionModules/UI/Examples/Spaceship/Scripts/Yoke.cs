using Leap.Unity.RuntimeGizmos;
using Leap.Unity.UI.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Examples {

  public class Yoke : MonoBehaviour, IRuntimeGizmoComponent {

    public Spaceship spaceship;

    public Transform yokeHead;
    public InteractionBehaviour leftHandle;
    public InteractionBehaviour rightHandle;

    private float _rollSensitivity = 1.0F;
    private float _pitchSensitivity = 200.0F;

    private float _baseHandleHeightDelta = 0F;
    private float _baseYokeHeight;
    private float _baseHandleDepthDelta = 0F;

    private Vector3 _restLeftHandleLocalPosition;
    //private Quaternion _restLeftHandleLocalRotation;
    private Vector3 _restRightHandleLocalPosition;
    //private Quaternion _restRightHandleLocalRotation;

    void Start() {
      leftHandle.OnGraspedMovement += OnLeftHandlePostGraspedMovement;
      rightHandle.OnGraspedMovement += OnRightHandlePostGraspedMovement;

      _baseYokeHeight = GetYokeHeadHeightFromBase();

      _restLeftHandleLocalPosition = leftHandle.transform.localPosition;
      _restRightHandleLocalPosition = rightHandle.transform.localPosition;
    }

    private void OnLeftHandlePostGraspedMovement(Vector3 newPos, Quaternion newRot, Hand hand) {
      //CorrectHandleRotation(leftHandle);
    }

    private void OnRightHandlePostGraspedMovement(Vector3 newPos, Quaternion newRot, Hand hand) {
      //CorrectHandleRotation(rightHandle);
    }

    private void CorrectHandleRotation(InteractionBehaviour handle) {
      Quaternion rot = Quaternion.LookRotation(yokeHead.transform.position - handle.Rigidbody.position, yokeHead.up);
      float slerp = 2F * Time.fixedDeltaTime;
      handle.Rigidbody.rotation = Quaternion.Slerp(handle.Rigidbody.rotation, rot, slerp);
      handle.transform.rotation = Quaternion.Slerp(handle.Rigidbody.rotation, rot, slerp);
    }

    private float RightRollAmountDegrees {
      get {
        Vector3 A = yokeHead.transform.right, B = (yokeHead.transform.position - leftHandle.transform.position).normalized;
        Vector3 ACrossB = Vector3.Cross(A, B);
        Vector3 crossDirectionalityBasis = yokeHead.transform.forward;
        bool add90 = Vector3.Dot((yokeHead.transform.position - leftHandle.transform.position), yokeHead.transform.right) < 0;
        return (Mathf.Asin(
                  Mathf.Clamp01(ACrossB.magnitude)
                ) * Mathf.Rad2Deg
               + (add90 ? 90 : 0)) * (Vector3.Dot(ACrossB, crossDirectionalityBasis) > 0 ? 1 : -1);
      }
    }


    // TODO: Test, and refactor the above and below to use this
    public float SignedAngle(Vector3 A, Vector3 B, Vector3 directionOfPositiveRotation) {
      A = A.normalized;
      B = B.normalized;
      Vector3 ACrossB = Vector3.Cross(A, B);
      Vector3 crossDirectionalityBasis = yokeHead.transform.right;
      bool add90 = Vector3.Dot(B, yokeHead.transform.forward) < 0;
      return (Mathf.Asin(
                Mathf.Clamp01(ACrossB.magnitude)
              ) * Mathf.Rad2Deg
              + (add90 ? 90 : 0)) * (Vector3.Dot(ACrossB, crossDirectionalityBasis) > 0 ? 1 : -1);
    }

    private float PitchUpAmount {
      get {
        return GetYokeHeadHeightFromBase() - _baseYokeHeight;
      }
    }

    private float GetYokeHeadHeightFromBase() {
      return Vector3.Dot(yokeHead.transform.position - this.transform.position, spaceship.transform.up);
    }

    void FixedUpdate() {
      if (!leftHandle.IsGrasped)  CorrectHandleRotation(leftHandle);
      if (!rightHandle.IsGrasped) CorrectHandleRotation(rightHandle);

      if (!leftHandle.IsGrasped && !rightHandle.IsGrasped) {
        float positionLerp = 10F * Time.fixedDeltaTime;

        Vector3 leftHandleTargetPosition = this.transform.TransformPoint(_restLeftHandleLocalPosition);
        Vector3 rightHandleTargetPosition = this.transform.TransformPoint(_restRightHandleLocalPosition);

        Vector3 leftLerpedPos = Vector3.Lerp(leftHandle.Rigidbody.position, leftHandleTargetPosition, positionLerp);
        leftHandle.Rigidbody.position = leftLerpedPos;
        leftHandle.transform.position = leftLerpedPos;
        Vector3 rightLerpedPos = Vector3.Lerp(rightHandle.Rigidbody.position, rightHandleTargetPosition, positionLerp);
        rightHandle.Rigidbody.position = rightLerpedPos;
        rightHandle.transform.position = rightLerpedPos;
      }

      yokeHead.transform.position = (leftHandle.Rigidbody.position + rightHandle.Rigidbody.position) / 2F;
      Vector3 toYokeBase = (this.transform.position - yokeHead.transform.position).normalized;
      yokeHead.transform.rotation = Quaternion.LookRotation(toYokeBase, spaceship.transform.up);

      float targetRollVelocity = RightRollAmountDegrees * _rollSensitivity;
      float currentRollVelocity = spaceship.ShipAlignedAngularVelocity.z;
      float rollTorque = targetRollVelocity - currentRollVelocity;
      spaceship.AddShipAlignedTorque(rollTorque * Vector3.forward);

      float targetPitchVelocity = PitchUpAmount * _pitchSensitivity;
      float currentPitchVelocity = spaceship.ShipAlignedAngularVelocity.x;
      float pitchTorque = targetPitchVelocity - currentPitchVelocity;
      spaceship.AddShipAlignedTorque(pitchTorque * Vector3.right);
    }

    public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
      drawer.color = Color.blue;
      drawer.DrawLine(yokeHead.transform.position, yokeHead.transform.position + spaceship.transform.forward * RightRollAmountDegrees * _rollSensitivity * 0.002F);
      drawer.color = Color.red;
      drawer.DrawLine(yokeHead.transform.position, yokeHead.transform.position + spaceship.transform.right * PitchUpAmount * _pitchSensitivity * 0.002F);
    }
  }

}
