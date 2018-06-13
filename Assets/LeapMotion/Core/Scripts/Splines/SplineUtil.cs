using Leap.Unity.Infix;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Splines {

  using IPositionSpline = ISpline<Vector3, Vector3>;
  using IPoseSpline = ISpline<Pose, Movement>;
  public static class SplineUtil {

    #region Conversion Utils

    public static PoseToPositionSplineWrapper AsPositionSpline(this IPoseSpline poseSpline) {
      return new PoseToPositionSplineWrapper(poseSpline);
    }

    public struct PoseToPositionSplineWrapper : IPositionSpline {

      public IPoseSpline poseSpline;

      public PoseToPositionSplineWrapper(IPoseSpline poseSpline) {
        this.poseSpline = poseSpline;
      }

      #region IPositionSpline

      public float minT { get { return poseSpline.minT; } }

      public float maxT { get { return poseSpline.maxT; } }

      public Vector3 ValueAt(float t) {
        return poseSpline.ValueAt(t).position;
      }

      public Vector3 DerivativeAt(float t) {
        return poseSpline.DerivativeAt(t).velocity;
      }

      public void ValueAndDerivativeAt(float t, out Vector3 value, out Vector3 deltaValuePerSec) {
        Pose pose;
        Movement movement;
        poseSpline.ValueAndDerivativeAt(t, out pose, out movement);

        value = pose.position;
        deltaValuePerSec = movement.velocity;
      }

      #endregion

    }

    #endregion

  }

}