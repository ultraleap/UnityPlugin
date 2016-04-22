/******************************************************************************\
* Copyright (C) 2012-2016 Leap Motion, Inc. All rights reserved.               *
* Leap Motion proprietary and confidential. Not for distribution.              *
* Use subject to the terms of the Leap Motion SDK Agreement available at       *
* https://developer.leapmotion.com/sdk_agreement, or another agreement         *
* between Leap Motion and you, your company or other organization.             *
\******************************************************************************/

namespace LeapInternal
{
  using System;
  using System.Collections.Generic;
  using System.Text;
  using System.Threading;
  using System.Runtime.InteropServices;


  using Leap;

  public class ServiceFrameFactory
  {
    public static readonly int handStructSize = Marshal.SizeOf(typeof(LEAP_HAND));

    public Frame makeFrame(ref LEAP_TRACKING_EVENT trackingMsg)
    {
      Frame newFrame = new Leap.Frame((long)trackingMsg.info.frame_id,
                           (long)trackingMsg.info.timestamp,
                           trackingMsg.framerate,
                           new InteractionBox(trackingMsg.interaction_box_center.ToLeapVector(),
                               trackingMsg.interaction_box_size.ToLeapVector()),
                           new List<Hand>((int)trackingMsg.nHands)
            );

      for (int h = 0; h < trackingMsg.nHands; h++)
      {
        LEAP_HAND hand;
        StructMarshal<LEAP_HAND>.ArrayElementToStruct(trackingMsg.pHands, h, out hand);
        newFrame.Hands.Add(makeHand(ref hand, newFrame));
      }
      return newFrame;
    }

    public Hand makeHand(ref LEAP_HAND hand, Frame owningFrame)
    {
      Arm newArm = makeArm(ref hand.arm);

      Hand newHand = new Hand(
        (int)owningFrame.Id,
        (int)hand.id,
        hand.confidence,
        hand.grab_strength,
        hand.grab_angle,
        hand.pinch_strength,
        hand.pinch_distance,
        hand.palm.width,
        hand.type == eLeapHandType.eLeapHandType_Left,
        hand.visible_time,
        newArm,
        new List<Finger>(5),
        new Vector(hand.palm.position.x, hand.palm.position.y, hand.palm.position.z),
        new Vector(hand.palm.stabilized_position.x, hand.palm.stabilized_position.y, hand.palm.stabilized_position.z),
        new Vector(hand.palm.velocity.x, hand.palm.velocity.y, hand.palm.velocity.z),
        new Vector(hand.palm.normal.x, hand.palm.normal.y, hand.palm.normal.z),
        new Vector(hand.palm.direction.x, hand.palm.direction.y, hand.palm.direction.z),
        newArm.NextJoint //wrist position
      );
      newHand.Fingers.Insert(0, makeFinger(owningFrame, ref hand, ref hand.thumb, Finger.FingerType.TYPE_THUMB));
      newHand.Fingers.Insert(1, makeFinger(owningFrame, ref hand, ref hand.index, Finger.FingerType.TYPE_INDEX));
      newHand.Fingers.Insert(2, makeFinger(owningFrame, ref hand, ref hand.middle, Finger.FingerType.TYPE_MIDDLE));
      newHand.Fingers.Insert(3, makeFinger(owningFrame, ref hand, ref hand.ring, Finger.FingerType.TYPE_RING));
      newHand.Fingers.Insert(4, makeFinger(owningFrame, ref hand, ref hand.pinky, Finger.FingerType.TYPE_PINKY));

      return newHand;
    }

    public Finger makeFinger(Frame owner, ref LEAP_HAND hand, ref LEAP_DIGIT digit, Finger.FingerType type)
    {
      Bone metacarpal = makeBone(ref digit.metacarpal, Bone.BoneType.TYPE_METACARPAL);
      Bone proximal = makeBone(ref digit.proximal, Bone.BoneType.TYPE_PROXIMAL);
      Bone intermediate = makeBone(ref digit.intermediate, Bone.BoneType.TYPE_INTERMEDIATE);
      Bone distal = makeBone(ref digit.distal, Bone.BoneType.TYPE_DISTAL);
      return new Finger((int)owner.Id,
          (int)hand.id,
          (int)digit.finger_id,
          hand.visible_time,
          distal.NextJoint,
          new Vector(digit.tip_velocity.x, digit.tip_velocity.y, digit.tip_velocity.z),
          intermediate.Direction,
          new Vector(digit.stabilized_tip_position.x, digit.stabilized_tip_position.y, digit.stabilized_tip_position.z),
          intermediate.Width,
          proximal.Length + intermediate.Length + (distal.Length * 0.77f), //0.77 is used in platform code for this calculation
          digit.is_extended != 0,
          type,
          metacarpal,
          proximal,
          intermediate,
          distal
      );
    }

    public Bone makeBone(ref LEAP_BONE bone, Bone.BoneType type)
    {
      Vector prevJoint = new Vector(bone.prev_joint.x, bone.prev_joint.y, bone.prev_joint.z);
      Vector nextJoint = new Vector(bone.next_joint.x, bone.next_joint.y, bone.next_joint.z);
      Vector center = (nextJoint + prevJoint) * .5f;
      float length = (nextJoint - prevJoint).Magnitude;
      Vector direction = (nextJoint - prevJoint) / length;
      LeapQuaternion rotation = new LeapQuaternion(bone.rotation);
      return new Bone(prevJoint, nextJoint, center, direction, length, bone.width, type, rotation);
    }

    public Arm makeArm(ref LEAP_BONE bone)
    {
      Vector prevJoint = new Vector(bone.prev_joint.x, bone.prev_joint.y, bone.prev_joint.z);
      Vector nextJoint = new Vector(bone.next_joint.x, bone.next_joint.y, bone.next_joint.z);
      Vector center = (nextJoint + prevJoint) * .5f;
      float length = (nextJoint - prevJoint).Magnitude;
      Vector direction = Vector.Zero;
      if (length > 0)
        direction = (nextJoint - prevJoint) / length;
      LeapQuaternion rotation = new LeapQuaternion(bone.rotation);
      return new Arm(prevJoint, nextJoint, center, direction, length, bone.width, rotation);
    }
  }
}
