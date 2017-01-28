using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Constraints {

  public class LeapConstraint : MonoBehaviour {

    public Constraint[] constraints;

    public enum ConstraintType {
      Line    = 0,
      Plane   = 1,
      Sphere  = 2,
      Box     = 3,
      Capsule = 4,
      Clip    = 5
    }

    [System.Serializable]
    public struct Constraint {
      [HideInInspector]
      public string label;

      public ConstraintType type;
      public float radius;
      public Vector3 normal;
      public Vector3 center;
      public Vector3 extents;
      public Vector3 start;
      public Vector3 end;

      public void Evaluate(Transform inTransform, Transform parentTransform, Rigidbody inBody=null) {
        switch (type) {
          case ConstraintType.Line:
            if (parentTransform != null) {
              inTransform.localPosition = Vector3.Project(inTransform.localPosition - start, end - start) + start;
              if (inBody != null) {
                Vector3 projectedVelocity = Vector3.Project(inBody.velocity, end - start);
                inBody.velocity = projectedVelocity;
                //float progress = Vector3.Dot(inTransform.localPosition - start, end - start);
              }
            }
            else {
              inTransform.position = Vector3.Project(inTransform.position - start, end - start) + start;
            }
            break;
          case ConstraintType.Plane:
            if (parentTransform != null) {
              inTransform.localPosition = Vector3.ProjectOnPlane(inTransform.localPosition - center, normal) + center;
            }
            else {
              inTransform.position = Vector3.ProjectOnPlane(inTransform.position - center, normal) + center;
            }
            break;
          case ConstraintType.Sphere:
            if (parentTransform != null) {
              float Distance = Vector3.Distance(inTransform.localPosition, center);
              inTransform.localPosition = ((inTransform.localPosition - center) / Distance) * Mathf.Clamp(Distance, 0f, radius);
            }
            else {
              float Distance = Vector3.Distance(inTransform.position, center);
              inTransform.position = ((inTransform.position - center) / Distance) * Mathf.Clamp(Distance, 0f, radius);
            }
            break;
        }
      }
    }

    void Start() {
      PhysicsCallbacks.OnPrePhysics += OnPrePhysics;

      _body = GetComponent<Rigidbody>();
    }

    private Rigidbody _body;

    private void OnPrePhysics() {
      for (int i = 0; i < constraints.Length; i++) {
        constraints[i].Evaluate(transform, transform.parent, _body);
      }
    }

  }

}