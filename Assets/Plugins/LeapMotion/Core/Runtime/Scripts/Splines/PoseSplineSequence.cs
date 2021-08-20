/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity.RuntimeGizmos;
using System;
using UnityEngine;

namespace Leap.Unity.Animation {

  [Serializable]
  public struct PoseSplineSequence : IIndexable<HermitePoseSpline>,
                                     ISpline<Pose, Movement>,
                                     ISpline<Vector3, Vector3> {
    public HermitePoseSpline[] splines;
    public bool allowExtrapolation;

    public PoseSplineSequence(HermitePoseSpline[] splines,
                              bool allowExtrapolation = false) {
      this.splines = splines;
      this.allowExtrapolation = allowExtrapolation;
    }

    public HermitePoseSpline this[int idx] {
      get { return splines[idx]; }
    }

    public int Count { get { return splines.Length; } }

    public Pose PoseAt(float t) {
      Pose pose;
      Movement unusedMovement;
      PoseAndMovementAt(t, out pose, out unusedMovement);
      return pose;
    }

    public Movement MovementAt(float t) {
      Pose unusePose;
      Movement movement;
      PoseAndMovementAt(t, out unusePose, out movement);
      return movement;
    }

    public void PoseAndMovementAt(float t, out Pose pose, out Movement movement) {
      var minT = splines[0].minT;
      var maxT = splines[splines.Length - 1].maxT;

      pose = Pose.identity;
      movement = Movement.identity;

      var dt = 0f;
      Pose poseOrigin; Movement extrapMovement;
      if (t < minT) {
        if (allowExtrapolation) {
          splines[0].PoseAndMovementAt(minT, out poseOrigin, out extrapMovement);
          dt = t - minT;
          pose = poseOrigin.Integrated(extrapMovement, dt);
          movement = extrapMovement;
          return;
        }
        else {
          t = minT;
        }
      }
      else if (t > maxT) {
        if (allowExtrapolation) {
          splines[splines.Length - 1].PoseAndMovementAt(maxT, out poseOrigin, out extrapMovement);
          dt = t - maxT;
          pose = poseOrigin.Integrated(extrapMovement, dt);
          movement = extrapMovement;
          return;
        }
        else {
          t = maxT;
        }
      }

      foreach (var spline in splines) {
        if (t >= spline.minT && t <= spline.maxT) {
          pose = spline.PoseAt(t);
          movement = spline.MovementAt(t);
          return;
        }
      }

      Debug.LogError("PoseSplineSequence couldn't evaluate T: " + t);
    }

    #region ISpline<Pose, Movement>

    public float minT {
      get {
        if (splines == null || splines.Length == 0) {
          return 0;
        }
        else {
          return splines[0].minT;
        }
      }
    }
    
    public float maxT {
      get {
        if (splines == null || splines.Length == 0) {
          return 0;
        }
        else {
          return splines[splines.Length - 1].maxT;
        }
      }
    }

    public Pose ValueAt(float t) {
      return PoseAt(t);
    }

    public Movement DerivativeAt(float t) {
      return MovementAt(t);
    }

    public void ValueAndDerivativeAt(float t, out Pose value, out Movement deltaValuePerSec) {
      PoseAndMovementAt(t, out value, out deltaValuePerSec);
    }

    #endregion

    #region ISpline<Vector3, Vector3>
    
    float ISpline<Vector3, Vector3>.minT { get { return minT; } }

    float ISpline<Vector3, Vector3>.maxT { get { return maxT; } }

    Vector3 ISpline<Vector3, Vector3>.ValueAt(float t) {
      return PoseAt(t).position;
    }

    Vector3 ISpline<Vector3, Vector3>.DerivativeAt(float t) {
      return MovementAt(t).velocity;
    }

    void ISpline<Vector3, Vector3>.ValueAndDerivativeAt(float t,
                                                        out Vector3 value,
                                                        out Vector3 deltaValuePerT) {
      Pose pose;
      Movement movement;
      PoseAndMovementAt(t, out pose, out movement);

      value = pose.position;
      deltaValuePerT = movement.velocity;
    }

    #endregion

  }

  public static class PoseSplineSequenceExtensions {
    public static void DrawPoseSplineSequence(this RuntimeGizmoDrawer drawer,
                                              PoseSplineSequence poseSplines,
                                              bool drawPoses = true,
                                              bool drawSegments = true) {
      for (int i = 0; i < poseSplines.Count; i++) {
        drawer.DrawPoseSpline(poseSplines[i],
                              drawPoses: drawPoses,
                              drawSegments: drawSegments);
      }
    }
  }

}
