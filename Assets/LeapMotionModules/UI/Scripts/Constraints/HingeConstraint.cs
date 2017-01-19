using Leap.Unity.Attributes;
using UnityEngine;

namespace Leap.Unity.UI.Constraints {

  public class HingeConstraint : AngularRotationConstraint {

    [Header("Lever Configuration")]
    [Tooltip("Whether the hinge's angular range of motion should extend from its beginning, center, or end.")]
    public Anchor hingeAnchor = Anchor.Center;
    [Tooltip("The local-space axis around which the hinge allows rotation.")]
    public Axis hingeAxis = Axis.X;
    [Tooltip("The total angular width the hinge is allowed to rotate.")]
    [MinValue(0F)]
    [MaxValue(360F)]
    public float angularWidth = 60F;

    public override Quaternion EvaluateRotationAlongConstraint(float angleAlongConstraint) {
      float axisSign = hingeAnchor == Anchor.End ? -1 : 1;
      return Quaternion.AngleAxis(angleAlongConstraint, axisSign * (hingeAxis.ToUnitVector3()));
    }

    public override float GetConstraintProjectionAngle() {
      // Normalize the local rotation.
      transform.localRotation = Quaternion.Lerp(transform.localRotation, this.transform.localRotation, 1F);

      // Find angle by projection and trig.
      Vector3 localLeverAxis = constraintLocalRotation * hingeAxis.ToUnitVector3();
      Vector3 localProjection = Vector3.ProjectOnPlane(transform.localRotation * ProjectionDirection, localLeverAxis).normalized;
      if (localProjection.magnitude < 0.0001F) {
        return 0F;
      }
      else {
        Vector3 zeroAngleProjection = constraintLocalRotation * ProjectionDirection;
        float angle = Vector3.Angle(localProjection, zeroAngleProjection);
        if (Vector3.Dot(localLeverAxis, Vector3.Cross(zeroAngleProjection, localProjection)) > 0F) {
          // positive angle
        }
        else {
          // negative angle
          angle = -angle;
        }
        if (hingeAnchor == Anchor.Beginning) {
          float cutoffAngle = (MinAngle + (MaxAngle - 360F)) / 2F;
          if (angle < cutoffAngle) angle += 360F;
          return Mathf.Clamp(angle, MinAngle, MaxAngle);
        }
        else if (hingeAnchor == Anchor.End) {
          angle = -angle;
          if (angle < 0F) {
            angle += 360F;
          }
          float cutoffAngle = 180F + (MaxAngle + (MinAngle)) / 2F;
          if (angle > cutoffAngle) {
            angle -= 360F;
          }
          return Mathf.Clamp(angle, MaxAngle, MinAngle);
        }
        else {
          return Mathf.Clamp(angle, MinAngle, MaxAngle);
        }
      }
    }

    public override void EnforceConstraint() {
      this.transform.localPosition = constraintLocalPosition;
      this.transform.localRotation = constraintLocalRotation * EvaluateRotationAlongConstraint(GetConstraintProjectionAngle());
    }

    public Vector3 ProjectionDirection {
      get {
        switch (hingeAxis) {
          case Axis.X:
            return Vector3.forward * -1F;
          case Axis.Y:
            return Vector3.forward * -1F;
          case Axis.Z:
          default:
            return Vector3.up;
        }
      }
    }

    private float MinAngle {
      get {
        switch (hingeAnchor) {
          case Anchor.Beginning: {
              return 0F;
            }
          case Anchor.Center: {
              return -angularWidth / 2F;
            }
          case Anchor.End:
          default: {
              return angularWidth;
            }
        }
      }
    }

    private float MaxAngle {
      get {
        switch (hingeAnchor) {
          case Anchor.Beginning: {
              return angularWidth;
            }
          case Anchor.Center: {
              return angularWidth / 2F;
            }
          case Anchor.End:
          default: {
              return 0F;
            }
        }
      }
    }

    #region Gizmos

    void OnDrawGizmos() {
      int numSteps = (int)(angularWidth / 6F);
      float lengthMultiplier = 0.05F;
      Vector3 rayDirection = ProjectionDirection;

      Gizmos.matrix = Matrix4x4.TRS(this.transform.position, this.transform.rotation * Quaternion.Inverse(Quaternion.Inverse(constraintLocalRotation) * transform.localRotation), Vector3.one);

      Gizmos.color = DefaultGizmoColor;
      Gizmos.DrawRay(Vector3.zero, EvaluateRotationAlongConstraint(MinAngle) * rayDirection * lengthMultiplier);
      Gizmos.DrawRay(Vector3.zero, EvaluateRotationAlongConstraint(MaxAngle) * rayDirection * lengthMultiplier);

      Gizmos.color = DefaultGizmoSubtleColor;
      for (int i = 1; i < numSteps; i++) {
        Gizmos.DrawRay(Vector3.zero, EvaluateRotationAlongConstraint(
                                      Mathf.Lerp(MinAngle, MaxAngle, (float)i / numSteps)
                                    ) * rayDirection * lengthMultiplier);
      }

      Gizmos.color = DefaultGizmoColor;
      for (int i = 0; i < numSteps; i++) {
        Gizmos.DrawLine(EvaluateRotationAlongConstraint(
                                      Mathf.Lerp(MinAngle, MaxAngle, (float)i / numSteps)
                                    ) * rayDirection * lengthMultiplier,
                       EvaluateRotationAlongConstraint(
                                      Mathf.Lerp(MinAngle, MaxAngle, (float)(i+1) / numSteps)
                                    ) * rayDirection * lengthMultiplier);
      }
    }

    //private List<float> _listCache = new List<float>(3);
    //private float MedianComponent(Vector3 v) {
    //  _listCache.Clear();
    //  _listCache.Add(v.x); _listCache.Add(v.y); _listCache.Add(v.z);
    //  _listCache.Sort();
    //  return _listCache[1];
    //}

    #endregion

  }


}