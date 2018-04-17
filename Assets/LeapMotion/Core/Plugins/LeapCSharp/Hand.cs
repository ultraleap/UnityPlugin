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
  using System.Collections.Generic;

  /// <summary>
  /// The Hand class reports the physical characteristics of a detected hand.
  /// 
  /// Hand tracking data includes a palm position and velocity; vectors for
  /// the palm normal and direction to the fingers; and lists of the attached fingers.
  /// 
  /// Note that Hand objects can be invalid, which means that they do not contain
  /// valid tracking data and do not correspond to a physical entity. Invalid Hand
  /// objects can be the result of using the default constructor, or modifying the
  /// hand data in an incorrect way.
  /// @since 1.0
  /// </summary>
  [Serializable]
  public class Hand : IEquatable<Hand> {

    /// <summary>
    /// Constructs a Hand object.
    /// 
    /// An uninitialized hand is considered invalid.
    /// Get valid Hand objects from a Frame object.
    /// 
    /// @since 1.0
    /// </summary>
    public Hand() {
      Arm = new Arm();
      Fingers = new List<Finger>(5);
      Fingers.Add(new Finger());
      Fingers.Add(new Finger());
      Fingers.Add(new Finger());
      Fingers.Add(new Finger());
      Fingers.Add(new Finger());
    }

    /// <summary>
    /// Constructs a hand.
    /// 
    /// Generally, you should not create your own Hand objects. Such objects will not
    /// have valid tracking data. Get valid Hand objects from a frame
    /// received from the service.
    /// @since 3.0
    /// </summary>
    public Hand(long frameID,
                int id,
                float confidence,
                float grabStrength,
                float grabAngle,
                float pinchStrength,
                float pinchDistance,
                float palmWidth,
                bool isLeft,
                float timeVisible,
                Arm arm,
                List<Finger> fingers,
                Vector palmPosition,
                Vector stabilizedPalmPosition,
                Vector palmVelocity,
                Vector palmNormal,
                LeapQuaternion palmOrientation,
                Vector direction,
                Vector wristPosition) {
      FrameId = frameID;
      Id = id;
      Confidence = confidence;
      GrabStrength = grabStrength;
      GrabAngle = grabAngle;
      PinchStrength = pinchStrength;
      PinchDistance = pinchDistance;
      PalmWidth = palmWidth;
      IsLeft = isLeft;
      TimeVisible = timeVisible;
      Arm = arm;
      Fingers = fingers;
      PalmPosition = palmPosition;
      StabilizedPalmPosition = stabilizedPalmPosition;
      PalmVelocity = palmVelocity;
      PalmNormal = palmNormal;
      Rotation = palmOrientation;
      Direction = direction;
      WristPosition = wristPosition;
    }

    /// <summary>
    /// The Finger object with the specified ID attached to this hand.
    /// 
    /// Use the Hand.Finger() function to retrieve a Finger object attached to
    /// this hand using an ID value obtained from a previous frame.
    /// 
    /// Note that ID values persist across frames, but only until tracking of a
    /// particular object is lost. If tracking of a hand is lost and subsequently
    /// regained, the new Finger object representing that finger may have a
    /// different ID than that representing the finger in an earlier frame.
    /// 
    /// @since 1.0
    /// </summary>
    public Finger Finger(int id) {
      for (int i = Fingers.Count; i-- != 0;) {
        if (Fingers[i].Id == id) {
          return Fingers[i];
        }
      }
      return null;
    }

    /// <summary>
    /// Compare Hand object equality.
    /// 
    /// Two Hand objects are equal if and only if both Hand objects represent the
    /// exact same physical hand in the same frame and both Hand objects are valid.
    /// </summary>
    public bool Equals(Hand other) {
      return Id == other.Id && FrameId == other.FrameId;
    }

    /// <summary>
    /// A string containing a brief, human readable description of the Hand object.
    /// @since 1.0
    /// </summary>
    public override string ToString() {
      return string.Format(
        "Hand {0} {1}.",
        this.Id,
        this.IsLeft ? "left" : "right"
      );
    }

    public long FrameId;

    /// <summary>
    /// A unique ID assigned to this Hand object, whose value remains the same
    /// across consecutive frames while the tracked hand remains visible. If
    /// tracking is lost (for example, when a hand is occluded by another hand
    /// or when it is withdrawn from or reaches the edge of the Leap Motion Controller field of view),
    /// the Leap Motion software may assign a new ID when it detects the hand in a future frame.
    /// 
    /// Use the ID value with the Frame.Hand() function to find this Hand object
    /// in future frames.
    /// 
    /// @since 1.0
    /// </summary>
    public int Id;

    /// <summary>
    /// The list of Finger objects detected in this frame that are attached to
    /// this hand, given in order from thumb to pinky.  The list cannot be empty.
    /// @since 1.0
    /// </summary>
    public List<Finger> Fingers;

    /// <summary>
    /// The center position of the palm.
    /// @since 1.0
    /// </summary>
    public Vector PalmPosition;

    /// <summary>
    /// The rate of change of the palm position.
    /// @since 1.0
    /// </summary>
    public Vector PalmVelocity;

    /// <summary>
    /// The normal vector to the palm. If your hand is flat, this vector will
    /// point downward, or "out" of the front surface of your palm.
    /// 
    /// The direction is expressed as a unit vector pointing in the same
    /// direction as the palm normal (that is, a vector orthogonal to the palm).
    /// 
    /// You can use the palm normal vector to compute the roll angle of the palm with
    /// respect to the horizontal plane.
    /// @since 1.0
    /// </summary>
    public Vector PalmNormal;

    /// <summary>
    /// The direction from the palm position toward the fingers.
    /// 
    /// The direction is expressed as a unit vector pointing in the same
    /// direction as the directed line from the palm position to the fingers.
    /// 
    /// You can use the palm direction vector to compute the pitch and yaw angles of the palm with
    /// respect to the horizontal plane.
    /// @since 1.0
    /// </summary>
    public Vector Direction;

    /// <summary>
    /// The transform of the hand.
    /// 
    /// Note, in version prior to 3.1, the Basis was a Matrix object.
    /// @since 3.1
    /// </summary>
    public LeapTransform Basis { get { return new LeapTransform(PalmPosition, Rotation); } }

    /// <summary>
    /// The rotation of the hand as a quaternion.
    /// 
    /// @since 3.1
    /// </summary>
    public LeapQuaternion Rotation;

    /// <summary>
    /// The strength of a grab hand pose.
    /// 
    /// The strength is zero for an open hand, and blends to one when a grabbing hand
    /// pose is recognized.
    /// @since 2.0
    /// </summary>
    public float GrabStrength;

    /// <summary>
    /// The angle between the fingers and the hand of a grab hand pose.
    /// 
    /// The angle is computed by looking at the angle between the direction of the
    /// 4 fingers and the direction of the hand. Thumb is not considered when
    /// computing the angle.
    /// The angle is 0 radian for an open hand, and reaches pi radians when the pose
    /// is a tight fist.
    /// 
    /// @since 3.0
    /// </summary>
    public float GrabAngle;

    /// <summary>
    /// The holding strength of a pinch hand pose.
    /// 
    /// The strength is zero for an open hand, and blends to one when a pinching
    /// hand pose is recognized. Pinching can be done between the thumb
    /// and any other finger of the same hand.
    /// @since 2.0
    /// </summary>
    public float PinchStrength;

    /// <summary>
    /// The distance between the thumb and index finger of a pinch hand pose.
    /// 
    /// The distance is computed by looking at the shortest distance between
    /// the last 2 phalanges of the thumb and those of the index finger.
    /// This pinch measurement only takes thumb and index finger into account.
    /// @since 3.0
    /// </summary>
    public float PinchDistance;

    /// <summary>
    /// The estimated width of the palm when the hand is in a flat position.
    /// @since 2.0
    /// </summary>
    public float PalmWidth;

    /// <summary>
    /// The stabilized palm position of this Hand.
    /// 
    /// Smoothing and stabilization is performed in order to make
    /// this value more suitable for interaction with 2D content. The stabilized
    /// position lags behind the palm position by a variable amount, depending
    /// primarily on the speed of movement.
    /// @since 1.0
    /// </summary>
    public Vector StabilizedPalmPosition;

    /// <summary>
    /// The position of the wrist of this hand.
    /// @since 2.0.3
    /// </summary>
    public Vector WristPosition;

    /// <summary>
    /// The duration of time this Hand has been visible to the Leap Motion Controller.
    /// @since 1.0
    /// </summary>
    public float TimeVisible;

    /// <summary>
    /// How confident we are with a given hand pose.
    /// The confidence level ranges between 0.0 and 1.0 inclusive.
    /// 
    /// @since 2.0
    /// </summary>
    public float Confidence;

    /// <summary>
    /// Identifies whether this Hand is a left hand.
    /// @since 2.0
    /// </summary>
    public bool IsLeft;

    /// <summary>
    /// Identifies whether this Hand is a right hand.
    /// @since 2.0
    /// </summary>
    public bool IsRight { get { return !IsLeft; } }

    /// <summary>
    /// The arm to which this hand is attached.
    /// 
    /// If the arm is not completely in view, Arm attributes are estimated based on
    /// the attributes of entities that are in view combined with typical human anatomy.
    /// @since 2.0.3
    /// </summary>
    public Arm Arm;
  }
}
