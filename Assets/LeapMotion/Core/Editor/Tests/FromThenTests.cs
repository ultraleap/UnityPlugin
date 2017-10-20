/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;

namespace Leap.Unity.Tests {

  /// <summary>
  /// Tests for From() and Then() extension methods.
  /// </summary>
  public class FromThenTests {

    public static float EPSILON = 0.0001f;

    #region Vector3

    private static Vector3 VEC_A = new Vector3(0.5f,  0.2f,  0.8f);
    private static Vector3 VEC_B = new Vector3(0.13f, 0.98f, 3000f);

    [Test]
    public void FromAVecToBVec() {
      Assert.That(AreVector3sEqual(VEC_A.Then(VEC_B.From(VEC_A)), VEC_B));
    }

    [Test]
    public void FromBVecToAVec() {
      Assert.That(AreVector3sEqual(VEC_B.Then(VEC_A.From(VEC_B)), VEC_A));
    }

    private static bool AreVector3sEqual(Vector3 a, Vector3 b) {
      return (a - b).magnitude < EPSILON;
    }

    #endregion

    #region Quaternion

    private static Quaternion QUAT_A {
      get { return Quaternion.AngleAxis(90f, Vector3.up); }
    }
    private static Quaternion QUAT_B {
      get { return Quaternion.AngleAxis(43f, Vector3.one.normalized); }
    }

    [Test]
    public void FromAQuatToBQuat() {
      Assert.That(AreQuaternionsEqual(QUAT_A.Then(QUAT_B.From(QUAT_A)), QUAT_B));
    }

    [Test]
    public void FromBQuatToAQuat() {
      Assert.That(AreQuaternionsEqual(QUAT_B.Then(QUAT_A.From(QUAT_B)), QUAT_A));
    }

    private static bool AreQuaternionsEqual(Quaternion a, Quaternion b) {
      return (a.ToAngleAxisVector() - b.ToAngleAxisVector()).magnitude < EPSILON;
    }

    #endregion

    #region Pose

    public static Pose POSE_A {
      get { return new Pose(VEC_A, QUAT_A); }
    }
    public static Pose POSE_B {
      get { return new Pose(VEC_B, QUAT_B); }
    }

    [Test]
    public void FromAPoseToBPose() {
      Pose aFromB = POSE_A.From(POSE_B);

      Pose recoverA = POSE_B.Then(aFromB);

      Assert.That(AreVector3sEqual(recoverA.position, VEC_A));
      Assert.That(AreQuaternionsEqual(recoverA.rotation, QUAT_A));
    }

    [Test]
    public void FromBPoseToAPose() {
      Pose bFromA = POSE_B.From(POSE_A);

      Pose recoverB = POSE_A.Then(bFromA);

      Assert.That(AreVector3sEqual(recoverB.position, VEC_B));
      Assert.That(AreQuaternionsEqual(recoverB.rotation, QUAT_B));
    }

    #endregion

    #region Matrix4x4

    private Matrix4x4 MAT_A {
      get {
        return Matrix4x4.TRS(Vector3.right * 100f,
                             Quaternion.AngleAxis(77f, Vector3.one),
                             Vector3.one * 35f);
      }
    }

    private Matrix4x4 MAT_B {
      get {
        return Matrix4x4.TRS(Vector3.one * 20f,
                             Quaternion.AngleAxis(24f, Vector3.up),
                             Vector3.one * 2f);
      }
    }

    [Test]
    public void FromMatrixBToMatrixA() {
      Assert.That(AreMatricesEqual(MAT_B.Then(MAT_A.From(MAT_B)), MAT_A));
    }

    [Test]
    public void FromMatrixAToMatrixB() {
      Assert.That(AreMatricesEqual(MAT_A.Then(MAT_B.From(MAT_A)), MAT_B));
    }

    private static bool AreMatricesEqual(Matrix4x4 a, Matrix4x4 b) {
      return AreVector3sEqual(a.GetVector3(), b.GetVector3())
          && AreQuaternionsEqual(a.GetQuaternion(), b.GetQuaternion());
    }

    #endregion

  }
}
