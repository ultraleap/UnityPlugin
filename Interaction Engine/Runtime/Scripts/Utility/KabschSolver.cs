/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;
using System.Collections.Generic;

namespace Leap.Unity.Interaction {

  public class KabschSolver {
    Vector3[] QuatBasis = new Vector3[3];
    Vector3[] DataCovariance = new Vector3[3];
    public Vector3 translation = Vector3.zero;
    public Quaternion OptimalRotation = Quaternion.identity;
    public float scaleRatio = 1f;
    public Matrix4x4 SolveKabsch(List<Vector3> inPoints, List<Vector3> refPoints, 
      int optimalRotationIterations = 9, bool solveScale = false) {
      if (inPoints.Count != refPoints.Count) { return Matrix4x4.identity; }
      List<Vector3> inPts = new List<Vector3>(inPoints), refPts = new List<Vector3>(refPoints);

      //Calculate the centroid offset and construct the centroid-shifted point matrices
      Vector3 inCentroid = Vector3.zero; Vector3 refCentroid = Vector3.zero;
      for (int i = 0; i < inPts.Count; i++) {
        inCentroid += inPts[i];
        refCentroid += refPts[i];
      }
      inCentroid /= inPts.Count;
      refCentroid /= refPts.Count;
      for (int i = 0; i < inPts.Count; i++) {
        inPts[i] -= inCentroid;
        refPts[i] -= refCentroid;
      }
      translation = refCentroid - inCentroid;

      //Calculate the scale ratio
      if (solveScale && inPoints.Count > 1) {
        float inScale = 0f, refScale = 0f;
        for (int i = 0; i < inPoints.Count; i++) {
          inScale += (new Vector3(inPoints[i].x, inPoints[i].y, inPoints[i].z) - inCentroid).magnitude;
          refScale += (new Vector3(refPoints[i].x, refPoints[i].y, refPoints[i].z) - refCentroid).magnitude;
        }
        scaleRatio = (refScale / inScale);
      } else { scaleRatio = 1f; }

      if (inPoints.Count != 1) {
        //Calculate the covariance matrix, is a 3x3 Matrix and Calculate the optimal rotation
        extractRotation(TransposeMult(inPts, refPts, DataCovariance), ref OptimalRotation, optimalRotationIterations);
      } else {
        OptimalRotation = Quaternion.identity;
      }

      return
      Matrix4x4.TRS(refCentroid, Quaternion.identity, Vector3.one * scaleRatio) *
      Matrix4x4.TRS(Vector3.zero, OptimalRotation, Vector3.one) *
      Matrix4x4.TRS(-inCentroid, Quaternion.identity, Vector3.one);
    }

    //https://animation.rwth-aachen.de/media/papers/2016-MIG-StableRotation.pdf
    void extractRotation(Vector3[] A, ref Quaternion q, int optimalRotationIterations = 9) {
      for (int iter = 0; iter < optimalRotationIterations; iter++) {
        q.FillMatrixFromQuaternion(ref QuatBasis);
        Vector3 omega = (Vector3.Cross(QuatBasis[0], A[0]) +
                         Vector3.Cross(QuatBasis[1], A[1]) +
                         Vector3.Cross(QuatBasis[2], A[2])) *
         (1f / Mathf.Abs(Vector3.Dot(QuatBasis[0], A[0]) +
                         Vector3.Dot(QuatBasis[1], A[1]) +
                         Vector3.Dot(QuatBasis[2], A[2]) + 0.000000001f));

        float w = omega.magnitude;
        if (w < 0.000000001f)
          break;
        q = Quaternion.AngleAxis(w * Mathf.Rad2Deg, omega / w) * q;
        q = Quaternion.Lerp(q, q, 0f); //Normalizes the Quaternion; critical for error suppression
      }
    }

      //Calculate Covariance Matrices --------------------------------------------------
      Vector3[] TransposeMult(List<Vector3> vec1, List<Vector3> vec2, Vector3[] covariance) {
      for (int i = 0; i < 3; i++) { //i is the row in this matrix
        covariance[i] = Vector3.zero;
        for (int j = 0; j < 3; j++) {//j is the column in the other matrix
          for (int k = 0; k < vec1.Count; k++) {//k is the column in this matrix
            covariance[i][j] += vec1[k][i] * vec2[k][j];
          }
        }
      }
      return covariance;
    }

    public static Vector3[] MatrixFromQuaternion(Quaternion q, Vector3[] covariance) {
      covariance[0] = q * Vector3.right;
      covariance[1] = q * Vector3.up;
      covariance[2] = q * Vector3.forward;
      return covariance;
    }
  }

  public static class FromMatrixExtension {
    public static Vector3 GetVector3(this Matrix4x4 m) { return m.GetColumn(3); }
    public static Quaternion GetQuaternion(this Matrix4x4 m) {
      if (m.GetColumn(2) == m.GetColumn(1)) { return Quaternion.identity; }
      return Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1));
    }
  }
}
