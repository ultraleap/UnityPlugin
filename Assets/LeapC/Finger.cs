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
   * @since 1.0
   */
  public class Finger
  {
    Bone[] _bones = new Bone[4];
    long _frameId = -1;

    /**
     * Constructs a Finger object.
     *
     * @since 1.0
     */
    public Finger() {}

    /**
     * Constructs a finger.
     *
     * Generally, you should not create your own finger objects. Such objects will not
     * have valid tracking data. Get valid finger objects from a hand in a frame
     * received from the service.
     *
     * @param frameId The id of the frame this finger appears in.
     * @param handId The id of the hand this finger belongs to.
     * @param fingerId The id of this finger (handId + 0-4 for finger position).
     * @param timeVisible How long this instance of the finger has been visible.
     * @param tipPosition The position of the finger tip.
     * @param tipVelocity The velocity of the finger tip.
     * @param direction The pointing direction of the finger.
     * @param stabilizedTipPosition The stabilized tip position.
     * @param width The average width of the finger.
     * @param length The length of the finger.
     * @param isExtended Whether the finger is more-or-less straight.
     * @param type The finger name.
     * @param metacarpal The first bone of the finger (inside the hand).
     * @param proximal The second bone of the finger
     * @param intermediate The third bone of the finger.
     * @param distal The end bone.
     * @since 3.0
     */
    public Finger(long frameId,
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
      Type = type;
      _bones[0] = metacarpal;
      _bones[1] = proximal;
      _bones[2] = intermediate;
      _bones[3] = distal;
      _frameId = frameId;
      Id = (handId * 10) + fingerId;
      HandId = handId;
      TipPosition = tipPosition;
      TipVelocity = tipVelocity;
      Direction = direction;
      Width = width;
      Length = length;
      IsExtended = isExtended;
      StabilizedTipPosition = stabilizedTipPosition;
      TimeVisible = timeVisible;
    }

    /**
     * Creates a copy of this finger, transformed by the specified transform.
     *
     * @param trs A LeapTransform containing the desired translation, rotation, and scale
     * of the copied finger.
     * @since 3.0
     */
    public Finger TransformedCopy(LeapTransform trs)
    {
      return new Finger(_frameId,
                        HandId,
                        Id % 10, //remove hand portion of finger Id
                        TimeVisible,
                        trs.TransformPoint(TipPosition),
                        trs.TransformVelocity(TipVelocity),
                        trs.TransformDirection(Direction),
                        trs.TransformPoint(StabilizedTipPosition),
                        Width * trs.scale.x,
                        Length * trs.scale.z,
                        IsExtended,
                        Type,
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
    public Bone Bone(Bone.BoneType boneIx)
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
    public override string ToString()
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
    public Finger.FingerType Type { get; private set; }

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
    public int Id { get; private set; }

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
    public int HandId { get; private set; }

    /**
     * The tip position in millimeters from the Leap Motion origin.
     *
     * \include Finger_tipPosition.txt
     *
     * @returns The Vector containing the coordinates of the tip position.
     * @since 1.0
     */
    public Vector TipPosition { get; private set; }

    /**
     * The rate of change of the tip position in millimeters/second.
     *
     * \include Finger_tipVelocity.txt
     *
     * @returns The Vector containing the coordinates of the tip velocity.
     * @since 1.0
     */
    public Vector TipVelocity { get; private set; }

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
    public Vector Direction { get; private set; }

    /**
     * The estimated width of the finger or tool in millimeters.
     *
     * \include Finger_width.txt
     *
     * @returns The estimated width of this Finger object.
     * @since 1.0
     */
    public float Width { get; private set; }

    /**
     * The estimated length of the finger or tool in millimeters.
     *
     * \include Finger_length.txt
     *
     * @returns The estimated length of this Finger object.
     * @since 1.0
     */
    public float Length { get; private set; }

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
    public bool IsExtended { get; private set; }

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
    public Vector StabilizedTipPosition { get; private set; }

    /**
     * The duration of time this Finger has been visible to the Leap Motion Controller.
     *
     * \include Finger_timeVisible.txt
     *
     * @returns The duration (in seconds) that this Finger has been tracked.
     * @since 1.0
     */
    public float TimeVisible { get; private set; }

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
  }
}
