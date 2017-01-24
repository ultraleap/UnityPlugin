using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Attributes;

namespace Leap.Unity.UI.zzOldConstraints {

  /// <summary>
  /// One-dimensional linear constraint from 0 (beginning) to 1 (end).
  /// </summary>
  public abstract class LinearConstraint : PositionConstraint {

    [ConstraintDebugging]
    [SerializeField]
    [Range(0, 1)]
    [Disable] // Readonly
    protected float _progressAlongConstraint = 0F;

    public float ProgressAlongConstraint {
      get { return _progressAlongConstraint; }
    }

    protected override void Update() {
      base.Update();

      _progressAlongConstraint = GetConstraintProjection();
    }

    /// <summary>
    /// Returns the local-space position of this Transform along this Linear Constraint
    /// evaluated at progressAlongConstaint [0...1].
    /// Hint: Use ConstraintBase.constraintLocalPosition as a base offset.
    /// </summary>
    public abstract Vector3 EvaluatePositionAlongConstraint(float progressAlongConstraint);

    /// <summary>
    /// Return the current Transform.localPosition as progress along this linear constraint, bounded [0...1].
    /// </summary>
    public abstract float GetConstraintProjection();

  }


}