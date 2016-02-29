using System;

namespace Leap
{
    /**
    * The IFinger class represents a tracked finger.
    *
    * Fingers are objects that the Leap Motion software has classified as a finger.
    * Get valid IFinger objects from a Frame or a IHand object.
    *
    * Fingers may be permanently associated to a hand. In this case the angular order of the finger IDs
    * will be invariant. As fingers move in and out of view it is possible for the guessed ID
    * of a finger to be incorrect. Consequently, it may be necessary for finger IDs to be
    * exchanged. All tracked properties, such as velocity, will remain continuous in the API.
    * However, quantities that are derived from the API output (such as a history of positions)
    * will be discontinuous unless they have a corresponding ID exchange.
    */
    public interface IFinger
    {
        /**
        * A unique ID assigned to this Finger object, whose value remains the
        * same across consecutive frames while the tracked finger or tool remains
        * visible. If tracking is lost (for example, when a finger is occluded by
        * another finger or when it is withdrawn from the Leap Motion Controller field of view), the
        * Leap Motion software may assign a new ID when it detects the entity in a future frame.
        *
        * \include Finger_id.txt
        *
        * Use the ID value with the Frame::pointable() function to find this
        * Finger object in future frames.
        *
        * IDs should be from 1 to 100 (inclusive). If more than 100 objects are tracked
        * an IDs of -1 will be used until an ID in the defined range is available.
        *
        * @returns The ID assigned to this Finger object.
        * @since 1.0
        */
        int Id { get; }

        /**
        * The Hand associated with a finger.
        *
        * \include Finger_hand.txt
        *
        * Not that in version 2+, tools are not associated with hands. For
        * tools, this function always returns an invalid Hand object.
        *
        * @returns The associated Hand object, if available; otherwise,
        * an invalid Hand object is returned.
        * @since 1.0
        */
        int HandId { get; }

        /**
        * The duration of time this Finger has been visible to the Leap Motion Controller.
        *
        * \include Finger_timeVisible.txt
        *
        * @returns The duration (in seconds) that this Finger has been tracked.
        * @since 1.0
        */
        float TimeVisible { get; }

        /**
        * The name of this finger.
        *
        * \include Finger_type.txt
        *
        * @returns The anatomical type of this finger as a member of the Finger::Type
        * enumeration.
        * @since 2.0
        */
        Finger.FingerType Type { get; }

        /**
        * Whether or not this Finger is in an extended posture.
        *
        * A finger is considered extended if it is extended straight from the hand as if
        * pointing. A finger is not extended when it is bent down and curled towards the
        * palm.  Tools are always extended.
        *
        * \include Finger_isExtended.txt
        *
        * @returns True, if the pointable is extended.
        * @since 2.0
        */
        bool IsExtended { get; }

        /**
        * The estimated length of the finger or tool in millimeters.
        *
        * \include Finger_length.txt
        *
        * @returns The estimated length of this Finger object.
        * @since 1.0
        */
        float Length { get; }

        /**
        * The estimated width of the finger or tool in millimeters.
        *
        * \include Finger_width.txt
        *
        * @returns The estimated width of this Finger object.
        * @since 1.0
        */
        float Width { get; }

        /**
        * The direction in which this finger or tool is pointing.
        *
        * \include Finger_direction.txt
        *
        * The direction is expressed as a unit vector pointing in the same
        * direction as the tip.
        *
        * \image html images/Leap_Finger_Model.png
        *
        * @returns The Vector pointing in the same direction as the tip of this
        * Finger object.
        * @since 1.0
        */
        Vector Direction { get; }

        /**
        * The stabilized tip position of this Finger.
        *
        * Smoothing and stabilization is performed in order to make
        * this value more suitable for interaction with 2D content. The stabilized
        * position lags behind the tip position by a variable amount, depending
        * primarily on the speed of movement.
        *
        * \include Finger_stabilizedTipPosition.txt
        *
        * @returns A modified tip position of this Finger object
        * with some additional smoothing and stabilization applied.
        * @since 1.0
        */
        Vector StabilizedTipPosition { get; }

        /**
        * The tip position in millimeters from the Leap Motion origin.
        *
        * \include Finger_tipPosition.txt
        *
        * @returns The Vector containing the coordinates of the tip position.
        * @since 1.0
        */
        Vector TipPosition { get; }

        /**
        * The rate of change of the tip position in millimeters/second.
        *
        * \include Finger_tipVelocity.txt
        *
        * @returns The Vector containing the coordinates of the tip velocity.
        * @since 1.0
        */
        Vector TipVelocity { get; }

        /**
        * The bone at a given bone index on this finger.
        *
        * \include Bone_iteration.txt
        *
        * @param boneIx An index value from the Bone::Type enumeration identifying the
        * bone of interest.
        * @returns The Bone that has the specified bone type.
        * @since 2.0
        */
        IBone Bone(Bone.BoneType boneIx);

        /**
        * Creates a copy of this finger, transformed by the specified transform
        * on demand.
        *
        * @param trs A Matrix containing the desired translation, rotation, and scale
        * of the copied finger.
        */
        IFinger TransformedShallowCopy(ref Matrix trs);

        /**
        * Creates a copy of this finger, transformed by the specified transform.
        *
        * @param trs A Matrix containing the desired translation, rotation, and scale
        * of the copied finger.
        * @since 3.0
        */
        IFinger TransformedCopy(ref Matrix trs);

        /**
        * A string containing a brief, human readable description of the Finger object.
        *
        * \include Finger_toString.txt
        *
        * @returns A description of the Finger object as a string.
        * @since 1.0
        */
        string ToString();
    }
}
