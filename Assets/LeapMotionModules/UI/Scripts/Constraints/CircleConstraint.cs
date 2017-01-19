using Leap.Unity.Attributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Constraints {

  public class CircleConstraint : AngularConstraint {

    [Header("Circle Configuration")]
    [Tooltip("The local-space axis around which the circular constraint applies.")]
    public Axis circleAxis = Axis.Z;
    //[Tooltip("Local-space position of the center of the circle constraint. This value is independent of this.transform.localPosition and is NOT affected by this.transform.localScale.")]
    //public Vector3 localRadialOrigin = Vector3.zero;
    [Tooltip("Local-space radius of the circle constraint. This value is NOT affected by this.transform.localScale.")]
    [MinValue(0.0001F)]
    public float radius = 0.02F;

    public override Vector3 EvaluatePositionAlongConstraint(float angleAlongConstraint) {
      return LocalCenter + Quaternion.AngleAxis(angleAlongConstraint, AxisVector) * RadiusVector;
    }

    public float GetConstraintProjectionAngle(Vector3 localPosition) {
      Vector3 originToLocalPosition = localPosition - LocalCenter;
      Vector3 onCirclePlane = Vector3.ProjectOnPlane(originToLocalPosition, AxisVector);

      Vector3 V = onCirclePlane.normalized;
      if (V == Vector3.zero) {
        return 0F;
      }
      else {
        Vector3 R = RadiusVector.normalized;
        float plusOrMinusDegreesToV = Mathf.Acos(Vector3.Dot(V, R)) * (360F / (2 * Mathf.PI));
        if (Vector3.Dot(Vector3.Cross(R, V), AxisVector) >= 0F) {
          return plusOrMinusDegreesToV;
        }
        else {
          return -1F * plusOrMinusDegreesToV;
        }
      }
    }

    public override float GetConstraintProjectionAngle() {
      return GetConstraintProjectionAngle(this.transform.localPosition);
    }

    public override void EnforceConstraint() {
      this.transform.localPosition = EvaluatePositionAlongConstraint(GetConstraintProjectionAngle());
    }

    public Vector3 LocalCenter {
      get { return constraintLocalPosition - RadiusVector; }
    }

    public Vector3 AxisVector {
      get { return circleAxis.ToUnitVector3(); }
    }

    public Vector3 RadiusVector {
      get {
        switch (circleAxis) {
          case Axis.X:
            return Vector3.forward * radius;
          case Axis.Y:
            return Vector3.right * radius;
          case Axis.Z: default:
            return Vector3.up * radius;
        }
      }
    }

    #region Gizmos

    private const float GIZMO_THICKNESS = 0.01F;

    void OnDrawGizmos() {
      Gizmos.color = DefaultGizmoColor;
      Gizmos.matrix = this.transform.parent.localToWorldMatrix;
      Gizmos.DrawSphere(LocalCenter, radius * 0.05F);
      DrawCircle(LocalCenter, AxisVector, RadiusVector);
    }

    private void DrawCircle(Vector3 center, Vector3 axisOfRotation, Vector3 radiusVector, int numSegments=32) {
      for (int i = 0; i < numSegments; i++) {
        Gizmos.DrawLine(center + Quaternion.AngleAxis((360F / numSegments) * i, axisOfRotation) * radiusVector,
                        center + Quaternion.AngleAxis((360F / numSegments) * (i + 1), axisOfRotation) * radiusVector);
      }
    }

    #endregion

  }


}