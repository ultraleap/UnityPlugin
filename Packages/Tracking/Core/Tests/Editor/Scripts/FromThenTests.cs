/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Leap.Unity.Tests
{

    /// <summary>
    /// Tests for From(), To(), and Then() extension methods.
    /// 
    /// These extension methods provide a consistent rightward syntax for mathematical
    /// transformations, e.g. Quaternions and matrices as well as more trivial types like
    /// float and Vector3.
    /// </summary>
    public class FromThenTests
    {

        public static float EPSILON = 0.0006f;

        #region Vector3

        private static Vector3 VEC_A = new Vector3(0.5f, 0.2f, 0.8f);
        private static Vector3 VEC_B = new Vector3(0.13f, 0.98f, 3000f);

        [Test]
        public void FromVecAToVecB()
        {
            Assert.That(AreVector3sEqual(VEC_A.Then(VEC_B.From(VEC_A)), VEC_B));
        }

        [Test]
        public void ToVecBFromVecA()
        {
            Assert.That(AreVector3sEqual(VEC_A.Then(VEC_A.To(VEC_B)), VEC_B));
        }

        [Test]
        public void FromVecBToVecA()
        {
            Assert.That(AreVector3sEqual(VEC_B.Then(VEC_A.From(VEC_B)), VEC_A));
        }

        [Test]
        public void ToVecAFromVecB()
        {
            Assert.That(AreVector3sEqual(VEC_B.Then(VEC_B.To(VEC_A)), VEC_A));
        }

        private static bool AreVector3sEqual(Vector3 a, Vector3 b)
        {
            return (a - b).magnitude < EPSILON;
        }

        #endregion

        #region Quaternion

        private static Quaternion QUAT_A
        {
            get { return Quaternion.AngleAxis(90f, Vector3.up); }
        }
        private static Quaternion QUAT_B
        {
            get { return Quaternion.AngleAxis(43f, Vector3.one.normalized); }
        }

        [Test]
        public void FromQuatAToQuatB()
        {
            Assert.That(AreQuaternionsEqual(QUAT_A.Then(QUAT_B.From(QUAT_A)), QUAT_B));
        }

        [Test]
        public void ToQuatAFromQuatB()
        {
            Assert.That(AreQuaternionsEqual(QUAT_A.Then(QUAT_A.To(QUAT_B)), QUAT_B));
        }

        [Test]
        public void FromQuatBToQuatA()
        {
            Assert.That(AreQuaternionsEqual(QUAT_B.Then(QUAT_A.From(QUAT_B)), QUAT_A));
        }

        [Test]
        public void ToQuatBFromQuatA()
        {
            Assert.That(AreQuaternionsEqual(QUAT_B.Then(QUAT_B.To(QUAT_A)), QUAT_A));
        }

        private static bool AreQuaternionsEqual(Quaternion a, Quaternion b)
        {
            return (a.ToAngleAxisVector() - b.ToAngleAxisVector()).magnitude < EPSILON;
        }

        #endregion

        #region Pose

        public static Pose POSE_A
        {
            get { return new Pose(VEC_A, QUAT_A); }
        }
        public static Pose POSE_B
        {
            get { return new Pose(VEC_B, QUAT_B); }
        }

        [Test]
        public void FromPoseAToPoseB()
        {
            Assert.That(ArePosesEqual(POSE_B.Then(POSE_A.From(POSE_B)), POSE_A));
        }

        [Test]
        public void ToPoseAFromPoseB()
        {
            Assert.That(ArePosesEqual(POSE_B.Then(POSE_B.To(POSE_A)), POSE_A));
        }

        [Test]
        public void FromPoseBToPoseA()
        {
            Assert.That(ArePosesEqual(POSE_A.Then(POSE_B.From(POSE_A)), POSE_B));
        }

        [Test]
        public void ToPoseBFromPoseA()
        {
            Assert.That(ArePosesEqual(POSE_A.Then(POSE_A.To(POSE_B)), POSE_B));
        }

        private bool ArePosesEqual(Pose a, Pose b)
        {
            return AreVector3sEqual(a.position, b.position)
                && AreQuaternionsEqual(a.rotation, b.rotation);
        }

        #endregion

        #region Matrix4x4

        private Matrix4x4 MAT_A
        {
            get
            {
                return Matrix4x4.TRS(Vector3.right * 100f,
                                     Quaternion.AngleAxis(77f, Vector3.one),
                                     Vector3.one * 35f);
            }
        }

        private Matrix4x4 MAT_B
        {
            get
            {
                return Matrix4x4.TRS(Vector3.one * 20f,
                                     Quaternion.AngleAxis(24f, Vector3.up),
                                     Vector3.one * 2f);
            }
        }

        [Test]
        public void FromMatrixBToMatrixA()
        {
            Assert.That(AreMatricesEqual(MAT_B.Then(MAT_A.From(MAT_B)), MAT_A));
        }

        [Test]
        public void ToMatrixAFromMatrixB()
        {
            Assert.That(AreMatricesEqual(MAT_B.Then(MAT_B.To(MAT_A)), MAT_A));
        }

        [Test]
        public void FromMatrixAToMatrixB()
        {
            Assert.That(AreMatricesEqual(MAT_A.Then(MAT_B.From(MAT_A)), MAT_B));
        }

        private static bool AreMatricesEqual(Matrix4x4 a, Matrix4x4 b)
        {
            return AreVector3sEqual(a.GetVector3(), b.GetVector3())
                && AreQuaternionsEqual(a.GetQuaternion(), b.GetQuaternion());
        }

        #endregion

    }
}