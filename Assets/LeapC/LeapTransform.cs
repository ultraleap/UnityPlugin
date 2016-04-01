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
    public LeapTransform(Vector origin, LeapQuaternion rotation) :
      this()
    {
      _scale = Vector.Ones;
      this.origin = origin;
      this.quaternion = rotation;
    }

    public LeapTransform(Vector origin, LeapQuaternion rotation, Vector scale) :
      this()
    {
      _scale = scale;
      this.origin = origin;
      this.quaternion = rotation;
    }

    public Vector TransformPoint(Vector point)
    {
      return _xBasisScaled * point.x + _yBasisScaled * point.y + _zBasisScaled * point.z + _originScaled;
    }

    public Vector TransformDirection(Vector direction)
    {
      return _xBasis * direction.x + yBasis * direction.y + zBasis * direction.z;
    }

    public LeapQuaternion TransformQuaternion(LeapQuaternion rhs)
    {
      return quaternion.Multiply(rhs);
    }

    // Setting xBasis/yBasis/zBasis directly does not modify the quaternion and TransformQuaternion
    public Vector xBasis { get { return _xBasis; } set { _xBasis = value; _xBasisScaled = value * scale.x; } }
    public Vector yBasis { get { return _yBasis; } set { _yBasis = value; _yBasisScaled = value * scale.y; } }
    public Vector zBasis { get { return _zBasis; } set { _zBasis = value; _zBasisScaled = value * scale.z; } }

    public Vector origin { get { return _origin; } set { _origin = value; _originScaled = new Vector(value.x * scale.x, value.y * scale.y, value.z * scale.z); } }

    public Vector scale { get { return _scale; } set {
      _scale = value;
      // Cache scaled versions.
      xBasis = xBasis; yBasis = yBasis; zBasis = zBasis; origin = origin;
    } }

    public LeapQuaternion quaternion { get { return _quaternion; } set {
      _quaternion = value;

      // Convert the quaternion to a 3x3 rotation matrix

		  float d = value.MagnitudeSquared;
		  float s = 2.0f / d;

		  float xs = value.x * s,   ys = value.y * s,   zs = value.z * s;
		  float wx = value.w * xs,  wy = value.w * ys,  wz = value.w * zs;
		  float xx = value.x * xs,  xy = value.x * ys,  xz = value.x * zs;
		  float yy = value.y * ys,  yz = value.y * zs,  zz = value.z * zs;

      this.xBasis = new Vector(1.0f - (yy + zz),  xy - wz, xz + wy);
      this.yBasis = new Vector(xy + wz, 1.0f - (xx + zz), yz - wx);
      this.zBasis = new Vector(xz - wy, yz + wx, 1.0f - (xx + yy));
    } }

    public static readonly LeapTransform Identity = new LeapTransform(Vector.Zero, LeapQuaternion.Identity);

    private LeapQuaternion _quaternion;
    private Vector _xBasis;
    private Vector _yBasis;
    private Vector _zBasis;
    private Vector _origin;
    private Vector _scale;
    private Vector _xBasisScaled;
    private Vector _yBasisScaled;
    private Vector _zBasisScaled;
    private Vector _originScaled;
  }
}
