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

  public struct LeapTransform
  {
    public LeapTransform(Vector translation, LeapQuaternion rotation) :
      this(translation, rotation, Vector.Ones)
    {
    }

    public LeapTransform(Vector translation, LeapQuaternion rotation, Vector scale) :
      this()
    {
      _scale = scale;
      // these are non-trival setters.
      this.translation = translation;
      this.rotation = rotation; // Calls validateBasis
    }

    public Vector TransformPoint(Vector point)
    {
      return _xBasisScaled * point.x + _yBasisScaled * point.y + _zBasisScaled * point.z + translation;
    }

    public Vector TransformDirection(Vector direction)
    {
      return _xBasis * direction.x + _yBasis * direction.y + _zBasis * direction.z;
    }

    public Vector TransformVelocity(Vector velocity)
    {
      return _xBasisScaled * velocity.x + _yBasisScaled * velocity.y + _zBasisScaled * velocity.z;
    }

    // This is only usable when the basis vectors have not been modified directly.
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

    // Additionally mirror transformed data accross the X axis.  Note translation applied is unchanged.
    public void MirrorX()
    {
      _xBasis = -_xBasis;
      _xBasisScaled = -_xBasisScaled;

      _flip = true;
      _flipAxes.y = -_flipAxes.y;
      _flipAxes.z = -_flipAxes.z;
    }

    // Additionally mirror transformed data accross the X axis.  Note translation applied is unchanged.
    public void MirrorZ()
    {
      _zBasis = -_zBasis;
      _zBasisScaled = -_zBasisScaled;

      _flip = true;
      _flipAxes.x = -_flipAxes.x;
      _flipAxes.y = -_flipAxes.y;
    }

    // Setting xBasis directly makes it impossible to use access the rotation quaternion and TransformQuaternion.
    public Vector xBasis {
      get { return _xBasis; }
      set {
        _xBasis = value;
        _xBasisScaled = value * scale.x;
        _quaternionDirty = true;
      }
    }

    // Setting yBasis directly makes it impossible to use access the rotation quaternion and TransformQuaternion.
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

    // Setting zBasis directly makes it impossible to use access the rotation quaternion and TransformQuaternion.
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

    public Vector translation { get { return _translation; }
      set {
        _translation = value;
      }
    }

    // The scale does not influence the translation.
    public Vector scale {
      get { return _scale; }
      set {
        _scale = value;
        _xBasisScaled = _xBasis * scale.x;
        _yBasisScaled = _yBasis * scale.y;
        _zBasisScaled = _zBasis * scale.z;
      }
    }

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
