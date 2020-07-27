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
  /// The LeapTransform class represents a transform in three dimensional space.
  /// 
  /// Note that the LeapTransform class replaces the Leap.Matrix class.
  /// @since 3.1.2
  /// </summary>
  public struct LeapTransform {
    /// <summary>
    /// Constructs a new transform from the specified translation and rotation.
    /// @since 3.1.2
    /// </summary>
    public LeapTransform(Vector translation, LeapQuaternion rotation) :
      this(translation, rotation, Vector.Ones) {
    }

    /// <summary>
    /// Constructs a new transform from the specified translation, rotation and scale.
    /// @since 3.1.2
    /// </summary>
    public LeapTransform(Vector translation, LeapQuaternion rotation, Vector scale) :
      this() {
      _scale = scale;
      // these are non-trival setters.
      this.translation = translation;
      this.rotation = rotation; // Calls validateBasis
    }

    /// <summary>
    /// Transforms the specified position vector, applying translation, rotation and scale.
    /// @since 3.1.2
    /// </summary>
    public Vector TransformPoint(Vector point) {
      return _xBasisScaled * point.x + _yBasisScaled * point.y + _zBasisScaled * point.z + translation;
    }

    /// <summary>
    /// Transforms the specified direction vector, applying rotation only.
    /// @since 3.1.2
    /// </summary>
    public Vector TransformDirection(Vector direction) {
      return _xBasis * direction.x + _yBasis * direction.y + _zBasis * direction.z;
    }

    /// <summary>
    /// Transforms the specified velocity vector, applying rotation and scale.
    /// @since 3.1.2
    /// </summary>
    public Vector TransformVelocity(Vector velocity) {
      return _xBasisScaled * velocity.x + _yBasisScaled * velocity.y + _zBasisScaled * velocity.z;
    }

    /// <summary>
    /// Transforms the specified quaternion.
    /// Multiplies the quaternion representing the rotational part of this transform by the specified
    /// quaternion.
    ///
    /// **Important:** Modifying the basis vectors of this transform directly leaves the underlying quaternion in
    /// an indeterminate state. Neither this function nor the LeapTransform.rotation quaternion can be used after
    /// the basis vectors are set.
    ///
    /// @since 3.1.2
    /// </summary>
    public LeapQuaternion TransformQuaternion(LeapQuaternion rhs) {
      if (_quaternionDirty)
        throw new InvalidOperationException("Calling TransformQuaternion after Basis vectors have been modified.");

      if (_flip) {
        // Mirror the axis of rotation across the flip axis.
        rhs.x *= _flipAxes.x;
        rhs.y *= _flipAxes.y;
        rhs.z *= _flipAxes.z;
      }

      LeapQuaternion t = _quaternion.Multiply(rhs);
      return t;
    }

    /// <summary>
    /// Mirrors this transform's rotation and scale across the x-axis. Translation is not affected.
    /// @since 3.1.2
    /// </summary>
    public void MirrorX() {
      _xBasis = -_xBasis;
      _xBasisScaled = -_xBasisScaled;

      _flip = true;
      _flipAxes.y = -_flipAxes.y;
      _flipAxes.z = -_flipAxes.z;
    }

    /// <summary>
    /// Mirrors this transform's rotation and scale across the z-axis. Translation is not affected.
    /// @since 3.1.2
    /// </summary>
    public void MirrorZ() {
      _zBasis = -_zBasis;
      _zBasisScaled = -_zBasisScaled;

      _flip = true;
      _flipAxes.x = -_flipAxes.x;
      _flipAxes.y = -_flipAxes.y;
    }

    /// <summary>
    /// The x-basis of the transform.
    ///
    /// **Important:** Modifying the basis vectors of this transform directly leaves the underlying quaternion in
    /// an indeterminate state. Neither the TransformQuaternion() function nor the LeapTransform.rotation quaternion
    ///  can be used after the basis vectors are set.
    ///
    /// @since 3.1.2
    /// </summary>
    public Vector xBasis {
      get { return _xBasis; }
      set {
        _xBasis = value;
        _xBasisScaled = value * scale.x;
        _quaternionDirty = true;
      }
    }

    /// <summary>
    /// The y-basis of the transform.
    ///
    /// **Important:** Modifying the basis vectors of this transform directly leaves the underlying quaternion in
    /// an indeterminate state. Neither the TransformQuaternion() function nor the LeapTransform.rotation quaternion
    ///  can be used after the basis vectors are set.
    ///
    /// @since 3.1.2
    /// </summary>
    public Vector yBasis {
      get { return _yBasis; }
      set {
        _yBasis = value;
        _yBasisScaled = value * scale.y;
        _quaternionDirty = true;
      }
    }

    /// <summary>
    /// The z-basis of the transform.
    ///
    /// **Important:** Modifying the basis vectors of this transform directly leaves the underlying quaternion in
    /// an indeterminate state. Neither the TransformQuaternion() function nor the LeapTransform.rotation quaternion
    ///  can be used after the basis vectors are set.
    ///
    /// @since 3.1.2
    /// </summary>
    public Vector zBasis {
      get { return _zBasis; }
      set {
        _zBasis = value;
        _zBasisScaled = value * scale.z;
        _quaternionDirty = true;
      }
    }

    /// <summary>
    /// The translation component of the transform.
    /// @since 3.1.2
    /// </summary>
    public Vector translation {
      get { return _translation; }
      set {
        _translation = value;
      }
    }

    /// <summary>
    /// The scale factors of the transform.
    /// Scale is kept separate from translation.
    /// @since 3.1.2
    /// </summary>
    public Vector scale {
      get { return _scale; }
      set {
        _scale = value;
        _xBasisScaled = _xBasis * scale.x;
        _yBasisScaled = _yBasis * scale.y;
        _zBasisScaled = _zBasis * scale.z;
      }
    }

    /// <summary>
    /// The rotational component of the transform.
    ///
    /// **Important:** Modifying the basis vectors of this transform directly leaves the underlying quaternion in
    /// an indeterminate state. This rotation quaternion cannot be accessed after
    /// the basis vectors are modified directly.
    ///
    /// @since 3.1.2
    /// </summary>
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
        float xs = value.x * s, ys = value.y * s, zs = value.z * s;
        float wx = value.w * xs, wy = value.w * ys, wz = value.w * zs;
        float xx = value.x * xs, xy = value.x * ys, xz = value.x * zs;
        float yy = value.y * ys, yz = value.y * zs, zz = value.z * zs;

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

    /// <summary>
    /// The identity transform.
    /// @since 3.1.2
    /// </summary>
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
