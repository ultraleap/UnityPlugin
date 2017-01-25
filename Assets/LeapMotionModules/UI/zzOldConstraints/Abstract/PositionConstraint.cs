using Leap.Unity.UI.zzOldConstraints;
using Leap.Unity.Attributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PositionConstraint : ConstraintBase {

  [Header("Constraint Basis")]
  [Disable] // (Read-only)
  public Vector3 constraintLocalPosition = Vector3.zero;

  protected override void InitLocalBasis() {
    constraintLocalPosition = this.transform.localPosition;
  }

  protected override void ResetToLocalBasis() {
    this.transform.localPosition = constraintLocalPosition;
  }

}
