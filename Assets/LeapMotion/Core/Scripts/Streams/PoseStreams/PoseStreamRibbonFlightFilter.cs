using Leap.Unity.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Streams {

  public class PoseStreamRibbonFlightFilter : MonoBehaviour,
                                              IStreamReceiver<Pose>,
                                              IStream<Pose> {

    #region Inspector

    [Header("Canvas Alignment (Optional)")]
    
    public bool doCanvasAlignment = true;

    [DisableIf("doCanvasAlignment", isEqualTo: false)]
    public float maxAlignmentAnglePerCentimeter = 30f;

    [UnitCurve]
    [DisableIf("doCanvasAlignment", isEqualTo: false)]
    public AnimationCurve alignmentStrengthCurve = DefaultCurve.SigmoidUp;

    [Range(5f, 90f)]
    [DisableIf("doCanvasAlignment", isEqualTo: false)]
    public float minimumAlignmentAngleFromY = 30f;

    [Range(5f, 90f)]
    [DisableIf("doCanvasAlignment", isEqualTo: false)]
    public float maximumAlignmentAngleFromY = 90f;

    [Range(0f, 20f)]
    public float maxSegmentAngleToAlign = 5f;

    private void OnValidate() {
      if (minimumAlignmentAngleFromY > maximumAlignmentAngleFromY) {
        maximumAlignmentAngleFromY = minimumAlignmentAngleFromY;
      }
      if (maximumAlignmentAngleFromY < minimumAlignmentAngleFromY) {
        minimumAlignmentAngleFromY = maximumAlignmentAngleFromY;
      }
    }

    #endregion

    public event Action OnOpen  = () => { };
    public event Action<Pose> OnSend = (pose) => { };
    public event Action OnClose = () => { };

    private RingBuffer<Pose> _buffer = new RingBuffer<Pose>(3);

    public void Open() {
      _buffer.Clear();

      OnOpen();
    }

    public void Receive(Pose data) {
      _buffer.Add(data);

      if (_buffer.Count == 2) {
        Pose a = _buffer.Get(0), b = _buffer.Get(1);
        var ab = b.position - a.position;

        var handDorsal = a.rotation * Vector3.up;

        Quaternion initRot;

        var initAngle = Vector3.Angle(handDorsal, ab);
        if (initAngle < 10f) {
          var handDistal = a.rotation * Vector3.forward;
          var ribbonRight = Vector3.Cross(ab, handDistal).normalized;
          var ribbonUp = Vector3.Cross(ribbonRight, ab).normalized;

          initRot = Quaternion.LookRotation(ab, ribbonUp);
        }
        else {
          var ribbonRight = Vector3.Cross(ab, handDorsal).normalized;
          var ribbonUp = Vector3.Cross(ribbonRight, ab).normalized;

          initRot = Quaternion.LookRotation(ab, ribbonUp);
        }

        var initPose = new Pose(a.position, initRot);
        _buffer.Set(0, initPose);
        OnSend(initPose);
      }

      if (_buffer.IsFull) {
        Pose a = _buffer.Get(0), b = _buffer.Get(1), c = _buffer.Get(2);

        var rolllessRot = getRolllessRotation(a, b, c);

        var midPoseRot = Quaternion.Slerp(a.rotation, rolllessRot, 0.5f);
        var midPose = new Pose(b.position, midPoseRot);

        // Canvas alignment.
        if (doCanvasAlignment) {
          var ribbonNormal = midPoseRot * Vector3.up;
          var canvasNormal = b.rotation * Vector3.up;
          var ribbonForward = midPoseRot * Vector3.forward;

          if (Vector3.Dot(ribbonNormal, canvasNormal) < 0f) {
            canvasNormal *= -1f;
          }

          var ribbonCanvasAngle = Vector3.SignedAngle(ribbonNormal, canvasNormal,
                                                      ribbonForward);

          var alignmentStrengthParam = Mathf.Abs(ribbonCanvasAngle)
                                              .Map(maximumAlignmentAngleFromY,
                                                   minimumAlignmentAngleFromY,
                                                   0f, 1f);
          var alignmentStrength = alignmentStrengthCurve.Evaluate(alignmentStrengthParam);
          var aligningAngle = alignmentStrength * ribbonCanvasAngle;

          // Limit canvas alignment based on how much the pitch is currently changing.
          var right = a.rotation * Vector3.right;
          var forward = a.rotation * Vector3.forward;
          var bc = (c.position - b.position);
          var bc_pitchProjection = Vector3.ProjectOnPlane(bc, right);
          var pitchAngle = Vector3.Angle(forward, bc_pitchProjection);
          alignmentStrength *= pitchAngle.Map(0f, maxSegmentAngleToAlign, 1f, 0f);

          var abDistInCM = (b.position - a.position).magnitude * 100f;
          var maxAngleCorrection = abDistInCM * maxAlignmentAnglePerCentimeter
                                   * alignmentStrength;
          aligningAngle = Mathf.Clamp(aligningAngle,
                                      -1f * maxAngleCorrection, maxAngleCorrection);

          var aligningRot = Quaternion.AngleAxis(aligningAngle, ribbonForward);
          var alignedRot = aligningRot * midPoseRot;

          midPose = new Pose(midPose.position, alignedRot);
        }

        _buffer.Set(1, midPose);
        OnSend(midPose);
      }
    }

    /// <summary>
    /// Returns the "roll-less" resulting orientation when the rotation at A traverses
    /// the path AB and then BC.
    /// 
    /// Imagine an airplane traveling along AB and then BC,
    /// that cannot adjust its roll but can adjust its pitch and then its yaw,
    /// to match its forward vector to align with BC.
    /// 
    /// This function returns the _final_ orientation when the required roll-less
    /// rotation is applied to the rotation at A; the Pose at A must contain identity
    /// rotation if you seek the delta rotation only.
    /// </summary>
    private Quaternion getRolllessRotation(Pose a, Pose b, Pose c) {
      var ab = b.position - a.position;
      var bc = c.position - b.position;

      ab = ab.normalized;
      bc = bc.normalized;

      var right = a.rotation * Vector3.right;
      var up = a.rotation * Vector3.up;
      var forward = a.rotation * Vector3.forward;

      var bc_pitchProjection = Vector3.ProjectOnPlane(bc, right);
      var pitchAngle = Vector3.SignedAngle(forward, bc_pitchProjection, right);

      var Q_right_theta = Quaternion.AngleAxis(pitchAngle, right);
      var newUp = Q_right_theta * up;
      var newForward = Q_right_theta * forward;

      var yawAngle = Vector3.SignedAngle(newForward, bc, newUp);
      var Q_reachBC = Quaternion.AngleAxis(yawAngle, newUp);

      var rolllessRot = Q_reachBC * Q_right_theta * a.rotation;
      return rolllessRot;
    }

    public void Close() {
      Pose a = _buffer.Get(0), b = _buffer.Get(1), c = _buffer.Get(2);
      var rolllessRot = getRolllessRotation(a, b, c);

      var finalPose = new Pose(c.position, rolllessRot);
      OnSend(finalPose);

      OnClose();
    }

  }

}
