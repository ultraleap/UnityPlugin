/******************************************************************************\
* Copyright (C) 2012-2016 Leap Motion, Inc. All rights reserved.               *
* Leap Motion proprietary and confidential. Not for distribution.              *
* Use subject to the terms of the Leap Motion SDK Agreement available at       *
* https://developer.leapmotion.com/sdk_agreement, or another agreement         *
* between Leap Motion and you, your company or other organization.             *
\******************************************************************************/

#define DEBUG_CHECK_AGAINST_UNITY
#if DEBUG_CHECK_AGAINST_UNITY
using Leap.Unity;
using UnityEngine;
#endif

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
      Vector result = _xBasisScaled * point.x + _yBasisScaled * point.y + _zBasisScaled * point.z + translation;

#if DEBUG_CHECK_AGAINST_UNITY
      Vector3 t = _dbgFull.MultiplyPoint(result.ToVector3());
      UnityEngine.Assertions.Assert.AreApproximatelyEqual(t.x, result.x);
      UnityEngine.Assertions.Assert.AreApproximatelyEqual(t.y, result.y);
      UnityEngine.Assertions.Assert.AreApproximatelyEqual(t.z, result.z);
#endif
      return result;
    }

    public Vector TransformDirection(Vector direction)
    {
      Vector result = _xBasis * direction.x + yBasis * direction.y + zBasis * direction.z;

#if DEBUG_CHECK_AGAINST_UNITY
      Vector3 t = _dbgRot.MultiplyPoint(result.ToVector3());
      UnityEngine.Assertions.Assert.AreApproximatelyEqual(t.x, result.x);
      UnityEngine.Assertions.Assert.AreApproximatelyEqual(t.y, result.y);
      UnityEngine.Assertions.Assert.AreApproximatelyEqual(t.z, result.z);
#endif
      return result;
    }

    // This is only usable when the basis vectors have not been modified directly.
    public LeapQuaternion TransformQuaternion(LeapQuaternion rhs)
    {
      if (_quaternionDirty)
        throw new InvalidOperationException("Calling TransformQuaternion after Basis vectors have been modified.");

      LeapQuaternion t = _quaternion.Multiply(rhs);

#if DEBUG_CHECK_AGAINST_UNITY
      Quaternion dbg = _quaternion.ToQuaternion() * rhs.ToQuaternion();
      UnityEngine.Assertions.Assert.AreApproximatelyEqual(t.x, dbg.x);
      UnityEngine.Assertions.Assert.AreApproximatelyEqual(t.y, dbg.y);
      UnityEngine.Assertions.Assert.AreApproximatelyEqual(t.z, dbg.z);
      UnityEngine.Assertions.Assert.AreApproximatelyEqual(t.z, dbg.w);
#endif

      if (_flipX) {
        // Mirror the axis of rotation accross the x axis.  This has to be done after concatenation
        // as there is no way to prepend a mirror transform using quaternions.
        t.y = -t.y;
        t.z = -t.z;
      }

      return t;
    }

    // Mirror points and rotations accross the x axis.
    public void FlipX()
    {
      _flipX = !_flipX;

      _xBasis = -_xBasis;
      _xBasisScaled = -_xBasisScaled;
      _translation.x = -_translation.x;

#if DEBUG_CHECK_AGAINST_UNITY
      _dbgRot.SetColumn(2, -_dbgRot.GetColumn(2));
      _dbgFull.SetColumn(2, -_dbgFull.GetColumn(2));
      Vector4 t = _dbgFull.GetColumn(3);
      t.z = -t.z;
      _dbgFull.SetColumn(3, t);

      validateBasis();
#endif
    }

    // Setting xBasis directly makes it impossible to use access the rotation quaternion and TransformQuaternion.
    public Vector xBasis {
      get { return _xBasis; }
      set {
        _xBasis = value;
        _xBasisScaled = value * scale.x;
        _quaternionDirty = true;
#if DEBUG_CHECK_AGAINST_UNITY
        _dbgRot.SetColumn(0, value.ToVector4());
        _dbgFull.SetColumn(0, value.ToVector4() * scale.x);
        validateBasis();
#endif
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
#if DEBUG_CHECK_AGAINST_UNITY
        _dbgRot.SetColumn(1, value.ToVector4());
        _dbgFull.SetColumn(1, value.ToVector4() * scale.y);
        validateBasis();
#endif
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
#if DEBUG_CHECK_AGAINST_UNITY
        _dbgRot.SetColumn(2, value.ToVector4());
        _dbgFull.SetColumn(2, value.ToVector4() * scale.z);
        validateBasis();
#endif
      }
    }

    public Vector translation { get { return _translation; }
      set {
        _translation = value;
#if DEBUG_CHECK_AGAINST_UNITY
        _dbgFull.SetColumn(3, value.ToVector4());
#endif
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

#if DEBUG_CHECK_AGAINST_UNITY
        _dbgFull.SetColumn(0, _dbgRot.GetColumn(0) * scale.x);
        _dbgFull.SetColumn(1, _dbgRot.GetColumn(1) * scale.y);
        _dbgFull.SetColumn(2, _dbgRot.GetColumn(2) * scale.z);
        validateBasis();
#endif
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

        this.xBasis = new Vector(1.0f - (yy + zz), xy - wz,          xz + wy);
        this.yBasis = new Vector(xy + wz,          1.0f - (xx + zz), yz - wx);
        this.zBasis = new Vector(xz - wy,          yz + wx,          1.0f - (xx + yy));

      _quaternionDirty = false;
      _flipX = false;

#if DEBUG_CHECK_AGAINST_UNITY
        _dbgFull.SetTRS(_translation.ToVector3(), _quaternion.ToQuaternion(), _scale.ToVector3());
        _dbgRot.SetTRS(Vector3.zero, _quaternion.ToQuaternion(), Vector3.one);
        validateBasis();
#endif
      }
    }

    public static readonly LeapTransform Identity = new LeapTransform(Vector.Zero, LeapQuaternion.Identity, Vector.Ones);

#if DEBUG_CHECK_AGAINST_UNITY
    void validateBasis()
    {
      for (int i = 0; i < 3; ++i)
      {
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(_xBasis.ToVector4()[i], _dbgRot.GetColumn(0)[i]);
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(_yBasis.ToVector4()[i], _dbgRot.GetColumn(1)[i]);
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(_zBasis.ToVector4()[i], _dbgRot.GetColumn(2)[i]);

        UnityEngine.Assertions.Assert.AreApproximatelyEqual(_xBasisScaled.ToVector4()[i], _dbgFull.GetColumn(0)[i]);
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(_yBasisScaled.ToVector4()[i], _dbgFull.GetColumn(1)[i]);
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(_zBasisScaled.ToVector4()[i], _dbgFull.GetColumn(2)[i]);
      }
#endif
    }

    private Vector _translation;
    private Vector _scale;
    private LeapQuaternion _quaternion;
    private bool _quaternionDirty;
    private bool _flipX;
    private Vector _xBasis;
    private Vector _yBasis;
    private Vector _zBasis;
    private Vector _xBasisScaled;
    private Vector _yBasisScaled;
    private Vector _zBasisScaled;

#if DEBUG_CHECK_AGAINST_UNITY
    Matrix4x4 _dbgFull;
    Matrix4x4 _dbgRot;
#endif
  }
}
