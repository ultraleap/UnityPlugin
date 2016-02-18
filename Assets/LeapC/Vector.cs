/******************************************************************************\
* Copyright (C) 2012-2016 Leap Motion, Inc. All rights reserved.               *
* Leap Motion proprietary and confidential. Not for distribution.              *
* Use subject to the terms of the Leap Motion SDK Agreement available at       *
* https://developer.leapmotion.com/sdk_agreement, or another agreement         *
* between Leap Motion and you, your company or other organization.             *
\******************************************************************************/
namespace Leap
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;

    /**
     * Constants used in Leap Motion math functions.
     */
    public static class Constants
    {
        /**
 * The constant pi as a single precision floating point number.
 * @since 1.0
 */
        public const float PI = 3.1415926536f;
        /**
 * The constant ratio to convert an angle measure from degrees to radians.
 * Multiply a value in degrees by this constant to convert to radians.
 * @since 1.0
 */
        public const float DEG_TO_RAD = 0.0174532925f;
        /**
 * The constant ratio to convert an angle measure from radians to degrees.
 * Multiply a value in radians by this constant to convert to degrees.
 * @since 1.0
 */
        public const float RAD_TO_DEG = 57.295779513f;
        
        /**
* The difference between 1 and the least value greater than 1 that is
* representable as a float.
* @since 2.0
*/
        public const float EPSILON = 1.192092896e-07f;
    }
