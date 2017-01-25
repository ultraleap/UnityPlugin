using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Constraints {

  public class LeapConstraint : MonoBehaviour {

    public Constraint[] constraints;

    public enum ConstraintType {
      Line,
      Plane,
      Sphere,
      Box,
      Capsule,
      Clip
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

      public void Evaluate(Transform inTransform, Transform refTransform) {
        switch (type) {
          case ConstraintType.Line:
            if (refTransform != null) {
              inTransform.localPosition = Vector3.Project(inTransform.localPosition - start, end - start) + start;
            }
            else {
              inTransform.position = Vector3.Project(inTransform.position - start, end - start) + start;
            }
            break;
          case ConstraintType.Plane:
            if (refTransform != null) {
              inTransform.localPosition = Vector3.ProjectOnPlane(inTransform.localPosition - center, normal) + center;
            }
            else {
              inTransform.position = Vector3.ProjectOnPlane(inTransform.position - center, normal) + center;
            }
            break;
          case ConstraintType.Sphere:
            if (refTransform != null) {
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

    void Update() {
      for (int i = 0; i < constraints.Length; i++) {
        constraints[i].Evaluate(transform, transform.parent);
      }
    }

  }

}