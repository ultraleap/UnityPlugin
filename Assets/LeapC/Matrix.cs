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

/**
 * The Matrix struct represents a transformation matrix.
 *
 * To use this struct to transform a Vector, construct a matrix containing the
 * desired transformation and then use the Matrix::transformPoint() or
 * Matrix::transformDirection() functions to apply the transform.
 *
 * Transforms can be combined by multiplying two or more transform matrices using
 * the * operator.
 * @since 1.0
 */

    public struct Matrix
    {
        private Vector _xBasis;
        private Vector _yBasis;
        private Vector _zBasis;
        private Vector _origin;

        /** Multiply two matrices. */
        public static Matrix operator * (Matrix m1, Matrix m2)
        {
            return m1._operator_mul (m2);
        }
        /** Copy this matrix to the specified array of 9 float values in row-major order. */
        public float[] ToArray3x3 (float[] output)
        {
            output [0] = xBasis.x;
            output [1] = xBasis.y;
            output [2] = xBasis.z;
            output [3] = yBasis.x;
            output [4] = yBasis.y;
            output [5] = yBasis.z;
            output [6] = zBasis.x;
            output [7] = zBasis.y;
            output [8] = zBasis.z;
            return output;
        }
        /** Copy this matrix to the specified array containing 9 double values in row-major order. */
        public double[] ToArray3x3 (double[] output)
        {
            output [0] = xBasis.x;
            output [1] = xBasis.y;
            output [2] = xBasis.z;
            output [3] = yBasis.x;
            output [4] = yBasis.y;
            output [5] = yBasis.z;
            output [6] = zBasis.x;
            output [7] = zBasis.y;
            output [8] = zBasis.z;
            return output;
        }
        /** Convert this matrix to an array containing 9 float values in row-major order. */
        public float[] ToArray3x3 ()
        {
            return ToArray3x3 (new float[9]);
        }
        /** Copy this matrix to the specified array of 16 float values in row-major order. */
        public float[] ToArray4x4 (float[] output)
        {
            output [0] = xBasis.x;
            output [1] = xBasis.y;
            output [2] = xBasis.z;
            output [3] = 0.0f;
            output [4] = yBasis.x;
            output [5] = yBasis.y;
            output [6] = yBasis.z;
            output [7] = 0.0f;
            output [8] = zBasis.x;
            output [9] = zBasis.y;
            output [10] = zBasis.z;
            output [11] = 0.0f;
            output [12] = origin.x;
            output [13] = origin.y;
            output [14] = origin.z;
            output [15] = 1.0f;
            return output;
        }
        /** Copy this matrix to the specified array of 16 double values in row-major order. */
        public double[] ToArray4x4 (double[] output)
        {
            output [0] = xBasis.x;
            output [1] = xBasis.y;
            output [2] = xBasis.z;
            output [3] = 0.0f;
            output [4] = yBasis.x;
            output [5] = yBasis.y;
            output [6] = yBasis.z;
            output [7] = 0.0f;
            output [8] = zBasis.x;
            output [9] = zBasis.y;
            output [10] = zBasis.z;
            output [11] = 0.0f;
            output [12] = origin.x;
            output [13] = origin.y;
            output [14] = origin.z;
            output [15] = 1.0f;
            return output;
        }
        /** Convert this matrix to an array containing 16 float values in row-major order. */
        public float[] ToArray4x4 ()
        {
            return ToArray4x4 (new float[16]);
        }

        /**
   * Constructs a copy of the specified Matrix object.
   *
   * \include Matrix_Matrix_copy.txt
   *
   * @since 1.0
   */
        public Matrix (Matrix other)
        {
            this._xBasis = other.xBasis;
            this._yBasis = other.yBasis;
            this._zBasis = other.zBasis;
            this._origin = other.origin;
        }

        /**
   * Constructs a transformation matrix from the specified basis vectors.
   *
   * \include Matrix_Matrix_basis.txt
   *
   * @param xBasis A Vector specifying rotation and scale factors for the x-axis.
   * @param yBasis A Vector specifying rotation and scale factors for the y-axis.
   * @param zBasis A Vector specifying rotation and scale factors for the z-axis.
   * @since 1.0
   */
        public Matrix (Vector xBasis, Vector yBasis, Vector zBasis)
        {
            this._xBasis = xBasis;
            this._yBasis = yBasis;
            this._zBasis = zBasis;
            this._origin = Vector.Zero;
        }

        /**
   * Constructs a transformation matrix from the specified basis and translation vectors.
   *
   * \include Matrix_Matrix_basis_origin.txt
   *
   * @param xBasis A Vector specifying rotation and scale factors for the x-axis.
   * @param yBasis A Vector specifying rotation and scale factors for the y-axis.
   * @param zBasis A Vector specifying rotation and scale factors for the z-axis.
   * @param origin A Vector specifying translation factors on all three axes.
   * @since 1.0
   */
        public Matrix (Vector xBasis, Vector yBasis, Vector zBasis, Vector origin)
        {
            this._xBasis = xBasis;
            this._yBasis = yBasis;
            this._zBasis = zBasis;
            this._origin = origin;
        }

        /**
   * Constructs a transformation matrix specifying a rotation around the specified vector.
   *
   * \include Matrix_Matrix_rotation.txt
   *
   * @param axis A Vector specifying the axis of rotation.
   * @param angleRadians The amount of rotation in radians.
   * @since 1.0
   */
        public Matrix (Vector axis, float angleRadians)
        {
            _xBasis = Vector.XAxis;
            _yBasis = Vector.YAxis;
            _zBasis = Vector.ZAxis;
            _origin = Vector.Zero;
            this.SetRotation(axis, angleRadians);
        }

        /**
   * Constructs a transformation matrix specifying a rotation around the specified vector
   * and a translation by the specified vector.
   *
   * \include Matrix_Matrix_rotation_translation.txt
   *
   * @param axis A Vector specifying the axis of rotation.
   * @param angleRadians The angle of rotation in radians.
   * @param translation A Vector representing the translation part of the transform.
   * @since 1.0
   */
        public Matrix (Vector axis, float angleRadians, Vector translation)
        {
            _xBasis = Vector.XAxis;
            _yBasis = Vector.YAxis;
            _zBasis = Vector.ZAxis;
            _origin = translation;
            this.SetRotation(axis, angleRadians);
        }

        public Matrix(float m00,
                      float m01,
                      float m02,
                      float m10,
                      float m11,
                      float m12,
                      float m20,
                      float m21,
                      float m22){
            _xBasis = new Vector(m00, m01, m02);
            _yBasis = new Vector(m10, m11, m12);
            _zBasis = new Vector(m20, m21, m22);
            _origin = Vector.Zero;
        }

        public Matrix(float m00,
                      float m01,
                      float m02,
                      float m10,
                      float m11,
                      float m12,
                      float m20,
                      float m21,
                      float m22,
                      float m30,
                      float m31,
                      float m32){
            _xBasis = new Vector(m00, m01, m02);
            _yBasis = new Vector(m10, m11, m12);
            _zBasis = new Vector(m20, m21, m22);
            _origin = new Vector(m30, m31, m32);
        }

        /**
   * Sets this transformation matrix to represent a rotation around the specified vector.
   *
   * \include Matrix_setRotation.txt
   *
   * This function erases any previous rotation and scale transforms applied
   * to this matrix, but does not affect translation.
   *
   * @param axis A Vector specifying the axis of rotation.
   * @param angleRadians The amount of rotation in radians.
   * @since 1.0
   */
        public void SetRotation (Vector axis, float angleRadians)
        {
            Vector n = axis.Normalized;
            float s = (float)Math.Sin(angleRadians);
            float c = (float)Math.Cos(angleRadians);
            float C = (1-c);
            
            xBasis = new Vector(n[0]*n[0]*C + c,      n[0]*n[1]*C - n[2]*s, n[0]*n[2]*C + n[1]*s);
            yBasis = new Vector(n[1]*n[0]*C + n[2]*s, n[1]*n[1]*C + c,      n[1]*n[2]*C - n[0]*s);
            zBasis = new Vector(n[2]*n[0]*C - n[1]*s, n[2]*n[1]*C + n[0]*s, n[2]*n[2]*C + c     );
        }

        //TODO functions for getting Axis and Angle from matrix

        /**
   * Transforms a vector with this matrix by transforming its rotation,
   * scale, and translation.
   *
   * \include Matrix_transformPoint.txt
   *
   * Translation is applied after rotation and scale.
   *
   * @param point The Vector to transform.
   * @returns A new Vector representing the transformed original.
   * @since 1.0
   */
        public Vector TransformPoint (Vector point)
        {
            return xBasis * point.x + yBasis * point.y + zBasis * point.z + origin;
        }

        /**
   * Transforms a vector with this matrix by transforming its rotation and
   * scale only.
   *
   * \include Matrix_transformDirection.txt
   *
   * @param in The Vector to transform.
   * @returns A new Vector representing the transformed original.
   * @since 1.0
   */
        public Vector TransformDirection (Vector direction)
        {
            return xBasis * direction.x + yBasis * direction.y + zBasis * direction.z;
        }

        /**
   * Performs a matrix inverse if the matrix consists entirely of rigid
   * transformations (translations and rotations).  If the matrix is not rigid,
   * this operation will not represent an inverse.
   *
   * \include Matrix_rigidInverse.txt
   *
   * Note that all matrices that are directly returned by the API are rigid.
   *
   * @returns The rigid inverse of the matrix.
   * @since 1.0
   */
        public Matrix RigidInverse ()
        {
            Matrix rotInverse = new Matrix(new Vector(xBasis[0], yBasis[0], zBasis[0]),
                                           new Vector(xBasis[1], yBasis[1], zBasis[1]),
                                           new Vector(xBasis[2], yBasis[2], zBasis[2]));
            rotInverse.origin = rotInverse.TransformDirection( -origin );
            return rotInverse;
        }

        /**
   * Multiply transform matrices.
   *
   * Combines two transformations into a single equivalent transformation.
   *
   * \include Matrix_operator_times.txt
   *
   * @param other A Matrix to multiply on the right hand side.
   * @returns A new Matrix representing the transformation equivalent to
   * applying the other transformation followed by this transformation.
   * @since 1.0
   */
        private Matrix _operator_mul (Matrix other)
        {
            return new Matrix(TransformDirection(other.xBasis),
                              TransformDirection(other.yBasis),
                              TransformDirection(other.zBasis),
                              TransformPoint(other.origin));
        }

        /**
   * Compare Matrix equality component-wise.
   *
   * \include Matrix_operator_equals.txt
   *
   * @since 1.0
   */
        public bool Equals (Matrix other)
        {
            return xBasis == other.xBasis &&
                   yBasis == other.yBasis &&
                   zBasis == other.zBasis &&
                   origin == other.origin;
        }

        /**
   * Write the matrix to a string in a human readable format.
   * @since 1.0
   */
        public override string ToString ()
        {
            return "xBasis: " + xBasis.ToString() +
                  " yBasis: " + yBasis.ToString() +
                  " zBasis: " + zBasis.ToString() + 
                  " origin: " + origin.ToString ();
        }

        /**
   * The basis vector for the x-axis.
   *
   * \include Matrix_xBasis.txt
   *
   * @since 1.0
   */
        public Vector xBasis {
            set {
                _xBasis = value;
            }   
   /**
   * The basis vector for the x-axis.
   *
   * \include Matrix_xBasis.txt
   *
   * @since 1.0
   */

            get {
                return _xBasis;
            } 
        }

        /**
   * The basis vector for the y-axis.
   *
   * \include Matrix_yBasis.txt
   *
   * @since 1.0
   */
        public Vector yBasis {
            set {
                _yBasis = value;
            }   
   /**
   * The basis vector for the y-axis.
   *
   * \include Matrix_yBasis.txt
   *
   * @since 1.0
   */

            get {
                return _yBasis;
            } 
        }

        /**
   * The basis vector for the z-axis.
   *
   * \include Matrix_zBasis.txt
   *
   * @since 1.0
   */
        public Vector zBasis {
            set {
                _zBasis = value;    
            }  
    /**
   * The basis vector for the z-axis.
   *
   * \include Matrix_zBasis.txt
   *
   * @since 1.0
   */

            get {
                return _zBasis;
            } 
        }

        /**
   * The translation factors for all three axes.
   *
   * \include Matrix_origin.txt
   *
   * @since 1.0
   */
        public Vector origin {
            set {
                _origin = value; 
            }   
   /**
   * The translation factors for all three axes.
   *
   * \include Matrix_origin.txt
   *
   * @since 1.0
   */

            get {
                return _origin;
            } 
        }

/**
   * Returns the identity matrix specifying no translation, rotation, and scale.
   *
   * \include Matrix_identity.txt
   *
   * @returns The identity matrix.
   * @since 1.0
   */
        public static Matrix Identity {
            get {
                return new Matrix (Vector.XAxis, 
                                   Vector.YAxis, 
                                   Vector.ZAxis, 
                                   Vector.Zero);
            } 
        }



    }

}
