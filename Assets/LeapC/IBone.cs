using System;

namespace Leap
{
   /**
   * The IBone interface represents a tracked bone.
   *
   * All fingers contain 4 bones that make up the anatomy of the finger.
   * Get valid Bone objects from a IFinger object.
   *
   * Bones are ordered from base to tip, indexed from 0 to 3.  Additionally, the
   * bone's Type enum may be used to index a specific bone anatomically.
   *
   * \include Bone_iteration.txt
   *
   * The thumb does not have a base metacarpal bone and therefore contains a valid,
   * zero length bone at that location.
   */
    public interface IBone
    {
        /**
        * The name of this bone.
        *
        * \include Bone_type.txt
        *
        * @returns The anatomical type of this bone as a member of the Bone::Type
        * enumeration.
        * @since 2.0
        */
        Bone.BoneType Type { get; }

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
        Matrix Basis { get; }

        /**
        * The midpoint of the bone.
        *
        * \include Bone_center.txt
        *
        * @returns The midpoint in the center of the bone.
        * @since 2.0
        */
        Vector Center { get; }

        /**
        * The normalized direction of the bone from base to tip.
        *
        * \include Bone_direction.txt
        *
        * @returns The normalized direction of the bone from base to tip.
        * @since 2.0
        */
        Vector Direction { get; }

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
        Vector NextJoint { get; }

        /**
        * The base of the bone, closest to the wrist.
        *
        * In anatomical terms, this is the proximal end of the bone.
        * \include Bone_prevJoint.txt
        *
        * @returns The Vector containing the coordinates of the previous joint position.
        * @since 2.0
        */
        Vector PrevJoint { get; }

        /**
        * The average width of the flesh around the bone in millimeters.
        *
        * \include Bone_width.txt
        *
        * @returns The width of the flesh around the bone in millimeters.
        * @since 2.0
        */
        float Width { get; }

        /**
        * The estimated length of the bone in millimeters.
        *
        * \include Bone_length.txt
        *
        * @returns The length of the bone in millimeters.
        * @since 2.0
        */
        float Length { get; }

        /**
        * Creates a shallow copy of this bone, transformed by the specified 
        * transform on demand.
        *
        * @param trs A Matrix containing the desired translation, rotation, and scale
        * of the copied bone.
        */
        IBone TransformedShallowCopy(ref Matrix trs);

        /**
        * Creates a copy of this bone, transformed by the specified transform.
        *
        * @param trs A Matrix containing the desired translation, rotation, and scale
        * of the copied bone.
        * @since 3.0
        */
        IBone TransformedCopy(ref Matrix trs);

        /**
        * Compare Bone object equality.
        *
        * Two Bone objects are equal if and only if both Bone objects represent the
        * exact same physical bone in the same frame and both Bone objects are valid.
        * @since 2.0
        */
        bool Equals(IBone other);

        /**
        * A string containing a brief, human readable description of the Bone object.
        *
        * \include Bone_toString.txt
        *
        * @returns A description of the Bone object as a string.
        * @since 2.0
        */
        string ToString();
    }
}
