/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using NUnit.Framework;
using System;

namespace Leap.LeapCSharp.Tests {
  [TestFixture()]
  public class VectorTests {
    Vector thisVector = Vector.Up;
    Vector thatVector = Vector.Forward;
    //Vector otherVector = Vector.Left;

    [OneTimeSetUp]
    public void Init() { }

    [Test()]
    public void Vector_Up() {
      Vector vec = Vector.Up;
      Assert.AreEqual(0, vec.x, "x");
      Assert.AreEqual(1, vec.y, "y");
      Assert.AreEqual(0, vec.z, "z");
    }

    [Test()]
    public void Vector_Down() {
      Vector vec = Vector.Down;
      Assert.AreEqual(0, vec.x, "x");
      Assert.AreEqual(-1, vec.y, "y");
      Assert.AreEqual(0, vec.z, "z");
    }

    [Test()]
    public void Vector_Forward() {
      Vector vec = Vector.Forward;
      Assert.AreEqual(0, vec.x, "x");
      Assert.AreEqual(0, vec.y, "y");
      Assert.AreEqual(-1, vec.z, "z");
    }

    [Test()]
    public void Vector_Backward() {
      Vector vec = Vector.Backward;
      Assert.AreEqual(0, vec.x, "x");
      Assert.AreEqual(0, vec.y, "y");
      Assert.AreEqual(1, vec.z, "z");
    }

    [Test()]
    public void Vector_Left() {
      Vector vec = Vector.Left;
      Assert.AreEqual(-1, vec.x, "x");
      Assert.AreEqual(0, vec.y, "y");
      Assert.AreEqual(0, vec.z, "z");
    }

    [Test()]
    public void Vector_Right() {
      Vector vec = Vector.Right;
      Assert.AreEqual(1, vec.x, "x");
      Assert.AreEqual(0, vec.y, "y");
      Assert.AreEqual(0, vec.z, "z");
    }

    [Test()]
    public void Vector_Zero() {
      Vector vec = Vector.Zero;
      Assert.AreEqual(0, vec.x, "x");
      Assert.AreEqual(0, vec.y, "y");
      Assert.AreEqual(0, vec.z, "z");
    }

    [Test()]
    public void Vector_XAxis() {
      Vector vec = Vector.XAxis;
      Assert.AreEqual(1, vec.x, "x");
      Assert.AreEqual(0, vec.y, "y");
      Assert.AreEqual(0, vec.z, "z");
    }

    [Test()]
    public void Vector_YAxis() {
      Vector vec = Vector.YAxis;
      Assert.AreEqual(0, vec.x, "x");
      Assert.AreEqual(1, vec.y, "y");
      Assert.AreEqual(0, vec.z, "z");
    }

    [Test()]
    public void Vector_ZAxis() {
      Vector vec = Vector.ZAxis;
      Assert.AreEqual(0, vec.x, "x");
      Assert.AreEqual(0, vec.y, "y");
      Assert.AreEqual(1, vec.z, "z");
    }

    [Test()]
    public void Vector_Constructor_1() {
      Vector vec = new Vector(0.5f, 200.3f, 67f);
      Assert.AreEqual(0.5f, vec.x, "x");
      Assert.AreEqual(200.3f, vec.y, "y");
      Assert.AreEqual(67f, vec.z, "z");
      vec = new Vector();
      Assert.AreEqual(0, vec.x, "x");
      Assert.AreEqual(0, vec.y, "y");
      Assert.AreEqual(0, vec.z, "z");
    }

    [Test()]
    public void Vector_Constructor_2() {
      Vector baseVector = new Vector(3, 4, 5);
      Vector vec = new Vector(baseVector);
      Assert.AreEqual(3, vec.x, "x");
      Assert.AreEqual(4, vec.y, "y");
      Assert.AreEqual(5, vec.z, "z");
      vec.x = 12;
      Assert.AreEqual(3, baseVector.x, "z");
    }

