using Leap.Unity.Attributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.zzOldConstraints {

  /// <summary>
  /// One-dimensional angular constraint in degrees [0..360].
  /// </summary>
  public abstract class AngularConstraint : PositionConstraint {

    [ConstraintDebugging]
    [SerializeField]
    [Disable] // (Readonly)
    protected float _angleAlongConstraint = 0F;

    public float AngleAlongConstraint {
      get { return _angleAlongConstraint; }
    }

    protected override void Update() {
      base.Update();

      _angleAlongConstraint = GetConstraintProjectionAngle();
    }

    // TODO: Currently progress is read-only so this does nothing.
    public override void InitializeDebuggingConstraint() { }

    /// <summary>
    /// Returns the local-space position of this Transform along this AngularConstraint
    /// evaluated at progressAlongConstaint [0..360].
    /// Hint: Use ConstraintBase.constraintLocalPosition as the arc/circle origin.
    /// </summary>
    public abstract Vector3 EvaluatePositionAlongConstraint(float angleAlongConstraint);

    /// <summary>
    /// Return the current Transform.localPosition as progress along this AngularConstraint, bounded [0..360].
    /// </summary>
    public abstract float GetConstraintProjectionAngle();

  }

  /// <summary>
  /// One-dimensional angular constraint in degrees [0..360], for Quaternions.
  /// </summary>
  public abstract class AngularRotationConstraint : PositionRotationConstraint {

    [ConstraintDebugging]
    [SerializeField]
    [Disable] // (Readonly)
    protected float _angleAlongConstraint = 0F;

    public float AngleAlongConstraint {
      get { return _angleAlongConstraint; }
    }

    protected override void Update() {
      base.Update();

      _angleAlongConstraint = GetConstraintProjectionAngle();
    }

    // TODO: Currently progress is read-only so this does nothing.
    public override void InitializeDebuggingConstraint() { }

    /// <summary>
    /// Returns the local-space position of this Transform along this AngularConstraint
    /// evaluated at progressAlongConstaint [0..360].
    /// Hint: Use ConstraintBase.constraintLocalPosition as the arc/circle origin.
    /// </summary>
    public abstract Quaternion EvaluateRotationAlongConstraint(float angleAlongConstraint);

    /// <summary>
    /// Return the current Transform.localPosition as progress along this AngularConstraint, bounded [0..360].
    /// </summary>
    public abstract float GetConstraintProjectionAngle();

  }

}