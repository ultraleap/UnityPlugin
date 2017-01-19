using Leap.Unity.UI.Constraints;
using Leap.Unity.Attributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PositionRotationConstraint : PositionConstraint {

  [Disable] // (Read-only)
  public Quaternion constraintLocalRotation = Quaternion.identity;

  [Disable] // (Read-only)
  [SerializeField]
#pragma warning disable 0414
  private Vector3 constraintLocalEulerRot = Vector3.zero;
#pragma warning restore 0414

  protected override void InitLocalBasis() {
    constraintLocalPosition = this.transform.localPosition;
    constraintLocalRotation = this.transform.localRotation;
    constraintLocalEulerRot = constraintLocalRotation.eulerAngles;
  }

  protected override void ResetToLocalBasis() {
    this.transform.localPosition = constraintLocalPosition;
    this.transform.localRotation = constraintLocalRotation;
  }

}
