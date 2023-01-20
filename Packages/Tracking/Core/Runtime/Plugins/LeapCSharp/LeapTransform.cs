/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/
using UnityEngine;

namespace Leap
{
    using System;
    /// <summary>
    /// The LeapTransform class represents a transform in three dimensional space.
    /// 
    /// Note that the LeapTransform class replaces the Leap.Matrix class.
    /// @since 3.1.2
    /// </summary>
    public struct LeapTransform
    {
        /// <summary>
        /// Constructs a new transform from the specified translation and rotation.
        /// </summary>
        public LeapTransform(Vector3 translation, Quaternion rotation) :
          this(translation, rotation, Vector3.one)
        {
        }

        /// <summary>
        /// Constructs a new transform from the specified translation, rotation and scale.
        /// </summary>
        public LeapTransform(Vector3 translation, Quaternion rotation, Vector3 scale) :
          this()
        {
            this.scale = scale;
            // these are non-trival setters.
            this.translation = translation;
            this.rotation = rotation; // Calls validateBasis
        }

        /// <summary>
        /// Constructs a new Leap transform from a Unity Transform
        /// </summary>
        /// <param name="t">Unity Transform</param>
        public LeapTransform(Transform t) : this()
        {
            this.scale = t.lossyScale;
            this.translation = t.position;
            this.rotation = t.rotation;
        }

        /// <summary>
        /// Transforms the specified position vector, applying translation, rotation and scale.
        /// </summary>
        public Vector3 TransformPoint(Vector3 point)
        {
            return _xBasisScaled * point.x + _yBasisScaled * point.y + _zBasisScaled * point.z + translation;
        }

        /// <summary>
        /// Transforms the specified direction vector, applying rotation only.
        /// </summary>
        public Vector3 TransformDirection(Vector3 direction)
        {
            return _xBasis * direction.x + _yBasis * direction.y + _zBasis * direction.z;
        }

        /// <summary>
        /// Transforms the specified velocity vector, applying rotation and scale.
        /// </summary>
        public Vector3 TransformVelocity(Vector3 velocity)
        {
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
        /// </summary>
        public Quaternion TransformQuaternion(Quaternion rhs)
        {
            if (_quaternionDirty)
                throw new InvalidOperationException("Calling TransformQuaternion after Basis vectors have been modified.");

            if (_flip)
            {
                // Mirror the axis of rotation across the flip axis.
                rhs.x *= _flipAxes.x;
                rhs.y *= _flipAxes.y;
                rhs.z *= _flipAxes.z;
            }

            Quaternion t = _quaternion * rhs;
            return t;
        }

        /// <summary>
        /// Mirrors this transform's rotation and scale across the x-axis. Translation is not affected.
        /// @since 3.1.2
        /// </summary>
        public void MirrorX()
        {
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
        public void MirrorZ()
        {
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
        public Vector3 xBasis
        {
            get { return _xBasis; }
            set
            {
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
        public Vector3 yBasis
        {
            get { return _yBasis; }
            set
            {
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
        public Vector3 zBasis
        {
            get { return _zBasis; }
            set
            {
                _zBasis = value;
                _zBasisScaled = value * scale.z;
                _quaternionDirty = true;
            }
        }

        /// <summary>
        /// The translation component of the transform.
        /// @since 3.1.2
        /// </summary>
        public Vector3 translation
        {
            get { return _translation; }
            set
            {
                _translation = value;
            }
        }

        /// <summary>
        /// The scale factors of the transform.
        /// Scale is kept separate from translation.
        /// @since 3.1.2
        /// </summary>
        public Vector3 scale
        {
            get { return _scale; }
            set
            {
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
        public Quaternion rotation
        {
            get
            {
                if (_quaternionDirty)
                    throw new InvalidOperationException("Requesting rotation after Basis vectors have been modified.");
                return _quaternion;
            }
            set
            {
                _quaternion = value;

                float d = value.x * value.x + value.y * value.y + value.z * value.z + value.w * value.w;
                float s = 2.0f / d;
                float xs = value.x * s, ys = value.y * s, zs = value.z * s;
                float wx = value.w * xs, wy = value.w * ys, wz = value.w * zs;
                float xx = value.x * xs, xy = value.x * ys, xz = value.x * zs;
                float yy = value.y * ys, yz = value.y * zs, zz = value.z * zs;

                _xBasis = new Vector3(1.0f - (yy + zz), xy + wz, xz - wy);
                _yBasis = new Vector3(xy - wz, 1.0f - (xx + zz), yz + wx);
                _zBasis = new Vector3(xz + wy, yz - wx, 1.0f - (xx + yy));

                _xBasisScaled = _xBasis * scale.x;
                _yBasisScaled = _yBasis * scale.y;
                _zBasisScaled = _zBasis * scale.z;

                _quaternionDirty = false;
                _flip = false;
                _flipAxes = new Vector3(1.0f, 1.0f, 1.0f);
            }
        }

        /// <summary>
        /// The identity transform.
        /// @since 3.1.2
        /// </summary>
        public static readonly LeapTransform Identity = new LeapTransform(Vector3.zero, Quaternion.identity, Vector3.one);

        private Vector3 _translation;
        private Vector3 _scale;
        private Quaternion _quaternion;
        private bool _quaternionDirty;
        private bool _flip;
        private Vector3 _flipAxes;
        private Vector3 _xBasis;
        private Vector3 _yBasis;
        private Vector3 _zBasis;
        private Vector3 _xBasisScaled;
        private Vector3 _yBasisScaled;
        private Vector3 _zBasisScaled;
    }
}