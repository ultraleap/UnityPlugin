/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

namespace Leap {
  using System;

  /// <summary>
  /// The Bone class represents a tracked bone.
  /// 
  /// All fingers contain 4 bones that make up the anatomy of the finger.
  /// Get valid Bone objects from a Finger object.
  /// 
  /// Bones are ordered from base to tip, indexed from 0 to 3.  Additionally, the
  /// bone's Type enum may be used to index a specific bone anatomically.
  /// 
  /// The thumb does not have a base metacarpal bone and therefore contains a valid,
  /// zero length bone at that location.
  /// @since 2.0
  /// </summary>
  [Serializable]
  public class Bone : IEquatable<Bone> {

    /// <summary>
    /// Constructs a default invalid Bone object.
    /// 
    /// @since 2.0
    /// </summary>
    public Bone() {
      Type = BoneType.TYPE_INVALID;
    }

    /// <summary>
    /// Constructs a new Bone object.
    /// @since 3.0
    /// </summary>
    public Bone(Vector prevJoint,
                Vector nextJoint,
                Vector center,
                Vector direction,
                float length,
                float width,
                Bone.BoneType type,
                LeapQuaternion rotation) {
      PrevJoint = prevJoint;
      NextJoint = nextJoint;
      Center = center;
      Direction = direction;
      Rotation = rotation;
      Length = length;
      Width = width;
      Type = type;
    }

    /// <summary>
    /// Compare Bone object equality.
    /// 
    /// Two Bone objects are equal if and only if both Bone objects represent the
    /// exact same physical bone in the same frame and both Bone objects are valid.
    /// @since 2.0
    /// </summary>
    public bool Equals(Bone other) {
      return Center == other.Center && Direction == other.Direction && Length == other.Length;
    }

    /// <summary>
    /// A string containing a brief, human readable description of the Bone object.
    /// @since 2.0
    /// </summary>
    public override string ToString() {
      return Enum.GetName(typeof(BoneType), this.Type) + " bone";
    }

    /// <summary>
    /// The base of the bone, closest to the wrist.
    /// In anatomical terms, this is the proximal end of the bone.
    /// @since 2.0
    /// </summary>
    public Vector PrevJoint;

    /// <summary>
    /// The end of the bone, closest to the finger tip.
    /// In anatomical terms, this is the distal end of the bone.
    /// @since 2.0
    /// </summary>
    public Vector NextJoint;

    /// <summary>
    /// The midpoint of the bone. 
    /// @since 2.0 
    /// </summary>
    public Vector Center;

    /// <summary>
    /// The normalized direction of the bone from base to tip.
    /// @since 2.0
    /// </summary>
    public Vector Direction;

    /// <summary>
    /// The estimated length of the bone.
    /// @since 2.0
    /// </summary>
    public float Length;

    /// <summary>
    /// The average width of the flesh around the bone.
    /// @since 2.0
    /// </summary>
    public float Width;

    /// <summary>
    /// The type of this bone.
    /// @since 2.0
    /// </summary>
    public BoneType Type;

    /// <summary>
    /// The orientation of this Bone as a Quaternion.
    /// @since 2.0
    /// </summary>
    public LeapQuaternion Rotation;

    /// <summary>
    /// The orthonormal basis vectors for this Bone as a Matrix.
    /// The orientation of this Bone as a Quaternion.
    /// 
    /// Basis vectors specify the orientation of a bone.
    /// 
    /// **xBasis** Perpendicular to the longitudinal axis of the
    ///   bone; exits the sides of the finger.
    /// 
    /// **yBasis or up vector** Perpendicular to the longitudinal
    ///   axis of the bone; exits the top and bottom of the finger. More positive
    ///   in the upward direction.
    /// 
    /// **zBasis** Aligned with the longitudinal axis of the bone.
    ///   More positive toward the base of the finger.
    /// 
    /// The bases provided for the right hand use the right-hand rule; those for
    /// the left hand use the left-hand rule. Thus, the positive direction of the
    /// x-basis is to the right for the right hand and to the left for the left
    /// hand. You can change from right-hand to left-hand rule by multiplying the
    /// z basis vector by -1.
    /// 
    /// You can use the basis vectors for such purposes as measuring complex
    /// finger poses and skeletal animation.
    /// 
    /// Note that converting the basis vectors directly into a quaternion
    /// representation is not mathematically valid. If you use quaternions,
    /// create them from the derived rotation matrix not directly from the bases.
    /// 
    /// @since 2.0
    /// </summary>
    public LeapTransform Basis { get { return new LeapTransform(PrevJoint, Rotation); } }

    /// <summary>
    /// Enumerates the type of bones.
    /// 
    /// Members of this enumeration are returned by Bone.Type() to identify a
    /// Bone object.
    /// @since 2.0
    /// </summary>
    public enum BoneType {
      TYPE_INVALID = -1,
      TYPE_METACARPAL = 0,
      TYPE_PROXIMAL = 1,
      TYPE_INTERMEDIATE = 2,
      TYPE_DISTAL = 3
    }
  }
}
