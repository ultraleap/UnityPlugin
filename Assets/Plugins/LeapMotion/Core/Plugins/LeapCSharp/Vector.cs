/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

namespace Leap {
  using System;

  /// <summary>
  /// Constants used in Leap Motion math functions.
  /// </summary>
  public static class Constants {
    public const float PI = 3.1415926536f;
    public const float DEG_TO_RAD = 0.0174532925f;
    public const float RAD_TO_DEG = 57.295779513f;
    public const float EPSILON = 1.192092896e-07f;
  }

  /// <summary>
  /// The Vector struct represents a three-component mathematical vector or point
  /// such as a direction or position in three-dimensional space.
  /// 
  /// The Leap Motion software employs a right-handed Cartesian coordinate system.
  /// Values given are in units of real-world millimeters. The origin is centered
  /// at the center of the Leap Motion Controller. The x- and z-axes lie in the horizontal
  /// plane, with the x-axis running parallel to the long edge of the device.
  /// The y-axis is vertical, with positive values increasing upwards (in contrast
  /// to the downward orientation of most computer graphics coordinate systems).
  /// The z-axis has positive values increasing away from the computer screen.
  /// @since 1.0
  /// </summary>
  [Serializable]
  public struct Vector : IEquatable<Vector> {

    public static Vector operator +(Vector v1, Vector v2) {
      return new Vector(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z);
    }

    public static Vector operator -(Vector v1, Vector v2) {
      return new Vector(v1.x - v2.x, v1.y - v2.y, v1.z - v2.z);
    }

    public static Vector operator *(Vector v1, float scalar) {
      return new Vector(v1.x * scalar, v1.y * scalar, v1.z * scalar);
    }

    public static Vector operator *(float scalar, Vector v1) {
      return new Vector(v1.x * scalar, v1.y * scalar, v1.z * scalar);
    }

    public static Vector operator /(Vector v1, float scalar) {
      return new Vector(v1.x / scalar, v1.y / scalar, v1.z / scalar);
    }

    public static Vector operator -(Vector v1) {
      return new Vector(-v1.x, -v1.y, -v1.z);
    }

    public static bool operator ==(Vector v1, Vector v2) {
      return v1.Equals(v2);
    }

    public static bool operator !=(Vector v1, Vector v2) {
      return !v1.Equals(v2);
    }

    public float[] ToFloatArray() {
      return new float[] { x, y, z };
    }

    /// <summary>
    /// Creates a new Vector with the specified component values.
    /// @since 1.0
    /// </summary>
    public Vector(float x, float y, float z) :
      this() {
      this.x = x;
      this.y = y;
      this.z = z;
    }

    /// <summary>
    /// Copies the specified Vector.
    /// @since 1.0
    /// </summary>
    public Vector(Vector vector) :
      this() {
      x = vector.x;
      y = vector.y;
      z = vector.z;
    }

    /// <summary>
    /// The distance between the point represented by this Vector
    /// object and a point represented by the specified Vector object.
    /// 
    /// @since 1.0
    /// </summary>
    public float DistanceTo(Vector other) {
      return (float)Math.Sqrt((x - other.x) * (x - other.x) +
          (y - other.y) * (y - other.y) +
          (z - other.z) * (z - other.z));

    }

    /// <summary>
    /// The angle between this vector and the specified vector in radians.
    /// 
    /// The angle is measured in the plane formed by the two vectors. The
    /// angle returned is always the smaller of the two conjugate angles.
    /// Thus A.angleTo(B) == B.angleTo(A) and is always a positive
    /// value less than or equal to pi radians (180 degrees).
    /// 
    /// If either vector has zero length, then this function returns zero.
    /// @since 1.0
    /// </summary>
    public float AngleTo(Vector other) {
      float denom = MagnitudeSquared * other.MagnitudeSquared;
      if (denom <= Constants.EPSILON) {
        return 0.0f;
      }
      float val = Dot(other) / (float)Math.Sqrt(denom);
      if (val >= 1.0f) {
        return 0.0f;
      } else if (val <= -1.0f) {
        return Constants.PI;
      }
      return (float)Math.Acos(val);
    }

