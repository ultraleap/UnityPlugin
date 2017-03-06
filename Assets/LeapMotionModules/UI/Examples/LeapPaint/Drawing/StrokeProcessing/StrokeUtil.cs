using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.StrokeProcessing {

  public static class StrokeUtil {

    /// <summary>
    /// Returns the Quaternion to apply to p0 to get it to face p1.
    /// Determined by correcting the (p0 -> p1) segment's pitch first, then yaw.
    /// The roll of the segment will drift naturally over time.
    /// 
    /// Also see CalculateCanvasAlignmentRotation, which adds a Roll rotation for
    /// rolling the segment to align itself with a canvas when such an alignment is possible on the Roll axis.
    /// </summary>
    public static Quaternion CalculateRotation(StrokePoint p0, StrokePoint p1) {

      StrokePoint point = p0;
      StrokePoint nextPoint = p1;

      Vector3 T = point.rotation * Vector3.forward;
      Vector3 N = point.rotation * Vector3.up;

      Vector3 segmentDirection = (nextPoint.position - point.position).normalized;

      // Pitch correction
      Vector3 sD_TN = (Vector3.Dot(T, segmentDirection) * T + Vector3.Dot(N, segmentDirection) * N).normalized;
      Vector3 T_x_sD_TN = Vector3.Cross(T, sD_TN);
      float T_x_sD_TN_magnitude = Mathf.Clamp(T_x_sD_TN.magnitude, 0F, 1F); // Fun fact! Sometimes the magnitude of this vector is 0.000002 larger than 1F, which causes NaNs from Mathf.Asin().
      Quaternion pitchCorrection;
      if (Vector3.Dot(T, sD_TN) >= 0F) {
        pitchCorrection = Quaternion.AngleAxis(Mathf.Asin(T_x_sD_TN_magnitude) * Mathf.Rad2Deg, T_x_sD_TN.normalized);
      }
      else {
        pitchCorrection = Quaternion.AngleAxis(180F - (Mathf.Asin(T_x_sD_TN_magnitude) * Mathf.Rad2Deg), T_x_sD_TN.normalized);
      }

      // Yaw correction
      Vector3 T_pC = pitchCorrection * T;
      Vector3 T_pC_x_sD = Vector3.Cross(T_pC, segmentDirection);
      Quaternion yawCorrection = Quaternion.AngleAxis(Mathf.Asin(T_pC_x_sD.magnitude) * Mathf.Rad2Deg, T_pC_x_sD.normalized);

      return pitchCorrection * yawCorrection;

    }

    /// <summary>
    /// Returns the Quaternion to apply to p0 to get it to face p1,
    /// then an additional rotation to align it to a plane when the stroke segment's normal (rotation * Vector3.up)
    /// is close to the planeNormal.
    /// 
    /// Also provides output parameters for getting the pitch+yaw and roll rotations independently, but beware:
    /// The rollOnly rotation only works when applied on top of the pitchYaw rotation.
    /// </summary>
    public static Quaternion CalculateCanvasAlignmentRotation(StrokePoint p0, StrokePoint p1, Vector3 planeNormal, out Quaternion pitchYawOnly, out Quaternion rollOnly) {

      StrokePoint point = p0;
      StrokePoint nextPoint = p1;

      Vector3 T = point.rotation * Vector3.forward;
      Vector3 N = point.rotation * Vector3.up;

      Vector3 segmentDirection = (nextPoint.position - point.position).normalized;

      // Pitch correction
      Vector3 sD_TN = (Vector3.Dot(T, segmentDirection) * T + Vector3.Dot(N, segmentDirection) * N).normalized;
      Vector3 T_x_sD_TN = Vector3.Cross(T, sD_TN);
      float T_x_sD_TN_magnitude = Mathf.Clamp(T_x_sD_TN.magnitude, 0F, 1F); // Fun fact! Sometimes the magnitude of this vector is 0.000002 larger than 1F, which causes NaNs from Mathf.Asin().
      Quaternion pitchCorrection;
      if (Vector3.Dot(T, sD_TN) >= 0F) {
        pitchCorrection = Quaternion.AngleAxis(Mathf.Asin(T_x_sD_TN_magnitude) * Mathf.Rad2Deg, T_x_sD_TN.normalized);
      }
      else {
        pitchCorrection = Quaternion.AngleAxis(180F - (Mathf.Asin(T_x_sD_TN_magnitude) * Mathf.Rad2Deg), T_x_sD_TN.normalized);
      }

      // Yaw correction
      Vector3 T_pC = pitchCorrection * T;
      Vector3 T_pC_x_sD = Vector3.Cross(T_pC, segmentDirection);
      Quaternion yawCorrection = Quaternion.AngleAxis(Mathf.Asin(T_pC_x_sD.magnitude) * Mathf.Rad2Deg, T_pC_x_sD.normalized);

      // Roll correction (align to canvas)
      T = pitchCorrection * yawCorrection * T;
      N = pitchCorrection * yawCorrection * N;
      Vector3 planeUp = planeNormal;
      Vector3 planeDown = -planeNormal;
      Vector3 canvasDirection;
      if (Vector3.Dot(N, planeUp) >= 0F) {
        canvasDirection = planeUp;
      }
      else {
        canvasDirection = planeDown;
      }
      Vector3 B = Vector3.Cross(T, N).normalized; // binormal
      Vector3 canvasCastNB = (Vector3.Dot(N, canvasDirection) * N + Vector3.Dot(B, canvasDirection) * B);
      Vector3 N_x_canvasNB = Vector3.Cross(N, canvasCastNB.normalized);
      float N_x_canvasNB_magnitude = Mathf.Clamp(N_x_canvasNB.magnitude, 0F, 1F); // Fun fact! Sometimes the magnitude of this vector is 0.000002 larger than 1F, which causes NaNs from Mathf.Asin().
      Quaternion rollCorrection = Quaternion.AngleAxis(
        DeadzoneDampenFilter(canvasCastNB.magnitude) * Mathf.Asin(N_x_canvasNB_magnitude) * Mathf.Rad2Deg,
        N_x_canvasNB.normalized);

      pitchYawOnly = pitchCorrection * yawCorrection;
      rollOnly = rollCorrection;
      return pitchYawOnly * rollOnly;

    }

    // Assumes input from 0 to 1.
    private static float DeadzoneDampenFilter(float input) {
      float deadzone = 0.5F;
      float dampen = 0.2F;
      return Mathf.Max(0F, (input - deadzone) * dampen);
    }

  }

}