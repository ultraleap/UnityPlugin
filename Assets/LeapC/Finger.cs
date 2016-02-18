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
   * The Finger class represents a tracked finger.
   *
   * Fingers are objects that the Leap Motion software has classified as a finger.
   * Get valid Finger objects from a Frame or a Hand object.
   *
   * Fingers may be permanently associated to a hand. In this case the angular order of the finger IDs
   * will be invariant. As fingers move in and out of view it is possible for the guessed ID
   * of a finger to be incorrect. Consequently, it may be necessary for finger IDs to be
   * exchanged. All tracked properties, such as velocity, will remain continuous in the API.
   * However, quantities that are derived from the API output (such as a history of positions)
   * will be discontinuous unless they have a corresponding ID exchange.
   *
   * Note that Finger objects can be invalid, which means that they do not contain
   * valid tracking data and do not correspond to a physical finger. Invalid Finger
   * objects can be the result of asking for a Finger object using an ID from an
   * earlier frame when no Finger objects with that ID exist in the current frame.
   * A Finger object created from the Finger constructor is also invalid.
   * Test for validity with the Finger::isValid() function.
   * @since 1.0
   */

    public class Finger
    {
        Bone[] _bones = new Bone[4];
        FingerType _type = FingerType.TYPE_UNKNOWN;
         int _frameId;
         int _id = 0;
         int _handID = 0;
         Vector _tipPosition;
         Vector _tipVelocity;
         Vector _direction;
         float _width = 0;
         float _length = 0;
         bool _isExtended = false;
         bool _isValid = false;
         Vector _stabilizedTipPosition;
         float _timeVisible = 0;

        /**
     * Constructs a Finger object.
     *
     * An uninitialized finger is considered invalid.
     * Get valid Finger objects from a Frame or a Hand object.
     * @since 1.0
     */
        public Finger ()
        {
        }

        public Finger (int frameId,
                           int handId, 
                           int fingerId,
                           float timeVisible,
                           Vector tipPosition,
                           Vector tipVelocity,
                           Vector direction,
                           Vector stabilizedTipPosition,
                           float width,
                           float length,
                           bool isExtended,
                           Finger.FingerType type,
                           Bone metacarpal,
                           Bone proximal,
                           Bone intermediate,
                           Bone distal)
        {
            _type = type;
            _bones [0] = metacarpal;
            _bones [1] = proximal;
            _bones [2] = intermediate;
            _bones [3] = distal;
            _frameId = frameId;
            _id = (handId * 10) + fingerId;
            _handID = handId;
            _tipPosition = tipPosition;
            _tipVelocity = tipVelocity;
            _direction = direction;
            _width = width;
            _length = length;
            _isExtended = isExtended;
            _isValid = false;
            _stabilizedTipPosition = stabilizedTipPosition;
            _timeVisible = timeVisible;
        }

        public Finger TransformedCopy(Matrix trs){
            float dScale = trs.zBasis.Magnitude;
            float hScale = trs.xBasis.Magnitude;
            return new Finger(_frameId,
                              _handID,
                              _id,
                              _timeVisible,
                              trs.TransformPoint(_tipPosition),
                              trs.TransformPoint(_tipVelocity),
                              trs.TransformDirection(_direction).Normalized,
                              trs.TransformPoint(_stabilizedTipPosition),
                              _width * hScale,
                              _length * dScale,
                              _isExtended,
                              _type,
                              _bones[0].TransformedCopy(trs),
                              _bones[1].TransformedCopy(trs),
                              _bones[2].TransformedCopy(trs),
                              _bones[3].TransformedCopy(trs));
        }


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
        public Bone Bone (Bone.BoneType boneIx)
        {
            return _bones[(int)boneIx];
        }

        /**
     * A string containing a brief, human readable description of the Finger object.
     *
     * \include Finger_toString.txt
     *
     * @returns A description of the Finger object as a string.
     * @since 1.0
     */
        public override string ToString ()
        {
            return Enum.GetName(typeof(FingerType), Type) + " id:" + Id;
        }

/**
     * The name of this finger.
     *
     * \include Finger_type.txt
     *
     * @returns The anatomical type of this finger as a member of the Finger::Type
     * enumeration.
     * @since 2.0
     */
        public Finger.FingerType Type {
            get {
                return _type;
            } 
        }

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
        public int Id {
            get {
                return _id;
            } 
        }

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
        public int HandId{
            get{
                return _handID;
            }
        }
        /**
     * The tip position in millimeters from the Leap Motion origin.
     *
     * \include Finger_tipPosition.txt
     *
     * @returns The Vector containing the coordinates of the tip position.
     * @since 1.0
     */
        public Vector TipPosition {
            get {
                return _tipPosition;
            } 
        }

        /**
     * The rate of change of the tip position in millimeters/second.
     *
     * \include Finger_tipVelocity.txt
     *
     * @returns The Vector containing the coordinates of the tip velocity.
     * @since 1.0
     */
        public Vector TipVelocity {
            get {
                return _tipVelocity;
            } 
        }

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
        public Vector Direction {
            get {
                return _direction;
            } 
        }

        /**
     * The estimated width of the finger or tool in millimeters.
     *
     * \include Finger_width.txt
     *
     * @returns The estimated width of this Finger object.
     * @since 1.0
     */
        public float Width {
            get {
                return _width;
            } 
        }

        /**
     * The estimated length of the finger or tool in millimeters.
     *
     * \include Finger_length.txt
     *
     * @returns The estimated length of this Finger object.
     * @since 1.0
     */
        public float Length {
            get {
                return _length;
            } 
        }
            
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
        public bool IsExtended {
            get {
                return _isExtended;
            } 
        }

        /**
     * Reports whether this is a valid Finger object.
     *
     * \include Finger_isValid.txt
     *
     * @returns True, if this Finger object contains valid tracking data.
     * @since 1.0
     */
        public bool IsValid {
            get {
                return _isValid;
            } 
        }

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
        public Vector StabilizedTipPosition {
            get {
                return _stabilizedTipPosition;
            } 
        }

        /**
     * The duration of time this Finger has been visible to the Leap Motion Controller.
     *
     * \include Finger_timeVisible.txt
     *
     * @returns The duration (in seconds) that this Finger has been tracked.
     * @since 1.0
     */
        public float TimeVisible {
            get {
                return _timeVisible;
            } 
        }

/**
     * Returns an invalid Finger object.
     *
     * You can use the instance returned by this function in comparisons testing
     * whether a given Finger instance is valid or invalid. (You can also use the
     * Finger::isValid() function.)
     *
     * \include Finger_invalid.txt
     *
     * @returns The invalid Finger instance.
     * @since 1.0
     */
        public static Finger Invalid {
            get {
                return new Finger ();
            } 
        }
            
        /**
       * Enumerates the names of the fingers.
       *
       * Members of this enumeration are returned by Finger::type() to identify a
       * Finger object.
       * @since 2.0
       */
        public enum FingerType
        {
            TYPE_THUMB = 0,
            TYPE_INDEX = 1,
            TYPE_MIDDLE = 2,
            TYPE_RING = 3,
            /** The pinky or little finger  */
            TYPE_PINKY = 4,
            TYPE_UNKNOWN = -1
        }

        /**
     * Deprecated as of version 2.0
     * Use 'bone' method instead.
     */
        public Vector JointPosition (Finger.FingerJoint jointIx)
        {
            switch (jointIx){
            case FingerJoint.JOINT_MCP:
                return _bones[0].NextJoint;
            case FingerJoint.JOINT_PIP:
                return _bones[1].NextJoint;
            case FingerJoint.JOINT_DIP:
                return _bones[2].NextJoint;
            case FingerJoint.JOINT_TIP:
                return _bones[3].NextJoint;
            }
            return Vector.Zero;
        }

        /**
       * Deprecated as of version 2.0
       */
        public enum FingerJoint
        {
            JOINT_MCP = 0,
            JOINT_PIP = 1,
            JOINT_DIP = 2,
            JOINT_TIP = 3
        }


    }

}