    /// <summary>
    /// The dot product of this vector with another vector.
    /// 
    /// The dot product is the magnitude of the projection of this vector
    /// onto the specified vector.
    /// @since 1.0
    /// </summary>
    public float Dot(Vector other) {
      return (x * other.x) + (y * other.y) + (z * other.z);
    }

    /// <summary>
    /// The cross product of this vector and the specified vector.
    /// 
    /// The cross product is a vector orthogonal to both original vectors.
    /// It has a magnitude equal to the area of a parallelogram having the
    /// two vectors as sides. The direction of the returned vector is
    /// determined by the right-hand rule. Thus A.cross(B) == -B.cross(A).
    /// 
    /// @since 1.0
    /// </summary>
    public Vector Cross(Vector other) {
      return new Vector((y * other.z) - (z * other.y),
                    (z * other.x) - (x * other.z),
                    (x * other.y) - (y * other.x));
    }

    /// <summary>
    /// Returns a string containing this vector in a human readable format: (x, y, z).
    /// @since 1.0
    /// </summary>
    public override string ToString() {
      return "(" + x + ", " + y + ", " + z + ")";
    }

    /// <summary>
    /// Compare Vector equality component-wise.
    /// @since 1.0
    /// </summary>
    public bool Equals(Vector v) {
      return x.NearlyEquals(v.x) && y.NearlyEquals(v.y) && z.NearlyEquals(v.z);
    }

    public override bool Equals(Object obj) {
      return obj is Vector && Equals((Vector)obj);
    }

    /// <summary>
    /// Returns true if all of the vector's components are finite.  If any
    /// component is NaN or infinite, then this returns false.
    /// @since 1.0
    /// </summary>
    public bool IsValid() {
      return !(float.IsNaN(x) || float.IsInfinity(x) ||
               float.IsNaN(y) || float.IsInfinity(y) ||
               float.IsNaN(z) || float.IsInfinity(z));
    }

    /// <summary>
    /// Index vector components numerically.
    /// Index 0 is x, index 1 is y, and index 2 is z.
    /// @since 1.0
    /// </summary>
    public float this[uint index] {
      get {
        if (index == 0)
          return x;
        if (index == 1)
          return y;
        if (index == 2)
          return z;
        throw new IndexOutOfRangeException();
      }
      set {
        if (index == 0)
          x = value;
        if (index == 1)
          y = value;
        if (index == 2)
          z = value;
        throw new IndexOutOfRangeException();
      }
    }

    public float x;
    public float y;
    public float z;

    /// <summary>
    /// The magnitude, or length, of this vector.
    /// 
    /// The magnitude is the L2 norm, or Euclidean distance between the origin and
    /// the point represented by the (x, y, z) components of this Vector object.
    /// @since 1.0
    /// </summary>
    public float Magnitude {
      get { return (float)Math.Sqrt(x * x + y * y + z * z); }
    }

    /// <summary>
    /// The square of the magnitude, or length, of this vector.
    /// @since 1.0
    /// </summary>
    public float MagnitudeSquared {
      get { return x * x + y * y + z * z; }
    }

    /// <summary>
    /// The pitch angle in radians.
    /// 
    /// Pitch is the angle between the negative z-axis and the projection of
    /// the vector onto the y-z plane. In other words, pitch represents rotation
    /// around the x-axis.
    /// If the vector points upward, the returned angle is between 0 and pi radians
    /// (180 degrees); if it points downward, the angle is between 0 and -pi radians.
    /// 
    /// @since 1.0
    /// </summary>
    public float Pitch {
      get { return (float)Math.Atan2(y, -z); }
    }