    [Test()]
    public void Vector_Magnitude() {
      Assert.AreEqual(0, Vector.Zero.Magnitude, "Zero has 0 length");
      Assert.AreEqual(1, Vector.Up.Magnitude, "Up has 1 length");
      Assert.AreEqual(1, Vector.Down.Magnitude, "Down has 1 length");
      Assert.AreEqual(1, Vector.Left.Magnitude, "Left has 1 length");
      Assert.AreEqual(1, Vector.Right.Magnitude, "Right has 1 length");
      Assert.AreEqual(1, Vector.Forward.Magnitude, "Forward has 1 length");
      Assert.AreEqual(1, Vector.Backward.Magnitude, "Backward has 1 length");

      float tooBig = new Vector(float.MaxValue, float.MaxValue, float.MaxValue).Magnitude;
      Assert.IsTrue(float.IsInfinity(tooBig), "max value is too large");
      float tooSmall = new Vector(float.MinValue, float.MinValue, float.MinValue).Magnitude;
      Assert.IsTrue(float.IsInfinity(tooSmall), "min value is too large");
      Assert.AreEqual((float)Math.Sqrt(3f), new Vector(1, 1, 1).Magnitude, "(1,1,1) has sqrt(3) length");
      Assert.AreEqual((float)Math.Sqrt(3f), new Vector(-1, -1, -1).Magnitude, "(-1,-1,-1) has sqrt(3) length");
    }

    [Test()]
    public void Vector_Magnitude_Squared() {
      Assert.AreEqual(0, Vector.Zero.MagnitudeSquared, "Zero has 0 length");
      Assert.AreEqual(1, Vector.Up.MagnitudeSquared, "Up has 1 length");
      Assert.AreEqual(1, Vector.Down.MagnitudeSquared, "Down has 1 length");
      Assert.AreEqual(1, Vector.Left.MagnitudeSquared, "Left has 1 length");
      Assert.AreEqual(1, Vector.Right.MagnitudeSquared, "Right has 1 length");
      Assert.AreEqual(1, Vector.Forward.MagnitudeSquared, "Forward has 1 length");
      Assert.AreEqual(1, Vector.Backward.MagnitudeSquared, "Backward has 1 length");

      float tooBig = new Vector(float.MaxValue, float.MaxValue, float.MaxValue).MagnitudeSquared;
      Assert.IsTrue(float.IsInfinity(tooBig), "max value is too large");
      float tooSmall = new Vector(float.MinValue, float.MinValue, float.MinValue).MagnitudeSquared;
      Assert.IsTrue(float.IsInfinity(tooSmall), "min value is too large");
      Assert.AreEqual(3, new Vector(1, 1, 1).MagnitudeSquared, "(1,1,1) has 3 length");
      Assert.AreEqual(3, new Vector(-1, -1, -1).MagnitudeSquared, "(-1,-1,-1) 3 length");
    }

    [Test()]
    public void Vector_DistanceTo() {
      Vector origin = Vector.Zero;
      Assert.AreEqual(0, origin.DistanceTo(Vector.Zero), "distance to 0 is 0");
      Assert.AreEqual(1, origin.DistanceTo(Vector.Up), "distance to Up is 1");
      Assert.AreEqual(1, origin.DistanceTo(Vector.Down), "distance to Down is 1");
      Assert.AreEqual(1, origin.DistanceTo(Vector.Left), "distance to Left is 1");
      Assert.AreEqual(1, origin.DistanceTo(Vector.Right), "distance to Right is 1");
      Assert.AreEqual(1, origin.DistanceTo(Vector.Forward), "distance to Forward is 1");
      Assert.AreEqual(1, origin.DistanceTo(Vector.Backward), "distance to Backward is 1");

      float tooBig = origin.DistanceTo(new Vector(float.MaxValue, float.MaxValue, float.MaxValue));
      Assert.IsTrue(float.IsInfinity(tooBig), "max value is too large");
      float tooSmall = origin.DistanceTo(new Vector(float.MinValue, float.MinValue, float.MinValue));
      Assert.IsTrue(float.IsInfinity(tooSmall), "min value is too large");
      Assert.AreEqual((float)Math.Sqrt(3f), origin.DistanceTo(new Vector(1, 1, 1)), "distance to (1,1,1) is sqrt(3)");
      Assert.AreEqual((float)Math.Sqrt(3f), origin.DistanceTo(new Vector(-1, -1, -1)), "distance to (-1,-1,-1) is sqrt(3)");
    }

