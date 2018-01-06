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
  * The LeapTransform class represents a transform in three dimensional space.
  *
  * Note that the LeapTransform class replaces the Leap.Matrix class.
  * @since 3.1.2
  */
  public struct LeapTransform
  {
    /**
    * Constructs a new transform from the specified translation and rotation.
    * @param translation the translation vector.
    * @param rotation the rotation quaternion.
    * @since 3.1.2
    */
    public LeapTransform(Vector translation, LeapQuaternion rotation) :
      this(translation, rotation, Vector.Ones)
    {
    }

    /**
    * Constructs a new transform from the specified translation, rotation and scale.
    * @param translation the translation vector.
    * @param rotation the rotation quaternion.
    * @param scale the scale vector.
    * @since 3.1.2
    */
    public LeapTransform(Vector translation, LeapQuaternion rotation, Vector scale) :
      this()
    {
      _scale = scale;
      // these are non-trival setters.
      this.translation = translation;
      this.rotation = rotation; // Calls validateBasis
    }

    /**
    * Transforms the specified position vector, applying translation, rotation and scale.
    * @param point the position vector to transform.
    * @returns the new position vector.
    * @since 3.1.2
    */
    public Vector TransformPoint(Vector point)
    {
      return _xBasisScaled * point.x + _yBasisScaled * point.y + _zBasisScaled * point.z + translation;
    }

    /**
    * Transforms the specified direction vector, applying rotation only.
    * @param direction the direction vector to transform.
    * @returns the new direction vector.
    * @since 3.1.2
    */
    public Vector TransformDirection(Vector direction)
    {
      return _xBasis * direction.x + _yBasis * direction.y + _zBasis * direction.z;
    }

    /**
    * Transforms the specified velocity vector, applying rotation and scale.
    * @param point the velocity vector to transform.
    * @returns the new velocity vector.
    * @since 3.1.2
    */
    public Vector TransformVelocity(Vector velocity)
    {
      return _xBasisScaled * velocity.x + _yBasisScaled * velocity.y + _zBasisScaled * velocity.z;
    }

    /**
    * Transforms the specified quaternion.
    * Multiplies the quaternion representing the rotational part of this transform by the specified
    * quaternion.
    *
    * **Important:** Modifying the basis vectors of this transform directly leaves the underlying quaternion in
    * an indeterminate state. Neither this function nor the LeapTransform.rotation quaternion can be used after
    * the basis vectors are set.
    *
    * @param rhs the quaternion to transform.
    * @returns the new quaternion.
    * @since 3.1.2
    */
    public LeapQuaternion TransformQuaternion(LeapQuaternion rhs)
    {
      if (_quaternionDirty)
        throw new InvalidOperationException("Calling TransformQuaternion after Basis vectors have been modified.");

      if (_flip)
      {
        // Mirror the axis of rotation accross the flip axis.
        rhs.x *= _flipAxes.x;
        rhs.y *= _flipAxes.y;
        rhs.z *= _flipAxes.z;
      }

      LeapQuaternion t = _quaternion.Multiply(rhs);
      return t;
    }

    /**
    * Mirrors this transform's rotation and scale across the x-axis. Translation is not affected.
    * @since 3.1.2
    */
    public void MirrorX()
    {
      _xBasis = -_xBasis;
      _xBasisScaled = -_xBasisScaled;

      _flip = true;
      _flipAxes.y = -_flipAxes.y;
      _flipAxes.z = -_flipAxes.z;
    }

    /**
    * Mirrors this transform's rotation and scale across the z-axis. Translation is not affected.
    * @since 3.1.2
    */
    public void MirrorZ()
    {
      _zBasis = -_zBasis;
      _zBasisScaled = -_zBasisScaled;

      _flip = true;
      _flipAxes.x = -_flipAxes.x;
      _flipAxes.y = -_flipAxes.y;
    }

    /**
    * The x-basis of the transform.
    *
    * **Important:** Modifying the basis vectors of this transform directly leaves the underlying quaternion in
    * an indeterminate state. Neither the TransformQuaternion() function nor the LeapTransform.rotation quaternion
    *  can be used after the basis vectors are set.
    *
    * @since 3.1.2
    */
    public Vector xBasis {
      get { return _xBasis; }
      set {
        _xBasis = value;
        _xBasisScaled = value * scale.x;
        _quaternionDirty = true;
      }
    }

    /**
    * The y-basis of the transform.
    *
    * **Important:** Modifying the basis vectors of this transform directly leaves the underlying quaternion in
    * an indeterminate state. Neither the TransformQuaternion() function nor the LeapTransform.rotation quaternion
    *  can be used after the basis vectors are set.
    *
    * @since 3.1.2
    */
    public Vector yBasis
    {
      get { return _yBasis; }
      set
      {
        _yBasis = value;
        _yBasisScaled = value * scale.y;
        _quaternionDirty = true;
      }
    }

    /**
    * The z-basis of the transform.
    *
    * **Important:** Modifying the basis vectors of this transform directly leaves the underlying quaternion in
    * an indeterminate state. Neither the TransformQuaternion() function nor the LeapTransform.rotation quaternion
    *  can be used after the basis vectors are set.
    *
    * @since 3.1.2
    */
    public Vector zBasis
    {
      get { return _zBasis; }
      set
      {
        _zBasis = value;
        _zBasisScaled = value * scale.z;
        _quaternionDirty = true;
      }
    }

    /**
    * The translation component of the transform.
    * @since 3.1.2
    */
    public Vector translation { get { return _translation; }
      set {
        _translation = value;
      }
    }

    /**
    * The scale factors of the transform.
    * Scale is kept separate from translation.
    * @since 3.1.2
    */
    public Vector scale {
      get { return _scale; }
      set {
        _scale = value;
        _xBasisScaled = _xBasis * scale.x;
        _yBasisScaled = _yBasis * scale.y;
        _zBasisScaled = _zBasis * scale.z;
      }
    }

    /**
    * The rotational component of the transform.
    *
    * **Important:** Modifying the basis vectors of this transform directly leaves the underlying quaternion in
    * an indeterminate state. This rotation quaternion cannot be accessed after
    * the basis vectors are modified directly.
    *
    * @since 3.1.2
    */
    public LeapQuaternion rotation {
      get {
        if (_quaternionDirty)
          throw new InvalidOperationException("Requesting rotation after Basis vectors have been modified.");
        return _quaternion;
      }
      set {
        _quaternion = value;

        float d = value.MagnitudeSquared;
        float s = 2.0f / d;
        float xs = value.x * s,   ys = value.y * s,   zs = value.z * s;
        float wx = value.w * xs,  wy = value.w * ys,  wz = value.w * zs;
        float xx = value.x * xs,  xy = value.x * ys,  xz = value.x * zs;
        float yy = value.y * ys,  yz = value.y * zs,  zz = value.z * zs;

        _xBasis = new Vector(1.0f - (yy + zz), xy + wz, xz - wy);
        _yBasis = new Vector(xy - wz, 1.0f - (xx + zz), yz + wx);
        _zBasis = new Vector(xz + wy, yz - wx, 1.0f - (xx + yy));

        _xBasisScaled = _xBasis * scale.x;
        _yBasisScaled = _yBasis * scale.y;
        _zBasisScaled = _zBasis * scale.z;

        _quaternionDirty = false;
        _flip = false;
        _flipAxes = new Vector(1.0f, 1.0f, 1.0f);
      }
    }

    /**
    * The identity transform.
    * @since 3.1.2
    */
    public static readonly LeapTransform Identity = new LeapTransform(Vector.Zero, LeapQuaternion.Identity, Vector.Ones);

    private Vector _translation;
    private Vector _scale;
    private LeapQuaternion _quaternion;
    private bool _quaternionDirty;
    private bool _flip;
    private Vector _flipAxes;
    private Vector _xBasis;
    private Vector _yBasis;
    private Vector _zBasis;
    private Vector _xBasisScaled;
    private Vector _yBasisScaled;
    private Vector _zBasisScaled;
  }
}
