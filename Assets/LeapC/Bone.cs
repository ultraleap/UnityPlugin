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
   * @since 2.0
   */
  public class Bone
  {
    /**
     * Constructs a default Bone object.
     *
     * @since 2.0
     */
    public Bone()
    {
      PrevJoint = Vector.Zero;
      NextJoint = Vector.Zero;
      Basis = Matrix.Identity;
      Center = Vector.Zero;
      Direction = Vector.Zero;
      Type = BoneType.TYPE_INVALID;
    }

    /**
     * Constructs a new Bone object.
     *
     * @param prevJoint The proximal end of the bone (closest to the body)
     * @param nextJoint The distal end of the bone (furthest from the body)
     * @param center The midpoint of the bone
     * @param direction The unit direction vector pointing from prevJoint to nextJoint.
     * @param length The estimated length of the bone.
     * @param width The estimated average width of the bone.
     * @param type The type of finger bone.
     * @param basis The matrix representing the orientation of the bone.
     * @since 3.0
     */
    public Bone(Vector prevJoint,
                Vector nextJoint,
                Vector center,
                Vector direction,
                float length,
                float width,
                Bone.BoneType type,
                Matrix basis
                )
    {
      PrevJoint = prevJoint;
      NextJoint = nextJoint;
      Center = center;
      Direction = direction;
      Basis = basis;
      Length = length;
      Width = width;
      Type = type;
    }

    /**
     * Creates a copy of this bone, transformed by the specified transform.
     *
     * @param trs A Matrix containing the desired translation, rotation, and scale
     * of the copied bone.
     * @since 3.0
     */
    public Bone TransformedCopy(Matrix trs)
    {
      float dScale = trs.zBasis.Magnitude;
      float hScale = trs.xBasis.Magnitude;
      return new Bone(trs.TransformPoint(PrevJoint),
          trs.TransformPoint(NextJoint),
          trs.TransformPoint(Center),
          trs.TransformDirection(Direction).Normalized,
          Length * dScale,
          Width * hScale,
          Type,
          trs * Basis);
    }

    /**
     * Compare Bone object equality.
     *
     * Two Bone objects are equal if and only if both Bone objects represent the
     * exact same physical bone in the same frame and both Bone objects are valid.
     * @since 2.0
     */
    public bool Equals(Bone other)
    {
      return Center == other.Center && Direction == other.Direction && Length == other.Length;
    }

    /**
     * A string containing a brief, human readable description of the Bone object.
     *
     * \include Bone_toString.txt
     *
     * @returns A description of the Bone object as a string.
     * @since 2.0
     */
    public override string ToString()
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
    public Vector PrevJoint { get; private set; }

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
    public Vector NextJoint { get; private set; }

    /**
     * The midpoint of the bone.
     *
     * \include Bone_center.txt
     *
     * @returns The midpoint in the center of the bone.
     * @since 2.0
     */
    public Vector Center { get; private set; }

    /**
     * The normalized direction of the bone from base to tip.
     *
     * \include Bone_direction.txt
     *
     * @returns The normalized direction of the bone from base to tip.
     * @since 2.0
     */
    public Vector Direction { get; private set; }

    /**
     * The estimated length of the bone in millimeters.
     *
     * \include Bone_length.txt
     *
     * @returns The length of the bone in millimeters.
     * @since 2.0
     */
    public float Length { get; private set; }

    /**
     * The average width of the flesh around the bone in millimeters.
     *
     * \include Bone_width.txt
     *
     * @returns The width of the flesh around the bone in millimeters.
     * @since 2.0
     */
    public float Width { get; private set; }

    /**
     * The name of this bone.
     *
     * \include Bone_type.txt
     *
     * @returns The anatomical type of this bone as a member of the Bone::Type
     * enumeration.
     * @since 2.0
     */
    public Bone.BoneType Type { get; private set; }

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
    public Matrix Basis { get; private set; }

    /**
     * Enumerates the names of the bones.
     *
     * Members of this enumeration are returned by Bone::type() to identify a
     * Bone object.
     * @since 2.0
     */
    public enum BoneType
    {
      TYPE_INVALID = -1,
      TYPE_METACARPAL = 0,
      TYPE_PROXIMAL = 1,
      TYPE_INTERMEDIATE = 2,
      TYPE_DISTAL = 3
    }
  }
}
