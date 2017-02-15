using Leap.Unity.Attributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.zzOldConstraints {

  /// <summary>
  /// Two-dimensional linear constraint.
  /// </summary>
  public abstract class Linear2Constraint : PositionConstraint {

    [ConstraintDebugging]
    [SerializeField]
    [Range(0, 1)]
    [Disable] // (Readonly)
    protected Vector2 _uvCoords = Vector2.zero;

    protected override void Update() {
      base.Update();

      _uvCoords = GetConstraintProjectionCoords();
    }

    // TODO: Not yet implemented; editor inspector readouts are currently read-only.
    public override void InitializeDebuggingConstraint() { }

    /// <summary>
    /// Returns the local-space position of this Transform along this Linear2Constraint
    /// evaluated at the given coordinates.
    /// Hint: Use ConstraintBase.constraintLocalPosition as a base offset.
    /// </summary>
    public abstract Vector3 EvaluatePositionOnConstraint(Vector2 coordinates);

    /// <summary>
    /// Return the current Transform.localPosition as the closest coordinates on this Linear2Constraint, each bounded [0...1].
    /// </summary>
    public abstract Vector2 GetConstraintProjectionCoords();


  }

}