    [Test()]
    public void Vector_AngleTo() {
      //The angle returned is always the smaller of the two conjugate angles. Thus A.angleTo(B) == B.angleTo(A) and is always a positive value less than or equal to pi radians (180 degrees).
      Assert.AreEqual(0, new Vector(1, -3, 45).AngleTo(new Vector(1, -3, 45)), "angle to same");

      Assert.AreEqual(90 * Constants.DEG_TO_RAD, Vector.Up.AngleTo(Vector.Left), "Up-Left");
      Assert.AreEqual(90 * Constants.DEG_TO_RAD, Vector.Up.AngleTo(Vector.Right), "Up-Right");
      Assert.AreEqual(90 * Constants.DEG_TO_RAD, Vector.Up.AngleTo(Vector.Forward), "Up-Forward");
      Assert.AreEqual(90 * Constants.DEG_TO_RAD, Vector.Up.AngleTo(Vector.Backward), "Up-Backward");
      Assert.AreEqual(90 * Constants.DEG_TO_RAD, Vector.Down.AngleTo(Vector.Left), "Down-Left");
      Assert.AreEqual(90 * Constants.DEG_TO_RAD, Vector.Down.AngleTo(Vector.Right), "Down-Right");
      Assert.AreEqual(90 * Constants.DEG_TO_RAD, Vector.Down.AngleTo(Vector.Forward), "Down-Forward");
      Assert.AreEqual(90 * Constants.DEG_TO_RAD, Vector.Down.AngleTo(Vector.Backward), "Down-Backward");
      Matrix rotator = Matrix.Identity;
      Vector baseVec = Vector.Left;
      Vector vec = new Vector(baseVec);
      int count = 0;
      for (; count <= 180; count++) {
        rotator.SetRotation(Vector.Up, count * Constants.DEG_TO_RAD);
        Vector rotated = rotator.TransformDirection(vec);
        Assert.AreEqual(count * Constants.DEG_TO_RAD, baseVec.AngleTo(rotated), 12 * Constants.EPSILON, "0-180 Angle is " + baseVec.AngleTo(rotated) * Constants.RAD_TO_DEG);
        Assert.AreEqual(rotated.AngleTo(baseVec), baseVec.AngleTo(rotated), Constants.EPSILON, "a to b == b to a");

      }
      for (; count <= 360; count++) {
        rotator.SetRotation(Vector.Up, count * Constants.DEG_TO_RAD);
        Vector rotated = rotator.TransformDirection(vec);

        Assert.AreEqual((360 - count) * Constants.DEG_TO_RAD, baseVec.AngleTo(rotated), 12 * Constants.EPSILON, "180-360 Angle is " + baseVec.AngleTo(rotated) * Constants.RAD_TO_DEG);
        Assert.AreEqual(rotated.AngleTo(baseVec), baseVec.AngleTo(rotated), Constants.EPSILON, "a to b == b to a");
      }
      for (; count <= 540; count++) {
        rotator.SetRotation(Vector.Up, count * Constants.DEG_TO_RAD);
        Vector rotated = rotator.TransformDirection(vec);
        Assert.AreEqual((count - 360) * Constants.DEG_TO_RAD, baseVec.AngleTo(rotated), 12 * Constants.EPSILON, "360-540 Angle is " + baseVec.AngleTo(rotated) * Constants.RAD_TO_DEG);
        Assert.AreEqual(rotated.AngleTo(baseVec), baseVec.AngleTo(rotated), Constants.EPSILON, "a to b == b to a");

      }
      for (; count <= 720; count++) {
        rotator.SetRotation(Vector.Up, count * Constants.DEG_TO_RAD);
        Vector rotated = rotator.TransformDirection(vec);

        Assert.AreEqual((720 - count) * Constants.DEG_TO_RAD, baseVec.AngleTo(rotated), 12 * Constants.EPSILON, "540-720 Angle is " + baseVec.AngleTo(rotated) * Constants.RAD_TO_DEG);
        Assert.AreEqual(rotated.AngleTo(baseVec), baseVec.AngleTo(rotated), Constants.EPSILON, "a to b == b to a");
      }
    }

