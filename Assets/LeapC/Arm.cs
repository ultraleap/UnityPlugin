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
  using System.Runtime.InteropServices;

  /**
   * The Arm class represents the forearm.
   *
   */
  public class Arm : Bone
  {
    /**
     * Constructs a default Arm object.
     *
     * Get valid Arm objects from a Hand object.
     *
     * \include Arm_get.txt
     *
     * @since 2.0.3
     */
    public Arm() {}

    /**
     * Constructs a new Arm object.
     *
     * @param elbow The position of the elbow.
     * @param wrist The position of the wrist.
     * @param center The position of the midpoint between the elbow and wrist.
     * @param direction The unit direction vector from elbow to wrist.
     * @param length The distance between elbow and wrist in millimeters.
     * @param width The estimated average width of the arm.
     * @param basis The basis matrix representing the orientation of the arm.
     * @since 3.0
     */
    public Arm(Vector elbow,
                Vector wrist,
                Vector center,
                Vector direction,
                float length,
                float width,
                Matrix basis
              ) : base(elbow,
                        wrist,
                        center,
                        direction,
                        length,
                        width,
                        BoneType.TYPE_METACARPAL, //ignored for arms
                        basis)
    { }

    /**
     * Creates a copy of this arm, transformed by the specified transform.
     *
     * @param trs A Matrix containing the desired translation, rotation, and scale
     * of the copied arm.
     * @since 3.0
     */
    public new Arm TransformedCopy(Matrix trs)
    {
      float dScale = trs.zBasis.Magnitude;
      float hScale = trs.xBasis.Magnitude;
      return new Arm(trs.TransformPoint(PrevJoint),
          trs.TransformPoint(NextJoint),
          trs.TransformPoint(Center),
          trs.TransformDirection(Direction),
          Length * dScale,
          Width * hScale,
          trs * Basis);
    }

    /**
     * Compare Arm object equality.
     *
     * \include Arm_operator_equals.txt
     *
     * Two Arm objects are equal if and only if both Arm objects represent the
     *
     * exact same physical arm in the same frame and both Arm objects are valid.
     * @since 2.0.3
     */
    public bool Equals(Arm other)
    {
      return base.Equals(other as Bone);
    }

    /**
     * A string containing a brief, human readable description of the Arm object.
     *
     * \include Arm_toString.txt
     *
     * @returns A description of the Arm object as a string.
     * @since 2.0.3
     */
    public override string ToString()
    {
      return "Arm";
    }

    /**
     * The position of the elbow.
     *
     * \include Arm_elbowPosition.txt
     *
     * If not in view, the elbow position is estimated based on typical human
     * anatomical proportions.
     *
     * @since 2.0.3
     */
    public Vector ElbowPosition
    {
      get
      {
        return base.PrevJoint;
      }
    }

    /**
     * The position of the wrist.
     *
     * \include Arm_wristPosition.txt
     *
     * Note that the wrist position is not collocated with the end of any bone in
     * the hand. There is a gap of a few centimeters since the carpal bones are
     * not included in the skeleton model.
     *
     * @since 2.0.3
     */
    public Vector WristPosition
    {
      get
      {
        return base.NextJoint;
      }
    }
  }
}
