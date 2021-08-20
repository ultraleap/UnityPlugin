/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

namespace Leap {

  public static class CopyFromOtherExtensions {

    /**
     * Copies the data from a source frame into a frame.  After the operation is
     * complete, the frame will be identical to the source frame.
     *
     * @param source The source frame that is copied into a frame.
     */
    public static Frame CopyFrom(this Frame frame, Frame source) {
      frame.Id = source.Id;
      frame.Timestamp = source.Timestamp;
      frame.CurrentFramesPerSecond = source.CurrentFramesPerSecond;

      frame.ResizeHandList(source.Hands.Count);

      for (int i = frame.Hands.Count; i-- != 0;) {
        frame.Hands[i].CopyFrom(source.Hands[i]);
      }

      return frame;
    }

    /*
     * Copies the data from a source hand into a hand.  After the operation is
     * complete, the hand will be identical to the source hand.
     *
     * @param source The source hand that is copied into a hand.
     */
    public static Hand CopyFrom(this Hand hand, Hand source) {
      hand.Id = source.Id;
      hand.Confidence = source.Confidence;
      hand.GrabStrength = source.GrabStrength;
      hand.GrabAngle = source.GrabAngle;
      hand.Rotation = source.Rotation;
      hand.PinchStrength = source.PinchStrength;
      hand.PinchDistance = source.PinchDistance;
      hand.PalmWidth = source.PalmWidth;
      hand.IsLeft = source.IsLeft;
      hand.TimeVisible = source.TimeVisible;
      hand.PalmPosition = source.PalmPosition;
      hand.StabilizedPalmPosition = source.StabilizedPalmPosition;
      hand.PalmVelocity = source.PalmVelocity;
      hand.PalmNormal = source.PalmNormal;
      hand.Direction = source.Direction;
      hand.WristPosition = source.WristPosition;

      hand.Arm.CopyFrom(source.Arm);

      for (int i = 5; i-- != 0;) {
        hand.Fingers[i].CopyFrom(source.Fingers[i]);
      }

      return hand;
    }

    /**
     * Copies the data from a source finger into a finger.  After the operation is
     * complete, the finger will be identical to the source finger.
     *
     * @param source The source finger that is copied into a finger.
     */
    public static Finger CopyFrom(this Finger finger, Finger source) {
      for (int i = 4; i-- != 0;) {
        finger.bones[i].CopyFrom(source.bones[i]);
      }

      finger.Id = source.Id;
      finger.HandId = source.HandId;
      finger.TimeVisible = source.TimeVisible;

      finger.TipPosition = source.TipPosition;
      finger.Direction = source.Direction;
      finger.Width = source.Width;
      finger.Length = source.Length;
      finger.IsExtended = source.IsExtended;
      finger.Type = source.Type;

      return finger;
    }

    /**
     * Copies the data from a source bone into a bone.  After the operation is
     * complete, the bone will be identical to the source bone.
     *
     * @param source The source bone that is copied into a bone.
     */
    public static Bone CopyFrom(this Bone bone, Bone source) {
      bone.PrevJoint = source.PrevJoint;
      bone.NextJoint = source.NextJoint;
      bone.Direction = source.Direction;
      bone.Center = source.Center;
      bone.Length = source.Length;
      bone.Width = source.Width;
      bone.Rotation = source.Rotation;
      bone.Type = source.Type;

      return bone;
    }
  }
}