    /// <summary>
    /// The roll angle in radians.
    /// 
    /// Roll is the angle between the y-axis and the projection of
    /// the vector onto the x-y plane. In other words, roll represents rotation
    /// around the z-axis. If the vector points to the left of the y-axis,
    /// then the returned angle is between 0 and pi radians (180 degrees);
    /// if it points to the right, the angle is between 0 and -pi radians.
    /// 
    /// Use this function to get roll angle of the plane to which this vector is a
    /// normal. For example, if this vector represents the normal to the palm,
    /// then this function returns the tilt or roll of the palm plane compared
    /// to the horizontal (x-z) plane.
    /// 
    /// @since 1.0
    /// </summary>
    public float Roll {
      get { return (float)Math.Atan2(x, -y); }
    }

    /// <summary>
    /// The yaw angle in radians.
    /// 
    /// Yaw is the angle between the negative z-axis and the projection of
    /// the vector onto the x-z plane. In other words, yaw represents rotation
    /// around the y-axis. If the vector points to the right of the negative z-axis,
    /// then the returned angle is between 0 and pi radians (180 degrees);
    /// if it points to the left, the angle is between 0 and -pi radians.
    /// 
    /// @since 1.0
    /// </summary>
    public float Yaw {
      get { return (float)Math.Atan2(x, -z); }
    }

    /// <summary>
    /// A normalized copy of this vector.
    /// 
    /// A normalized vector has the same direction as the original vector,
    /// but with a length of one.
    /// 
    /// @since 1.0
    /// </summary>
    public Vector Normalized {
      get {
        float denom = MagnitudeSquared;
        if (denom <= Constants.EPSILON) {
          return Zero;
        }
        denom = 1.0f / (float)Math.Sqrt(denom);
        return new Vector(x * denom, y * denom, z * denom);
      }
    }

    /// <summary>
    /// The zero vector: (0, 0, 0)
    /// </summary>
    public static readonly Vector Zero = new Vector(0, 0, 0);

    /// <summary>
    /// The ones vector: (1, 1, 1)
    /// </summary>
    public static readonly Vector Ones = new Vector(1, 1, 1);

    /// <summary>
    /// The x-axis unit vector: (1, 0, 0)
    /// </summary>
    public static readonly Vector XAxis = new Vector(1, 0, 0);

    /// <summary>
    /// The y-axis unit vector: (0, 1, 0)
    /// </summary>
    public static readonly Vector YAxis = new Vector(0, 1, 0);

    /// <summary>
    /// The z-axis unit vector: (0, 0, 1)
    /// </summary>
    public static readonly Vector ZAxis = new Vector(0, 0, 1);

    /// <summary>
    /// The unit vector pointing forward along the negative z-axis: (0, 0, -1)
    /// </summary>
    public static readonly Vector Forward = new Vector(0, 0, -1);

    /// <summary>
    /// The unit vector pointing backward along the positive z-axis: (0, 0, 1)
    /// </summary>
    public static readonly Vector Backward = new Vector(0, 0, 1);

    /// <summary>
    /// The unit vector pointing left along the negative x-axis: (-1, 0, 0)
    /// </summary>
    public static readonly Vector Left = new Vector(-1, 0, 0);

    /// <summary>
    /// The unit vector pointing right along the positive x-axis: (1, 0, 0)
    /// </summary>
    public static readonly Vector Right = new Vector(1, 0, 0);

    /// <summary>
    /// The unit vector pointing up along the positive y-axis: (0, 1, 0)
    /// </summary>
    public static readonly Vector Up = new Vector(0, 1, 0);

    /// <summary>
    /// The unit vector pointing down along the negative y-axis: (0, -1, 0)
    /// </summary>
    public static readonly Vector Down = new Vector(0, -1, 0);


    public static Vector Lerp(Vector a, Vector b, float t) {
      return new Vector(
              a.x + t * (b.x - a.x),
              a.y + t * (b.y - a.y),
              a.z + t * (b.z - a.z)
          );
    }

    public override int GetHashCode() {
      unchecked // Overflow is fine, just wrap
      {
        int hash = 17;
        hash = hash * 23 + x.GetHashCode();
        hash = hash * 23 + y.GetHashCode();
        hash = hash * 23 + z.GetHashCode();

        return hash;
      }
    }
  }
}