    [Test()]
    public void Vector_Pitch() {
      //If the vector points upward, the returned angle is between 0 and pi radians (180 degrees); if it points downward, the angle is between 0 and -pi radians
      Matrix rotator = Matrix.Identity;
      Vector baseVec = Vector.Forward;
      Vector vec = new Vector(baseVec);
      Vector axis = -Vector.XAxis;
      int count = 0;
      for (; count < 180; count++) {
        rotator.SetRotation(axis, count * Constants.DEG_TO_RAD);
        Vector rotated = rotator.TransformDirection(vec);
        Assert.AreEqual(count * Constants.DEG_TO_RAD, rotated.Pitch, 12 * Constants.EPSILON, "0-180 Pitch is " + rotated.Pitch * Constants.RAD_TO_DEG);
      }
      for (; count <= 360; count++) {
        rotator.SetRotation(axis, count * Constants.DEG_TO_RAD);
        Vector rotated = rotator.TransformDirection(vec);
        Assert.AreEqual((-360 + count) * Constants.DEG_TO_RAD, rotated.Pitch, 12 * Constants.EPSILON, "180-360 Pitch is " + rotated.Pitch * Constants.RAD_TO_DEG);
      }
    }

    [Test()]
    public void Vector_Yaw() {
      //If the vector points to the right of the negative z-axis, then the returned angle is between 0 and pi radians (180 degrees); if it points to the left, the angle is between 0 and -pi radians.
      Matrix rotator = Matrix.Identity;
      Vector baseVec = Vector.Forward;
      Vector vec = new Vector(baseVec);
      Vector axis = Vector.YAxis;
      int count = 0;
      for (; count < 180; count++) {
        rotator.SetRotation(axis, count * Constants.DEG_TO_RAD);
        Vector rotated = rotator.TransformDirection(vec);
        Assert.AreEqual(count * Constants.DEG_TO_RAD, rotated.Yaw, 12 * Constants.EPSILON, "0-180 Yaw is " + rotated.Yaw * Constants.RAD_TO_DEG);
      }
      for (; count <= 360; count++) {
        rotator.SetRotation(axis, count * Constants.DEG_TO_RAD);
        Vector rotated = rotator.TransformDirection(vec);
        Assert.AreEqual((-360 + count) * Constants.DEG_TO_RAD, rotated.Yaw, 12 * Constants.EPSILON, "180-360 Yaw is " + rotated.Yaw * Constants.RAD_TO_DEG);
      }
    }

    [Test()]
    public void Vector_Roll() {
      // If the vector points to the left of the y-axis, then the returned angle is between 0 and pi radians (180 degrees); if it points to the right, the angle is between 0 and -pi radians.
      Matrix rotator = Matrix.Identity;
      Vector baseVec = Vector.Down;
      Vector vec = new Vector(baseVec);
      Vector axis = -Vector.ZAxis;
      int count = 0;
      for (; count < 180; count++) {
        rotator.SetRotation(axis, count * Constants.DEG_TO_RAD);
        Vector rotated = rotator.TransformDirection(vec);
        Assert.AreEqual(count * Constants.DEG_TO_RAD, rotated.Roll, 12 * Constants.EPSILON, "0-180 Roll is " + rotated.Roll * Constants.RAD_TO_DEG);
      }
      for (; count <= 360; count++) {
        rotator.SetRotation(axis, count * Constants.DEG_TO_RAD);
        Vector rotated = rotator.TransformDirection(vec);
        Assert.AreEqual((-360 + count) * Constants.DEG_TO_RAD, rotated.Roll, 12 * Constants.EPSILON, "180-360 Roll is " + rotated.Roll * Constants.RAD_TO_DEG);
      }
    }