/**
 * The Vector struct represents a three-component mathematical vector or point
 * such as a direction or position in three-dimensional space.
 *
 * The Leap Motion software employs a right-handed Cartesian coordinate system.
 * Values given are in units of real-world millimeters. The origin is centered
 * at the center of the Leap Motion Controller. The x- and z-axes lie in the horizontal
 * plane, with the x-axis running parallel to the long edge of the device.
 * The y-axis is vertical, with positive values increasing upwards (in contrast
 * to the downward orientation of most computer graphics coordinate systems).
 * The z-axis has positive values increasing away from the computer screen.
 *
 * \image html images/Leap_Axes.png
 * @since 1.0
 */

    public struct Vector
    {
        private float _x;
        private float _y;
        private float _z;

        /** Add vectors component-wise. */
        public static Vector operator + (Vector v1, Vector v2)
        {
            return v1._operator_add (v2);
        }
        /** Subtract vectors component-wise. */
        public static Vector operator - (Vector v1, Vector v2)
        {
            return v1._operator_sub (v2);
        }
        /** Multiply vector by a scalar. */
        public static Vector operator * (Vector v1, float scalar)
        {
            return v1._operator_mul (scalar);
        }
        /** Multiply vector by a scalar on the left-hand side. */
        public static Vector operator * (float scalar, Vector v1)
        {
            return v1._operator_mul (scalar);
        }
        /** Divide vector by a scalar. */
        public static Vector operator / (Vector v1, float scalar)
        {
            return v1._operator_div (scalar);
        }
        /** Negate a vector. */
        public static Vector operator - (Vector v1)
        {
            return v1._operator_sub ();
        }
        /** Compare two vectors for equality. */
        public static bool operator == (Vector v1, Vector v2)
        {
            return v1.Equals (v2);
        }

        /** Compare two vectors for equality. */
        public static bool operator != (Vector v1, Vector v2)
        {
            return !v1.Equals (v2);
        }

        /** Convert this vector to an array of three float values: [x,y,z]. */
        public float[] ToFloatArray ()
        {
            return new float[] {x, y, z};
        }


        /**
   * Creates a new Vector with the specified component values.
   *
   * \include Vector_Constructor_1.txt
   * @since 1.0
   */
        public Vector (float x, float y, float z)
        {
            _x = x;
            _y = y;
            _z = z;
        }

        /**
   * Copies the specified Vector.
   *
   * \include Vector_Constructor_2.txt
   * @since 1.0
   */
        public Vector (Vector vector)
        {
            _x = vector.x;
            _y = vector.y;
            _z = vector.z;
        }


        /**
   * The distance between the point represented by this Vector
   * object and a point represented by the specified Vector object.
   *
   * \include Vector_DistanceTo.txt
   *
   * @param other A Vector object.
   * @returns The distance from this point to the specified point.
   * @since 1.0
   */
        public float DistanceTo (Vector other)
        {
            return (float)Math.Sqrt ((x - other.x) * (x - other.x) +
                (y - other.y) * (y - other.y) +
                (z - other.z) * (z - other.z));

        }

        /**
   * The angle between this vector and the specified vector in radians.
   *
   * The angle is measured in the plane formed by the two vectors. The
   * angle returned is always the smaller of the two conjugate angles.
   * Thus <tt>A.angleTo(B) == B.angleTo(A)</tt> and is always a positive
   * value less than or equal to pi radians (180 degrees).
   *
   * If either vector has zero length, then this function returns zero.
   *
   * \image html images/Math_AngleTo.png
   *
   * \include Vector_AngleTo.txt
   *
   * @param other A Vector object.
   * @returns The angle between this vector and the specified vector in radians.
   * @since 1.0
   */
        public float AngleTo (Vector other)
        {
            float denom = this.MagnitudeSquared * other.MagnitudeSquared;
            if (denom <= Constants.EPSILON) {
                return 0.0f;
            }
            float val = this.Dot (other) / (float)Math.Sqrt (denom);
            if (val >= 1.0f) {
                return 0.0f;
            } else if (val <= -1.0f) {
                return Constants.PI;
            }
            return (float)Math.Acos (val);
        }

        /**
   * The dot product of this vector with another vector.
   *
   * The dot product is the magnitude of the projection of this vector
   * onto the specified vector.
   *
   * \image html images/Math_Dot.png
   *
   * \include Vector_Dot.txt
   *
   * @param other A Vector object.
   * @returns The dot product of this vector and the specified vector.
   * @since 1.0
   */
        public float Dot (Vector other)
        {
            return (x * other.x) + (y * other.y) + (z * other.z);
        }

        /**
   * The cross product of this vector and the specified vector.
   *
   * The cross product is a vector orthogonal to both original vectors.
   * It has a magnitude equal to the area of a parallelogram having the
   * two vectors as sides. The direction of the returned vector is
   * determined by the right-hand rule. Thus <tt>A.cross(B) == -B.cross(A).</tt>
   *
   * \image html images/Math_Cross.png
   *
   * \include Vector_Cross.txt
   *
   * @param other A Vector object.
   * @returns The cross product of this vector and the specified vector.
   * @since 1.0
   */
        public Vector Cross (Vector other)
        {
            return new Vector ((y * other.z) - (z * other.y),
                          (z * other.x) - (x * other.z),
                          (x * other.y) - (y * other.x));
        }

        /**
   * A copy of this vector pointing in the opposite direction.
   *
   * \include Vector_Negate.txt
   *
   * @returns A Vector object with all components negated.
   * @since 1.0
   */
        private Vector _operator_sub ()
        {
            return new Vector (-x, -y, -z);
        }

        /**
   * Add vectors component-wise.
   *
   * \include Vector_Plus.txt
   * @since 1.0
   */
        private Vector _operator_add (Vector other)
        {
            return new Vector (x + other.x, y + other.y, z + other.z);
        }

        /**
   * Subtract vectors component-wise.
   *
   * \include Vector_Minus.txt
   * @since 1.0
   */
        private Vector _operator_sub (Vector other)
        {
            return new Vector (x - other.x, y - other.y, z - other.z);
        }

        /**
   * Multiply vector by a scalar.
   *
   * \include Vector_Times.txt
   * @since 1.0
   */
        private Vector _operator_mul (float scalar)
        {
            return new Vector (x * scalar, y * scalar, z * scalar);
        }

        /**
   * Divide vector by a scalar.
   *
   * \include Vector_Divide.txt
   * @since 1.0
   */
        private Vector _operator_div (float scalar)
        {
            return new Vector (x / scalar, y / scalar, z / scalar);
        }

        /**
   * Returns a string containing this vector in a human readable format: (x, y, z).
   * @since 1.0
   */
        public override string ToString ()
        {
            return "(" + x + ", " + y + ", " + z + ")";
        }

        /**
   * Compare Vector equality component-wise.
   *
   * \include Vector_Equals.txt
   * @since 1.0
   */
        public override bool Equals(System.Object obj)
        {
            if (!(obj is Vector))
                return false;

            Vector v = (Vector)obj;

            return x.NearlyEquals(v.x) && y.NearlyEquals(v.y) && z.NearlyEquals(v.z);
        }

   /**
   * Returns true if all of the vector's components are finite.  If any
   * component is NaN or infinite, then this returns false.
   *
   * \include Vector_IsValid.txt
   * @since 1.0
   */
        public bool IsValid ()
        {
            return float.IsNaN (x) && float.IsNaN (y) && float.IsNaN (z);
        }

        /**
   * Index vector components numerically.
   * Index 0 is x, index 1 is y, and index 2 is z.
   * @returns The x, y, or z component of this Vector, if the specified index
   * value is at least 0 and at most 2; otherwise, returns zero.
   *
   * \include Vector_Index.txt
   * @since 1.0
   */
        private float _operator_get (uint index)
        {
            if (index == 0)
                return x;
            if (index == 1)
                return y;
            if (index == 2)
                return z;
            return 0.0f;
        }
        public float this[uint index]
        {
            get
            {
                if (index == 0)
                    return x;
                if (index == 1)
                    return y;
                if (index == 2)
                    return z;
                return 0.0f;
            }
            set
            {
                if (index == 0)
                    x = value;
                if (index == 1)
                    y = value;
                if (index == 2)
                    z = value;
            }
        }
        /**
   * The horizontal component.
   * @since 1.0
   */
        public float x {
            set {
                _x = value;
            }   
            /**
   * The horizontal component.
   * @since 1.0
   */

            get {
                return _x;
            } 
        }

        /**
   * The vertical component.
   * @since 1.0
   */
        public float y {
            set {
                _y = value;
            }   /**
   * The vertical component.
   * @since 1.0
   */

            get {
                return _y;
            } 
        }

        /**
   * The depth component.
   * @since 1.0
   */
        public float z {
            set {
                _z = value;
            }   /**
   * The depth component.
   * @since 1.0
   */

            get {
                return _z;
            } 
        }

/**
   * The magnitude, or length, of this vector.
   *
   * The magnitude is the L2 norm, or Euclidean distance between the origin and
   * the point represented by the (x, y, z) components of this Vector object.
   *
   * \include Vector_Magnitude.txt
   *
   * @returns The length of this vector.
   * @since 1.0
   */
        public float Magnitude {
            get {
                return (float)Math.Sqrt (x * x + y * y + z * z);
            } 
        }

/**
   * The square of the magnitude, or length, of this vector.
   *
   * \include Vector_Magnitude_Squared.txt
   *
   * @returns The square of the length of this vector.
   * @since 1.0
   */
        public float MagnitudeSquared {
            get {
                return x * x + y * y + z * z;
            } 
        }

/**
   * The pitch angle in radians.
   *
   * Pitch is the angle between the negative z-axis and the projection of
   * the vector onto the y-z plane. In other words, pitch represents rotation
   * around the x-axis.
   * If the vector points upward, the returned angle is between 0 and pi radians
   * (180 degrees); if it points downward, the angle is between 0 and -pi radians.
   *
   * \image html images/Math_Pitch_Angle.png
   *
   * \include Vector_Pitch.txt
   *
   * @returns The angle of this vector above or below the horizon (x-z plane).
   * @since 1.0
   */
        public float Pitch {
            get {
                return (float)Math.Atan2 (y, -z);
            } 
        }

/**
   * The roll angle in radians.
   *
   * Roll is the angle between the y-axis and the projection of
   * the vector onto the x-y plane. In other words, roll represents rotation
   * around the z-axis. If the vector points to the left of the y-axis,
   * then the returned angle is between 0 and pi radians (180 degrees);
   * if it points to the right, the angle is between 0 and -pi radians.
   *
   * \image html images/Math_Roll_Angle.png
   *
   * Use this function to get roll angle of the plane to which this vector is a
   * normal. For example, if this vector represents the normal to the palm,
   * then this function returns the tilt or roll of the palm plane compared
   * to the horizontal (x-z) plane.
   *
   * \include Vector_Roll.txt
   *
   * @returns The angle of this vector to the right or left of the y-axis.
   * @since 1.0
   */
        public float Roll {
            get {
                return (float)Math.Atan2 (x, -y);
            } 
        }

/**
   * The yaw angle in radians.
   *
   * Yaw is the angle between the negative z-axis and the projection of
   * the vector onto the x-z plane. In other words, yaw represents rotation
   * around the y-axis. If the vector points to the right of the negative z-axis,
   * then the returned angle is between 0 and pi radians (180 degrees);
   * if it points to the left, the angle is between 0 and -pi radians.
   *
   * \image html images/Math_Yaw_Angle.png
   *
   * \include Vector_Yaw.txt
   *
   * @returns The angle of this vector to the right or left of the negative z-axis.
   * @since 1.0
   */
        public float Yaw {
            get {
                return (float)Math.Atan2 (x, -z);
            } 
        }

/**
   * A normalized copy of this vector.
   *
   * A normalized vector has the same direction as the original vector,
   * but with a length of one.
   *
   * \include Vector_Normalized.txt
   *
   * @returns A Vector object with a length of one, pointing in the same
   * direction as this Vector object.
   * @since 1.0
   */
        public Vector Normalized {
            get {
                float denom = this.MagnitudeSquared;
                if (denom <= Constants.EPSILON) {
                    return Vector.Zero;
                }
                denom = 1.0f / (float)Math.Sqrt (denom);
                return new Vector (x * denom, y * denom, z * denom);
            } 
        }

/**
   * The zero vector: (0, 0, 0)
   *
   * \include Vector_Zero.txt
   * @since 1.0
   */  
        public static  Vector Zero {
            get {
                return new Vector (0, 0, 0);
            } 
        }

/**
   * The x-axis unit vector: (1, 0, 0)
   *
   * \include Vector_XAxis.txt
   * @since 1.0
   */
        public static Vector XAxis {
            get {
                return new Vector (1, 0, 0);
            } 
        }

/**
   * The y-axis unit vector: (0, 1, 0)
   *
   * \include Vector_YAxis.txt
   * @since 1.0
   */
        public static Vector YAxis {
            get {
                return new Vector (0, 1, 0);
            } 
        }

/**
   * The z-axis unit vector: (0, 0, 1)
   *
   * \include Vector_ZAxis.txt
   * @since 1.0
   */
        public static Vector ZAxis {
            get {
                return new Vector (0, 0, 1);
            } 
        }

/**
   * The unit vector pointing forward along the negative z-axis: (0, 0, -1)
   *
   * \include Vector_Forward.txt
   * @since 1.0
   */
        public static Vector Forward {
            get {
                return new Vector (0, 0, -1);
            } 
        }

/**
   * The unit vector pointing backward along the positive z-axis: (0, 0, 1)
   *
   * \include Vector_Backward.txt
   * @since 1.0
   */
        public static Vector Backward {
            get {
                return new Vector (0, 0, 1);
            } 
        }

/**
   * The unit vector pointing left along the negative x-axis: (-1, 0, 0)
   *
   * \include Vector_Left.txt
   * @since 1.0
   */
        public static Vector Left {
            get {
                return new Vector (-1, 0, 0);
            } 
        }

/**
   * The unit vector pointing right along the positive x-axis: (1, 0, 0)
   *
   * \include Vector_Right.txt
   * @since 1.0
   */
        public static Vector Right {
            get {
                return new Vector (1, 0, 0);
            } 
        }

/**
   * The unit vector pointing up along the positive y-axis: (0, 1, 0)
   *
   * \include Vector_Up.txt
   * @since 1.0
   */
        public static Vector Up {
            get {
                return new Vector (0, 1, 0);
            } 
        }

/**
   * The unit vector pointing down along the negative y-axis: (0, -1, 0)
   *
   * \include Vector_Down.txt
   * @since 1.0
   */
        public static Vector Down {
            get {
                return new Vector (0, -1, 0);
            } 
        }


   public static Vector Lerp(Vector a, Vector b, float t) {
            return new Vector(
                    a.x + t * (b.x - a.x),
                    a.y + t * (b.y - a.y),
                    a.z + t * (b.z - a.z)
                );
   }

    public override int GetHashCode()
    {
        unchecked // Overflow is fine, just wrap
        {
            int hash = 17;
            hash = hash * 23 + x.GetHashCode();
            hash = hash * 23 + y.GetHashCode();
            hash = hash * 23 + z.GetHashCode();

            return hash;
        }
    }
    }// end of Vector class
} //end namespace
