using Leap.Unity;
using UnityEngine;

namespace Leap.Unity.UI.Constraints {

  #region Constraint Utilities

  public enum Axis { X, Y, Z }

  public static class ConstraintExtensions {
    public static Vector3 ToUnitVector3(this Axis axis) {
      switch (axis) {
        case Axis.X:
          return Vector3.right;
        case Axis.Y:
          return Vector3.up;
        case Axis.Z:
        default:
          return Vector3.forward;
      }
    }
  }

  /// <summary> Add this Attribute to a field to only display it when "Constraint Debugging" is checked
  /// on a constraint. </summary>
  public class ConstraintDebuggingAttribute : System.Attribute { }

  #endregion

  [ExecuteInEditMode]
  /// <summary>
  /// Base class for Constraints, which enforce boundaries on the motion Rigidbodies.
  /// </summary>
  public abstract class ConstraintBase : MonoBehaviour {

    public static Color DefaultGizmoColor       { get { return new Color(0.7F, 0.2F, 0.5F,   1F); } }
    public static Color DefaultGizmoSubtleColor { get { return new Color(0.7F, 0.2F, 0.5F, 0.3F); } }

    [Header("Editor Constraint Debugging")]
    [SerializeField]
    public bool debugConstraint;

    protected bool _currentlyDebugEnforcingConstraint;

    /// <summary> Store the current e.g. localPosition or localRotation
    /// as the basis of the constraint. </summary>
    protected abstract void InitLocalBasis();

    /// <summary> Restore the stored e.g. localPosition or localRotation. </summary>
    protected abstract void ResetToLocalBasis();

    protected virtual void OnEnable() {
      InitLocalBasis();
    }

    protected virtual void Start() {
      InitLocalBasis();
    }

    protected virtual void Update() {
      if (Application.isPlaying) {
        EnforceConstraint();
      }

#if UNITY_EDITOR
      if (!Application.isPlaying) {
        if (debugConstraint && !_currentlyDebugEnforcingConstraint) {
          InitLocalBasis();
          _currentlyDebugEnforcingConstraint = true;
        }

        if (_currentlyDebugEnforcingConstraint) {
          EnforceConstraint();
        }
        else {
          InitLocalBasis();
        }

        if (!debugConstraint && _currentlyDebugEnforcingConstraint) {
          ResetToLocalBasis();
          _currentlyDebugEnforcingConstraint = false;
        }
      }
#endif
    }

    /// <summary>
    /// After calling this method, the Transform's local space should satisfy the limits
    /// defined by this constraint.
    /// </summary>
    public abstract void EnforceConstraint();

  }

}