    [Test()]
    public void Vector_Dot() {
      Assert.AreEqual(0, Vector.Up.Dot(Vector.Forward), "Orthogonal");
      Assert.AreEqual(1, Vector.Up.Dot(Vector.Up), "Parallel");
      Assert.AreEqual(-1, Vector.Up.Dot(Vector.Down), "Opposite");
      Assert.AreEqual(1, Vector.Backward.Dot(new Vector(0, 1, 1)), "Hypoteneuse of right isoscelese");
      Assert.AreEqual((float)Math.Sqrt(2) / 2, Vector.Backward.Dot(new Vector(0, 1, 1).Normalized), "45 degree unit vectors");
    }

    [Test()]
    public void Vector_Cross() {
      Vector v1 = new Vector(1, 2, 3);
      Vector v2 = new Vector(3, 2, 1);
      Vector expected = new Vector(-4, 8, -4);
      Assert.AreEqual(expected, v1.Cross(v2), "v1 x v2");
      Assert.AreEqual(-expected, v2.Cross(v1), "v1 x v2");
      Assert.AreEqual(0, v1.Dot(v1.Cross(v2)), "Orthogonal with v1");
      Assert.AreEqual(0, v2.Dot(v1.Cross(v2)), "Orthogonal with v2");

    }

    [Test()]
    public void Vector_Normalized() {
      Vector v1 = new Vector(1, 2, 3);
      Vector v2 = new Vector(0, 0, 0);
      Vector v3 = new Vector(-4, 8, -4);
      Vector v4 = new Vector(99999999, 99999999, 99999999);
      Vector v5 = new Vector(-99999999, -99999999, -99999999);
      Assert.AreEqual(1.0f, v1.Normalized.Magnitude, Constants.EPSILON, "small");
      Assert.AreEqual(0.0f, v2.Normalized.Magnitude, Constants.EPSILON, "zero");
      Assert.AreEqual(1.0f, v3.Normalized.Magnitude, Constants.EPSILON, "small negative");
      Assert.AreEqual(1.0f, v4.Normalized.Magnitude, Constants.EPSILON, "large");
      Assert.AreEqual(1.0f, v5.Normalized.Magnitude, Constants.EPSILON, "large negative");
    }

    [Test()]
    public void Vector_Plus() {
      Vector v1 = new Vector(1, 2, 3);
      Vector v2 = new Vector(-4, 8, -4);
      Assert.AreEqual(new Vector(-3, 10, -1), v1 + v2);
    }

    [Test()]
    public void Vector_Minus() {
      Vector v1 = new Vector(1, 2, 3);
      Vector v2 = new Vector(-4, 8, -4);
      Assert.AreEqual(new Vector(5, -6, 7), v1 - v2);
    }

    [Test()]
    public void Vector_Negate() {
      Vector v1 = new Vector(1, 2, -3);
      Assert.AreEqual(new Vector(-1, -2, 3), -v1);
    }

    [Test()]
    public void Vector_Times() {
      Vector v1 = new Vector(1, 2, -3);
      Assert.AreEqual(new Vector(5.2f, 10.4f, -15.6f), (v1 * 5.2f));
    }

    [Test()]
    public void Vector_Divide() {
      Vector v1 = new Vector(25, 150, -300);
      Assert.AreEqual(new Vector(5f, 30f, -60f), (v1 / 5.0f));
    }

