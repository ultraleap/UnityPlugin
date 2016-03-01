using System;
namespace Leap
{
    /**
    * The IArm interface represents the forearm.
    */
    public interface IArm : IBone
    {
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
        Vector ElbowPosition { get; }

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
        Vector WristPosition { get; }

        /**
        * Creates a copy of this arm, transformed by the specified transform
        * on demand.
        *
        * @param trs A Matrix containing the desired translation, rotation, and scale
        * of the copied arm.
        */
        new IArm TransformedShallowCopy(ref Matrix trs);

        /**
        * Creates a copy of this arm, transformed by the specified transform.
        *
        * @param trs A Matrix containing the desired translation, rotation, and scale
        * of the copied arm.
        * @since 3.0
        */
        new IArm TransformedCopy(ref Matrix trs);

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
        bool Equals(IArm other);

        /**
        * A string containing a brief, human readable description of the Arm object.
        *
        * \include Arm_toString.txt
        *
        * @returns A description of the Arm object as a string.
        * @since 2.0.3
        */
        new string ToString();
    }
}
