/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using System.Collections.Generic;

namespace Leap.Unity.Interaction {

  public class KabschSolver {
    Vector3[] QuatBasis = new Vector3[3];
    Vector3[] DataCovariance = new Vector3[3];
    Quaternion OptimalRotation = Quaternion.identity;
    public Matrix4x4 SolveKabsch(List<Vector3> inPoints, List<Vector3> refPoints) {
      if (inPoints.Count != refPoints.Count) { return Matrix4x4.identity; }

      //Calculate the centroid offset and construct the centroid-shifted point matrices
      Vector3 inCentroid = Vector3.zero; Vector3 refCentroid = Vector3.zero;
      for (int i = 0; i < inPoints.Count; i++) {
        inCentroid += inPoints[i];
        refCentroid += refPoints[i];
      }
      inCentroid /= inPoints.Count;
      refCentroid /= refPoints.Count;
      for (int i = 0; i < inPoints.Count; i++) {
        inPoints[i] -= inCentroid;
        refPoints[i] -= refCentroid;
      }

      //Calculate the covariance matrix, is a 3x3 Matrix and Calculate the optimal rotation
      extractRotation(TransposeMult(inPoints, refPoints, DataCovariance), ref OptimalRotation);

      return
      Matrix4x4.TRS(refCentroid, Quaternion.identity, Vector3.one) *
      Matrix4x4.TRS(Vector3.zero, OptimalRotation, Vector3.one) *
      Matrix4x4.TRS(-inCentroid, Quaternion.identity, Vector3.one);
    }

    //https://animation.rwth-aachen.de/media/papers/2016-MIG-StableRotation.pdf
    void extractRotation(Vector3[] A, ref Quaternion q) {
      for (int iter = 0; iter < 9; iter++) {
        Vector3[] R = MatrixFromQuaternion(q, QuatBasis);
        Vector3 omega = (Vector3.Cross(R[0], A[0]) +
                         Vector3.Cross(R[1], A[1]) +
                         Vector3.Cross(R[2], A[2])) *
         (1f / Mathf.Abs(Vector3.Dot(R[0], A[0]) +
                         Vector3.Dot(R[1], A[1]) +
                         Vector3.Dot(R[2], A[2]) + 0.000000001f));

        float w = Mathf.Clamp(omega.magnitude * 50f, -10f, 10f); //How aggressive each iteration should be
        if (w < 0.000000001f)
          break;
        q = Quaternion.AngleAxis(w, (1f / w) * omega) * q;
        q = Quaternion.Lerp(q, q, 0f);//Normalizes the Quaternion; critical for error suppression
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
