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

    private float _globalSensitivity = 1.0F;
    private float _rollSensitivity = 2.5F;
    private float _pitchSensitivity = 200.0F;
    private float _yawSensitivity = 1.5F;

    private float _baseYokeHeight;

    private Vector3 _restLeftHandleLocalPosition;
    //private Quaternion _restLeftHandleLocalRotation;
    private Vector3 _restRightHandleLocalPosition;
    //private Quaternion _restRightHandleLocalRotation;

    void Start() {
      _baseYokeHeight = GetYokeHeadHeightFromBase();

      _restLeftHandleLocalPosition = leftHandle.transform.localPosition;
      _restRightHandleLocalPosition = rightHandle.transform.localPosition;
    }

    private void CorrectHandleRotation(InteractionBehaviour handle) {
      Quaternion rot = Quaternion.LookRotation(yokeHead.transform.position - handle.Rigidbody.position, yokeHead.up);
      float slerp = 2F * Time.fixedDeltaTime;
      handle.Rigidbody.rotation = Quaternion.Slerp(handle.Rigidbody.rotation, rot, slerp);
      handle.transform.rotation = Quaternion.Slerp(handle.Rigidbody.rotation, rot, slerp);
    }

    public float SignedAngle(Vector3 A, Vector3 B, Vector3 directionOfPositiveRotation, bool doAdd90) {
      A = Vector3.ProjectOnPlane(A, directionOfPositiveRotation).normalized;
      B = B.normalized;
      Vector3 ACrossB = Vector3.Cross(A, B);
      Vector3 crossDirectionalityBasis = directionOfPositiveRotation;
      bool add90 = Vector3.Dot(B, A) < 0;
      return (Mathf.Asin(
                Mathf.Clamp01(ACrossB.magnitude)
              ) * Mathf.Rad2Deg
              + (doAdd90 ? (add90 ? 90 : 0) : 0)) * (Vector3.Dot(ACrossB, crossDirectionalityBasis) > 0 ? 1 : -1);
    }

    private float RightRollAmountDegrees {
      get {
        Vector3 handleDisplacement = rightHandle.Rigidbody.position - leftHandle.Rigidbody.position;
        Vector3 rightBasis = yokeHead.transform.right;
        Vector3 directionalityBasis = -yokeHead.transform.forward;
        float rollAllowance = Mathf.Clamp01(Vector3.Dot(Vector3.ProjectOnPlane(yokeHead.transform.position - Camera.main.transform.position, spaceship.transform.up), spaceship.transform.forward));
        return SignedAngle(handleDisplacement, rightBasis, directionalityBasis, false) * rollAllowance;
      }
    }

    private float PitchUpAmount {
      get {
        return _baseYokeHeight - GetYokeHeadHeightFromBase();
      }
    }

    private float YawRightAmount {
      get {
        Vector3 handleDisplacement = Vector3.ProjectOnPlane(leftHandle.Rigidbody.position - rightHandle.Rigidbody.position, spaceship.transform.up);
        float yawSuppression = Mathf.Clamp01(Vector3.Dot(Vector3.ProjectOnPlane(yokeHead.transform.position - Camera.main.transform.position, spaceship.transform.up), spaceship.transform.forward));
        return SignedAngle(handleDisplacement, -spaceship.transform.right, -spaceship.transform.up, true) * (1 - yawSuppression);
      }
    }

    private float GetYokeHeadHeightFromBase() {
      return Vector3.Dot(yokeHead.transform.position - this.transform.position, spaceship.transform.up);
    }

    void FixedUpdate() {
      if (!leftHandle.isGrasped)  CorrectHandleRotation(leftHandle);
      if (!rightHandle.isGrasped) CorrectHandleRotation(rightHandle);

      if (!leftHandle.isGrasped && !rightHandle.isGrasped) {
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

      Vector3 shipAlignedAngularVelocity = spaceship.ShipAlignedAngularVelocity;

      float targetRollVelocity = RightRollAmountDegrees * _rollSensitivity * _globalSensitivity;
      float currentRollVelocity = shipAlignedAngularVelocity.z;
      float rollTorque = targetRollVelocity - currentRollVelocity;
      spaceship.AddShipAlignedTorque(rollTorque * Vector3.forward);

      float targetPitchVelocity = PitchUpAmount * _pitchSensitivity * _globalSensitivity;
      float currentPitchVelocity = shipAlignedAngularVelocity.x;
      float pitchTorque = targetPitchVelocity - currentPitchVelocity;
      spaceship.AddShipAlignedTorque(pitchTorque * Vector3.right);

      float targetYawVelocity = YawRightAmount * _yawSensitivity * _globalSensitivity;
      float currentYawVelocity = shipAlignedAngularVelocity.y;
      float yawTorque = targetYawVelocity - currentYawVelocity;
      spaceship.AddShipAlignedTorque(yawTorque * Vector3.up);
    }

    public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
      drawer.color = Color.blue;
      drawer.DrawLine(yokeHead.transform.position, yokeHead.transform.position + spaceship.transform.forward * RightRollAmountDegrees * _rollSensitivity * 0.002F);
      drawer.color = Color.red;
      drawer.DrawLine(yokeHead.transform.position, yokeHead.transform.position + spaceship.transform.right * PitchUpAmount * _pitchSensitivity * 0.002F);
      drawer.color = Color.green;
      drawer.DrawLine(yokeHead.transform.position, yokeHead.transform.position + spaceship.transform.up * YawRightAmount * _yawSensitivity * 0.002F);
    }
  }

}