    [Test()]
    public void Vector_Equals() {
      Vector v1 = new Vector(1, 2, 3);
      Vector v2 = new Vector(0, 0, 0);
      Vector v3 = new Vector(1, 2, 3);
      Assert.IsTrue(v1.Equals(v3), "simple integers 1");
      Assert.IsTrue(v3.Equals(v1), "simple integers 2");
      Assert.IsTrue(v1.Equals(v1), "simple integers 3");
      Assert.IsTrue(v1 == v3, "simple integers 4");
      Assert.IsTrue(v3 == v1, "simple integers 5");
      Assert.IsFalse(v1.Equals(v2), "simple integers 6");
      Assert.IsFalse(v2.Equals(v1), "simple integers 7");
      Assert.IsFalse(v1 == v2, "simple integers 8");
      Assert.IsFalse(v2 == v1, "simple integers 9");

      Vector v4 = new Vector(float.MinValue, float.MinValue, float.MinValue);
      Vector v5 = new Vector(float.MinValue, float.MinValue, float.MinValue);
      Assert.IsTrue(v4 == v5, "MinValue");

      Vector v6 = new Vector(float.MaxValue, float.MaxValue, float.MaxValue);
      Vector v7 = new Vector(float.MaxValue, float.MaxValue, float.MaxValue);
      Assert.IsTrue(v6 == v7, "MaxValue");

      Vector v8 = new Vector(float.Epsilon, float.Epsilon, float.Epsilon);
      Vector v9 = new Vector(float.Epsilon, float.Epsilon, float.Epsilon);
      Assert.IsTrue(v8 == v9, "Epsilon");

      Vector v10 = new Vector(float.PositiveInfinity, float.PositiveInfinity, float.NegativeInfinity);
      Vector v11 = new Vector(float.PositiveInfinity, float.PositiveInfinity, float.NegativeInfinity);
      Assert.IsTrue(v10 == v11, "Infinity");

      Vector v12 = new Vector(float.NaN, float.NaN, float.NaN);
      Vector v13 = new Vector(float.NaN, float.NaN, float.NaN);
      Assert.IsFalse(v12 == v13, "NaN");

      Vector v14 = new Vector(5 + float.Epsilon, -124.34f + float.Epsilon, float.MaxValue - float.Epsilon);
      Vector v15 = new Vector(5 - float.Epsilon, -124.34f - float.Epsilon, float.MaxValue);
      Assert.IsTrue(v14 == v15, "+- Epsilon");

      Vector v16 = new Vector(5 + Constants.EPSILON, -124.34f + Constants.EPSILON, float.MaxValue - Constants.EPSILON);
      Vector v17 = new Vector(5 - Constants.EPSILON, -124.34f - Constants.EPSILON, float.MaxValue);
      Assert.IsTrue(v16 == v17, "+- Leap Epsilon");

      float epsilonMultiplier = 11; //TODO figure out why this error is so high
      Vector v18 = new Vector(5 + Constants.EPSILON * epsilonMultiplier, -124.34f + Constants.EPSILON * epsilonMultiplier, float.MaxValue - Constants.EPSILON * epsilonMultiplier);
      Vector v19 = new Vector(5, -124.34f, float.MaxValue);
      Assert.IsFalse(v18 == v19, "Diff > Leap Epsilon");

    }

    [Test()]
    public void Vector_NotEqual() {
      // !!!Vector_NotEqual
      bool vectorsNotEqual = thisVector != thatVector;
      // !!!END
      Assert.IsTrue(vectorsNotEqual);
    }

    [Test()]
    public void Vector_IsValid() {
      Vector xInvalid = new Vector(float.NaN, 3f, 45f);
      Assert.IsFalse(xInvalid.IsValid());
      Vector yInvalid = new Vector(32.1f, float.NaN, 45f);
      Assert.IsFalse(yInvalid.IsValid());
      Vector zInvalid = new Vector(-345.32f, -78.67f, float.NaN);
      Assert.IsFalse(zInvalid.IsValid());
      Vector xInfinite = new Vector(float.PositiveInfinity, 3f, 45f);
      Assert.IsFalse(xInfinite.IsValid());
      Vector yInfinite = new Vector(-23.7f, float.NegativeInfinity, 3f);
      Assert.IsFalse(yInfinite.IsValid());
      Vector zInfinite = new Vector(3f, 45f, float.PositiveInfinity);
      Assert.IsFalse(zInfinite.IsValid());
      Vector mixed = new Vector(float.NaN, float.NegativeInfinity, float.PositiveInfinity);
      Assert.IsFalse(mixed.IsValid());
    }
  }
}

