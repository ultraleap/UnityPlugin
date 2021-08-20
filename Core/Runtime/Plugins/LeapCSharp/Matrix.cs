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
  /// The Matrix struct represents a transformation matrix.
  /// 
  /// To use this struct to transform a Vector, construct a matrix containing the
  /// desired transformation and then use the Matrix::transformPoint() or
  /// Matrix.TransformDirection() functions to apply the transform.
  /// 
  /// Transforms can be combined by multiplying two or more transform matrices using
  /// the * operator.
  /// @since 1.0
  /// </summary>
  public struct Matrix {

    /// <summary>
    /// Multiply two matrices.
    /// </summary>
    public static Matrix operator *(Matrix m1, Matrix m2) {
      return m1._operator_mul(m2);
    }
    
    /// <summary>
    /// Copy this matrix to the specified array of 9 float values in row-major order.
    /// </summary>
    public float[] ToArray3x3(float[] output) {
      output[0] = xBasis.x;
      output[1] = xBasis.y;
      output[2] = xBasis.z;
      output[3] = yBasis.x;
      output[4] = yBasis.y;
      output[5] = yBasis.z;
      output[6] = zBasis.x;
      output[7] = zBasis.y;
      output[8] = zBasis.z;
      return output;
    }

    /// <summary>
    /// Copy this matrix to the specified array containing 9 double values in row-major order.
    /// </summary>
    public double[] ToArray3x3(double[] output) {
      output[0] = xBasis.x;
      output[1] = xBasis.y;
      output[2] = xBasis.z;
      output[3] = yBasis.x;
      output[4] = yBasis.y;
      output[5] = yBasis.z;
      output[6] = zBasis.x;
      output[7] = zBasis.y;
      output[8] = zBasis.z;
      return output;
    }
    
    /// <summary>
    /// Convert this matrix to an array containing 9 float values in row-major order.
    /// </summary>
    public float[] ToArray3x3() {
      return ToArray3x3(new float[9]);
    }

    /// <summary>
    /// Copy this matrix to the specified array of 16 float values in row-major order.
    /// </summary>
    public float[] ToArray4x4(float[] output) {
      output[0] = xBasis.x;
      output[1] = xBasis.y;
      output[2] = xBasis.z;
      output[3] = 0.0f;
      output[4] = yBasis.x;
      output[5] = yBasis.y;
      output[6] = yBasis.z;
      output[7] = 0.0f;
      output[8] = zBasis.x;
      output[9] = zBasis.y;
      output[10] = zBasis.z;
      output[11] = 0.0f;
      output[12] = origin.x;
      output[13] = origin.y;
      output[14] = origin.z;
      output[15] = 1.0f;
      return output;
    }

    /// <summary>
    /// Copy this matrix to the specified array of 16 double values in row-major order.
    /// </summary>
    public double[] ToArray4x4(double[] output) {
      output[0] = xBasis.x;
      output[1] = xBasis.y;
      output[2] = xBasis.z;
      output[3] = 0.0f;
      output[4] = yBasis.x;
      output[5] = yBasis.y;
      output[6] = yBasis.z;
      output[7] = 0.0f;
      output[8] = zBasis.x;
      output[9] = zBasis.y;
      output[10] = zBasis.z;
      output[11] = 0.0f;
      output[12] = origin.x;
      output[13] = origin.y;
      output[14] = origin.z;
      output[15] = 1.0f;
      return output;
    }
    
    /// <summary>
    /// Convert this matrix to an array containing 16 float values in row-major order.
    /// </summary>
    public float[] ToArray4x4() {
      return ToArray4x4(new float[16]);
    }

    /// <summary>
    /// Constructs a copy of the specified Matrix object.
    /// @since 1.0
    /// </summary>
    public Matrix(Matrix other) :
      this() {
      xBasis = other.xBasis;
      yBasis = other.yBasis;
      zBasis = other.zBasis;
      origin = other.origin;
    }

    /// <summary>
    /// Constructs a transformation matrix from the specified basis vectors.
    /// @since 1.0
    /// </summary>
    public Matrix(Vector xBasis, Vector yBasis, Vector zBasis) :
      this() {
      this.xBasis = xBasis;
      this.yBasis = yBasis;
      this.zBasis = zBasis;
      this.origin = Vector.Zero;
    }

    /// <summary>
    /// Constructs a transformation matrix from the specified basis and translation vectors.
    /// @since 1.0
    /// </summary>
    public Matrix(Vector xBasis, Vector yBasis, Vector zBasis, Vector origin) :
      this() {
      this.xBasis = xBasis;
      this.yBasis = yBasis;
      this.zBasis = zBasis;
      this.origin = origin;
    }

    /// <summary>
    /// Constructs a transformation matrix specifying a rotation around the specified vector.
    /// @since 1.0
    /// </summary>
    public Matrix(Vector axis, float angleRadians) :
      this() {
      xBasis = Vector.XAxis;
      yBasis = Vector.YAxis;
      zBasis = Vector.ZAxis;
      origin = Vector.Zero;
      SetRotation(axis, angleRadians);
    }

    /// <summary>
    /// Constructs a transformation matrix specifying a rotation around the specified vector
    /// and a translation by the specified vector.
    /// @since 1.0
    /// </summary>
    public Matrix(Vector axis, float angleRadians, Vector translation) :
      this() {
      xBasis = Vector.XAxis;
      yBasis = Vector.YAxis;
      zBasis = Vector.ZAxis;
      origin = translation;
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
                  float m22) :
      this() {
      xBasis = new Vector(m00, m01, m02);
      yBasis = new Vector(m10, m11, m12);
      zBasis = new Vector(m20, m21, m22);
      origin = Vector.Zero;
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
                  float m32) :
      this() {
      xBasis = new Vector(m00, m01, m02);
      yBasis = new Vector(m10, m11, m12);
      zBasis = new Vector(m20, m21, m22);
      origin = new Vector(m30, m31, m32);
    }

    /// <summary>
    /// Sets this transformation matrix to represent a rotation around the specified vector.
    /// 
    /// This function erases any previous rotation and scale transforms applied
    /// to this matrix, but does not affect translation.
    /// 
    /// @since 1.0
    /// </summary>
    public void SetRotation(Vector axis, float angleRadians) {
      Vector n = axis.Normalized;
      float s = (float)Math.Sin(angleRadians);
      float c = (float)Math.Cos(angleRadians);
      float C = (1 - c);

      xBasis = new Vector(n[0] * n[0] * C + c, n[0] * n[1] * C - n[2] * s, n[0] * n[2] * C + n[1] * s);
      yBasis = new Vector(n[1] * n[0] * C + n[2] * s, n[1] * n[1] * C + c, n[1] * n[2] * C - n[0] * s);
      zBasis = new Vector(n[2] * n[0] * C - n[1] * s, n[2] * n[1] * C + n[0] * s, n[2] * n[2] * C + c);
    }

    /// <summary>
    /// Transforms a vector with this matrix by transforming its rotation,
    /// scale, and translation.
    /// 
    /// Translation is applied after rotation and scale.
    /// 
    /// @since 1.0
    /// </summary>
    public Vector TransformPoint(Vector point) {
      return xBasis * point.x + yBasis * point.y + zBasis * point.z + origin;
    }

    /// <summary>
    /// Transforms a vector with this matrix by transforming its rotation and
    /// scale only.
    /// @since 1.0
    /// </summary>
    public Vector TransformDirection(Vector direction) {
      return xBasis * direction.x + yBasis * direction.y + zBasis * direction.z;
    }

    /// <summary>
    /// Performs a matrix inverse if the matrix consists entirely of rigid
    /// transformations (translations and rotations).  If the matrix is not rigid,
    /// this operation will not represent an inverse.
    /// 
    /// Note that all matrices that are directly returned by the API are rigid.
    /// 
    /// @since 1.0
    /// </summary>
    public Matrix RigidInverse() {
      Matrix rotInverse = new Matrix(new Vector(xBasis[0], yBasis[0], zBasis[0]),
                                     new Vector(xBasis[1], yBasis[1], zBasis[1]),
                                     new Vector(xBasis[2], yBasis[2], zBasis[2]));
      rotInverse.origin = rotInverse.TransformDirection(-origin);
      return rotInverse;
    }

    /// <summary>
    /// Multiply transform matrices.
    /// Combines two transformations into a single equivalent transformation.
    /// @since 1.0
    /// </summary>
    private Matrix _operator_mul(Matrix other) {
      return new Matrix(TransformDirection(other.xBasis),
                        TransformDirection(other.yBasis),
                        TransformDirection(other.zBasis),
                        TransformPoint(other.origin));
    }

    /// <summary>
    /// Compare Matrix equality component-wise.
    /// @since 1.0
    /// </summary>
    public bool Equals(Matrix other) {
      return xBasis == other.xBasis &&
             yBasis == other.yBasis &&
             zBasis == other.zBasis &&
             origin == other.origin;
    }

    /// <summary>
    /// Write the matrix to a string in a human readable format.
    /// </summary>
    public override string ToString() {
      return string.Format("xBasis: {0} yBasis: {1} zBasis: {2} origin: {3}", xBasis, yBasis, zBasis, origin);
    }

    /// <summary>
    /// The basis vector for the x-axis.
    /// @since 1.0
    /// </summary>
    public Vector xBasis { get; set; }

    /// <summary>
    /// The basis vector for the y-axis.
    /// @since 1.0
    /// </summary>
    public Vector yBasis { get; set; }

    /// <summary>
    /// The basis vector for the z-axis.
    /// @since 1.0
    /// </summary>
    public Vector zBasis { get; set; }

    /// <summary>
    /// The translation factors for all three axes.
    /// @since 1.0
    /// </summary>
    public Vector origin { get; set; }

    /// <summary>
    /// Returns the identity matrix specifying no translation, rotation, and scale.
    /// @since 1.0
    /// </summary>
    public static readonly Matrix Identity = new Matrix(Vector.XAxis, Vector.YAxis, Vector.ZAxis, Vector.Zero);
  }
}
