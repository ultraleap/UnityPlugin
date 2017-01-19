using Leap.Unity.Attributes;
using UnityEngine;

namespace Leap.Unity.UI.Constraints {

  public struct JointAngles {
    public float theta;
    public float phi;
    public float twist;

    public JointAngles(float theta, float phi, float twist) {
      this.theta = theta; this.phi = phi; this.twist = twist;
    }
  }

  public class JointConstraint : PositionRotationConstraint {

    [ConstraintDebugging]
    [SerializeField]
    [Disable] // (Readonly)
    [Tooltip("Angle away from the joint axis.")]
    protected float _theta = 0F;

    [ConstraintDebugging]
    [SerializeField]
    [Disable] // (Readonly)
    [Tooltip("Angle around the joint axis.")]
    protected float _phi = 0F;

    [ConstraintDebugging]
    [SerializeField]
    [Disable] // (Readonly)
    [Tooltip("Twist angle of the joint arm.")]
    protected float _twist = 0F;

    [Header("Joint Configuration")]
    [Tooltip("The local-space axis away from and around which the joint rotates.")]
    public Axis jointAxis = Axis.Z;
    [Tooltip("The total angular width of the cone that traces the joint's range of motion.")]
    [MinValue(0F)]
    [MaxValue(360F)]
    public float coneAngularWidth = 120F;

    protected override void Update() {
      base.Update();

      JointAngles projectionAngles = GetConstraintProjectionAngles();
      _theta = projectionAngles.theta;
      _phi = projectionAngles.phi;
      _twist = projectionAngles.twist;
    }

    // TODO: Currently progress is read-only so this does nothing.
    public override void InitializeDebuggingConstraint() { }

    /// <summary>
    /// Returns the local-space rotation of this Transform evaluated with the input
    /// JointAngles.
    /// </summary>
    public Quaternion EvaluateRotationAlongConstraint(JointAngles jointAngles) {
      return Quaternion.identity;
    }

    /// <summary>
    /// Returns the three angles that define the current joint's rotation:
    /// Theta, angle away from the joint axis;
    /// Phi, angle around the joint axis;
    /// Twist, the twist angle of the joint arm.
    /// </summary>
    public JointAngles GetConstraintProjectionAngles() {
      return new JointAngles(0F, 0F, 0F);
    }

    public override void EnforceConstraint() {
      //this.transform.localPosition = constraintLocalPosition;
      //this.transform.localRotation = constraintLocalRotation * EvaluateRotationAlongConstraint(GetConstraintProjectionAngles());
      this.transform.localRotation = ConstraintsUtil.ConstrainRotationToConeWithTwist(this.transform.localRotation,
                                                                                      constraintLocalRotation * jointAxis.ToUnitVector3(),
                                                                                      jointAxis.ToUnitVector3(),
                                                                                      Mathf.Cos(coneAngularWidth / 2F * Mathf.Deg2Rad), 0.999F);
    }

    public Vector3 ProjectionDirection {
      get {
        switch (jointAxis) {
          case Axis.X:
            return Axis.Z.ToUnitVector3();
          case Axis.Y:
            return Axis.Z.ToUnitVector3();
          case Axis.Z: default:
            return Axis.Y.ToUnitVector3();
        }
      }
    }

    #region Gizmos

    void OnDrawGizmos() {
      Gizmos.color = DefaultGizmoColor;
      Gizmos.matrix = Matrix4x4.TRS(this.transform.position,
                                    this.transform.rotation * Quaternion.Inverse(this.transform.localRotation) * this.constraintLocalRotation,
                                    Vector3.one);

      int stepsPerQuadrant = 8;
      int numSteps = stepsPerQuadrant * 4;
      float phiStep = 360F / numSteps;
      float rayMagnitude = 0.05F;
      Vector3 ray = VisualAxisVector;
      Quaternion baseDrawRot = Quaternion.AngleAxis(coneAngularWidth / 2F,
                                                Vector3.Cross(VisualAxisVector,
                                                              Quaternion.Euler(10F, 10F, 10F) * VisualAxisVector));
      Quaternion drawRot = baseDrawRot;
      for (int i = 0; i < numSteps; i++) {
        if (i % stepsPerQuadrant == 0) Gizmos.color = DefaultGizmoColor;
        else Gizmos.color = DefaultGizmoSubtleColor;
        Gizmos.DrawRay(Vector3.zero, drawRot * ray * rayMagnitude);
        drawRot = Quaternion.AngleAxis(phiStep, VisualAxisVector) * drawRot;
      }

      Gizmos.color = DefaultGizmoColor;
      DrawJointRevolution(numSteps, baseDrawRot, phiStep, ray * rayMagnitude);

      Gizmos.color = DefaultGizmoSubtleColor;
      int numFillSteps = (int)(coneAngularWidth / 12F);
      float coneAngularStep = (coneAngularWidth / 2F) / numFillSteps;
      Quaternion fillDrawRot;
      for (int i = 0; i < numFillSteps; i++) {
        fillDrawRot = Quaternion.AngleAxis(coneAngularStep * i,
                                           Vector3.Cross(VisualAxisVector,
                                                         Quaternion.Euler(10F, 10F, 10F) * VisualAxisVector));
        DrawJointRevolution(numSteps, fillDrawRot, phiStep, ray * rayMagnitude);
      }
    }

    private void DrawJointRevolution(int numSteps, Quaternion baseDrawRotation, float phiStep, Vector3 normal) {
      Quaternion oldDrawRot = baseDrawRotation;
      Quaternion drawRot = baseDrawRotation;
      for (int i = 0; i < numSteps; i++) {
        oldDrawRot = Quaternion.AngleAxis(-phiStep, normal) * drawRot;
        Gizmos.DrawLine(oldDrawRot * normal, drawRot * normal);
        drawRot = Quaternion.AngleAxis(phiStep, normal) * drawRot;
      }
    }

    private Vector3 VisualAxisVector {
      get {
        switch (jointAxis) {
          case Axis.X:
          case Axis.Y:
            return jointAxis.ToUnitVector3();
          case Axis.Z:
          default:
            return -jointAxis.ToUnitVector3(); // Joysticks on Z should trace cones towards the player
        }
      }
    }

    #endregion
  }

}