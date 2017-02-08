using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Constraints {

  public class LeapConstraint : MonoBehaviour {

    public Constraint[] constraints = new Constraint[1];

    private Rigidbody _body;

    void Awake() {
      InitBasePositions();
    }

    void Start() {
      _body = GetComponent<Rigidbody>();
      PhysicsCallbacks.OnPrePhysics += OnPrePhysics;
    }

    void OnValidate() {
      InitBasePositions();
    }

    private void InitBasePositions() {
      for (int i = 0; i < constraints.Length; i++) {
        constraints[i].basePosition = this.transform.localPosition;
      }
    }

    /// <summary> Convenience for getting/setting Rigidbody local-to-parent position. Cache the result whenever possible. </summary>
    private Vector3 RigidbodyLocalPosition {
      get {
        if (this.transform.parent == null) {
          return _body.position;
        }
        else {
          return this.transform.parent.InverseTransformPoint(_body.position);
        }
      }
      set {
        if (this.transform.parent == null) {
          _body.position = value;
        }
        else {
          _body.position = this.transform.parent.TransformPoint(value);
        }
      }
    }

    /// <summary> Conveniences for getting/setting Rigidbody local-to-parent velocity. Cache the result whenever possible. </summary>
    private Vector3 RigidbodyLocalVelocity {
      get {
        if (this.transform.parent == null) {
          return _body.velocity;
        }
        else {
          return this.transform.parent.InverseTransformVector(_body.velocity);
        }
      }
      set {
        if (this.transform.parent == null) {
          _body.velocity = value;
        }
        else {
          _body.velocity = this.transform.parent.TransformVector(value);
        }
      }
    }

    private void OnPrePhysics() {
      for (int i = 0; i < constraints.Length; i++) {
        if (_body != null) {
          // Enforce constraint on Rigidbody __local__ position and velocity.
          Vector3 bodyLocalPosition = constraints[i].Evaluate(RigidbodyLocalPosition);
          RigidbodyLocalPosition = bodyLocalPosition;

          Vector3 freePositionNextFrame = bodyLocalPosition + RigidbodyLocalVelocity * Time.fixedDeltaTime;
          Vector3 constrainedPositionNextFrame = constraints[i].Evaluate(freePositionNextFrame);
          RigidbodyLocalVelocity = (constrainedPositionNextFrame - bodyLocalPosition) / Time.fixedDeltaTime;
        }
        else {
          // No rigidbody; enforce constraint directly on the transform.
          transform.localPosition = constraints[i].Evaluate(transform.localPosition);
        }
      }
    }

    #region Constraint Implementations

    public enum ConstraintType {
      Line = 0,
      Plane = 1,
      Sphere = 2,
      Box = 3,
      Capsule = 4,
      Clip = 5
    }

    [System.Serializable]
    public struct Constraint {
      [HideInInspector]
      public string label;

      // Constraint struct contains data for all possible constraints; no one constraint uses every parameter.
      public ConstraintType type;
      public float radius;
      public Vector3 normal;
      public Vector3 center;
      public Vector3 extents;
      public Vector3 start;
      public Vector3 end;

      // Automatically set to the local position of the transform. Offsets all positions.
      public Vector3 basePosition;

      public Vector3 Evaluate(Vector3 localPosition) {
        switch (type) {
          case ConstraintType.Line:
            Vector3 line   = (end - start);
            Vector3 bStart = basePosition + start;
            Vector3 bEnd   = basePosition + end;

            if (line.x == 0F && line.y == 0F && line.z == 0F) { return start; } // Early out for line length = 0.

            // Project local position to line.
            Vector3 projectedLocalPosition = Vector3.Project(localPosition - bStart, bEnd - bStart) + bStart;

            // Clamp to line length.
            float progressAlongLine = (projectedLocalPosition - bStart).magnitude;
            progressAlongLine = Mathf.Clamp(progressAlongLine, 0F, line.magnitude);

            // Return constrained local position.
            Vector3 lineDir = (bEnd - bStart).normalized;
            return bStart + lineDir * progressAlongLine;

          default:
            throw new System.NotImplementedException();
          //case ConstraintType.Plane:
          //  if (parentTransform != null) {
          //    inTransform.localPosition = Vector3.ProjectOnPlane(inTransform.localPosition - center, normal) + center;
          //  }
          //  else {
          //    inTransform.position = Vector3.ProjectOnPlane(inTransform.position - center, normal) + center;
          //  }
          //  break;
          //case ConstraintType.Sphere:
          //  if (parentTransform != null) {
          //    float Distance = Vector3.Distance(inTransform.localPosition, center);
          //    inTransform.localPosition = ((inTransform.localPosition - center) / Distance) * Mathf.Clamp(Distance, 0f, radius);
          //  }
          //  else {
          //    float Distance = Vector3.Distance(inTransform.position, center);
          //    inTransform.position = ((inTransform.position - center) / Distance) * Mathf.Clamp(Distance, 0f, radius);
          //  }
          //  break;
        }
      }
    }

    #endregion

    #region Gizmos

    void OnDrawGizmos() {
      Gizmos.color = new Color(0.7F, 0.3F, 0.6F);

      foreach (var constraint in constraints) {
        DrawConstraintGizmo(transform, constraint);
      }
    }

    void DrawConstraintGizmo(Transform transform, Constraint constraint) {
      switch (constraint.type) {
        case ConstraintType.Line:
          DrawLineConstraint(transform, constraint); break;
        default:
          throw new System.NotImplementedException();
      }
    }

    void DrawLineConstraint(Transform transform, Constraint constraint) {
      Gizmos.matrix = transform.localToWorldMatrix;
      Gizmos.DrawLine(constraint.start, constraint.end);
    }

    #endregion

  }

}