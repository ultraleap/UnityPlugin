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
   * The Bone class represents a tracked bone.
   *
   * All fingers contain 4 bones that make up the anatomy of the finger.
   * Get valid Bone objects from a Finger object.
   *
   * Bones are ordered from base to tip, indexed from 0 to 3.  Additionally, the
   * bone's Type enum may be used to index a specific bone anatomically.
   *
   * \include Bone_iteration.txt
   *
   * The thumb does not have a base metacarpal bone and therefore contains a valid,
   * zero length bone at that location.
   *
   * Note that Bone objects can be invalid, which means that they do not contain
   * valid tracking data and do not correspond to a physical bone. Invalid Bone
   * objects can be the result of asking for a Bone object from an invalid finger,
   * indexing a bone out of range, or constructing a new bone.
   * Test for validity with the Bone::isValid() function.
   * @since 2.0
   */

    public class Bone
    {
        Vector _prevJoint;
        Vector _nextJoint;
        Vector _center;
        Vector _direction;
        float _length = 0;
        float _width = 0;
        Bone.BoneType _type;
        Matrix _basis;
        bool _isValid = false;

        /**
     * Constructs an invalid Bone object.
     *
     * \include Bone_invalid.txt
     *
     * Get valid Bone objects from a Finger object.
     *
     * @since 2.0
     */
        public Bone ()
        {
            _prevJoint = Vector.Zero;
            _nextJoint = Vector.Zero;
            _basis = Matrix.Identity;
            _center = Vector.Zero;
            _direction = Vector.Zero;
            _type = BoneType.TYPE_METACARPAL; //There is no invalid BoneType
        }

        public Bone(Vector prevJoint,
                    Vector nextJoint,
                    Vector center,
                    Vector direction,
                    float length,
                    float width,
                    Bone.BoneType type,
                    Matrix basis
                    ){
            _prevJoint = prevJoint;
            _nextJoint = nextJoint;
            _center = center;
            _direction = direction;
            _basis = basis;
            _length = length;
            _width = width;
            _type = type;
            _isValid = true;
        }

        public Bone TransformedCopy(Matrix trs){
            float dScale = trs.zBasis.Magnitude;
            float hScale = trs.xBasis.Magnitude;
            return new Bone(trs.TransformPoint(_prevJoint),
                trs.TransformPoint(_nextJoint),
                trs.TransformPoint(_center),
                trs.TransformDirection(_direction).Normalized,
                _length * dScale,
                _width * hScale,
                _type,
                trs * _basis);
        }
        /**
     * Compare Bone object equality.
     *
     * Two Bone objects are equal if and only if both Bone objects represent the
     * exact same physical bone in the same frame and both Bone objects are valid.
     * @since 2.0
     */
        public bool Equals (Bone other)
        {
            return  this.IsValid &&
                    other.IsValid &&
                    (this.Center == other.Center) &&
                    (this.Direction == other.Direction) &&
                    (this.Length == other.Length);
        }

        /**
     * A string containing a brief, human readable description of the Bone object.
     *
     * \include Bone_toString.txt
     *
     * @returns A description of the Bone object as a string.
     * @since 2.0
     */
        public override string ToString ()
        {
            return Enum.GetName(typeof(BoneType), this.Type) + " bone";
        }

/**
     * The base of the bone, closest to the wrist.
     *
     * In anatomical terms, this is the proximal end of the bone.

     * \include Bone_prevJoint.txt
     *
     * @returns The Vector containing the coordinates of the previous joint position.
     * @since 2.0
     */
        public Vector PrevJoint {
            get {
                return _prevJoint;
            } 
        }

/**
     * The end of the bone, closest to the finger tip.
     *
     * In anatomical terms, this is the distal end of the bone.
     *
     * \include Bone_nextJoint.txt
     *
     * @returns The Vector containing the coordinates of the next joint position.
     * @since 2.0
     */
        public Vector NextJoint {
            get {
                return _nextJoint;
            } 
        }

/**
     * The midpoint of the bone.
     *
     * \include Bone_center.txt
     *
     * @returns The midpoint in the center of the bone.
     * @since 2.0
     */
        public Vector Center {
            get {
                return _center;
            } 
        }

/**
     * The normalized direction of the bone from base to tip.
     *
     * \include Bone_direction.txt
     *
     * @returns The normalized direction of the bone from base to tip.
     * @since 2.0
     */
        public Vector Direction {
            get {
                return _direction;
            } 
        }

/**
     * The estimated length of the bone in millimeters.
     *
     * \include Bone_length.txt
     *
     * @returns The length of the bone in millimeters.
     * @since 2.0
     */
        public float Length {
            get {
                return _length;
            } 
        }

/**
     * The average width of the flesh around the bone in millimeters.
     *
     * \include Bone_width.txt
     *
     * @returns The width of the flesh around the bone in millimeters.
     * @since 2.0
     */
        public float Width {
            get {
                return _width;
            } 
        }

/**
     * The name of this bone.
     *
     * \include Bone_type.txt
     *
     * @returns The anatomical type of this bone as a member of the Bone::Type
     * enumeration.
     * @since 2.0
     */
        public Bone.BoneType Type {
            get {
                return _type;
            } 
        }

/**
     * The orthonormal basis vectors for this Bone as a Matrix.
     *
     * Basis vectors specify the orientation of a bone.
     *
     * **xBasis** Perpendicular to the longitudinal axis of the
     *   bone; exits the sides of the finger.
     *
     * **yBasis or up vector** Perpendicular to the longitudinal
     *   axis of the bone; exits the top and bottom of the finger. More positive
     *   in the upward direction.
     *
     * **zBasis** Aligned with the longitudinal axis of the bone.
     *   More positive toward the base of the finger.
     *
     * The bases provided for the right hand use the right-hand rule; those for
     * the left hand use the left-hand rule. Thus, the positive direction of the
     * x-basis is to the right for the right hand and to the left for the left
     * hand. You can change from right-hand to left-hand rule by multiplying the
     * z basis vector by -1.
     *
     * You can use the basis vectors for such purposes as measuring complex
     * finger poses and skeletal animation.
     *
     * Note that converting the basis vectors directly into a quaternion
     * representation is not mathematically valid. If you use quaternions,
     * create them from the derived rotation matrix not directly from the bases.
     *
     * \include Bone_basis.txt
     *
     * @returns The basis of the bone as a matrix.
     * @since 2.0
     */
        public Matrix Basis {
            get {
                return _basis;
            } 
        }

/**
     * Reports whether this is a valid Bone object.
     *
     * \include Bone_isValid.txt
     *
     * @returns True, if this Bone object contains valid tracking data.
     * @since 2.0
     */
        public bool IsValid {
            get {
                return _isValid;
            } 
        }

/**
     * Returns an invalid Bone object.
     *
     * You can use the instance returned by this function in comparisons testing
     * whether a given Bone instance is valid or invalid. (You can also use the
     * Bone::isValid() function.)
     *
     * \include Bone_invalid.txt
     *
     * @returns The invalid Bone instance.
     * @since 2.0
     */
        public static Bone Invalid {
            get {
                return new Bone();
            } 
        }

        /**
       * Enumerates the names of the bones.
       *
       * Members of this enumeration are returned by Bone::type() to identify a
       * Bone object.
       * @since 2.0
       */
        public enum BoneType
        {
            TYPE_METACARPAL = 0,
            TYPE_PROXIMAL = 1,
            TYPE_INTERMEDIATE = 2,
            TYPE_DISTAL = 3
        }

    }

}
