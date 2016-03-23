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
        LEAP_HAND hand = StructMarshal<LEAP_HAND>.ArrayElementToStruct(trackingMsg.pHands, h);
        newFrame.Hands.Add(makeHand(ref hand, newFrame));
      }
      return newFrame;
    }

    public Hand makeHand(ref LEAP_HAND hand, Frame owningFrame)
    {
      LEAP_BONE arm = StructMarshal<LEAP_BONE>.PtrToStruct(hand.arm);
      Arm newArm = makeArm(ref arm);
      LEAP_PALM palm = StructMarshal<LEAP_PALM>.PtrToStruct(hand.palm);

      Hand newHand = new Hand(
        (int)owningFrame.Id,
        (int)hand.id,
        hand.confidence,
        hand.grab_strength,
        hand.grab_angle,
        hand.pinch_strength,
        hand.pinch_distance,
        palm.width,
        hand.type == eLeapHandType.eLeapHandType_Left,
        hand.visible_time,
        newArm,
        new List<Finger>(5),
        new Vector(palm.position.x, palm.position.y, palm.position.z),
        new Vector(palm.stabilized_position.x, palm.stabilized_position.y, palm.stabilized_position.z),
        new Vector(palm.velocity.x, palm.velocity.y, palm.velocity.z),
        new Vector(palm.normal.x, palm.normal.y, palm.normal.z),
        new Vector(palm.direction.x, palm.direction.y, palm.direction.z),
        newArm.NextJoint //wrist position
      );
      LEAP_DIGIT thumbDigit = StructMarshal<LEAP_DIGIT>.PtrToStruct(hand.thumb);
      newHand.Fingers.Insert(0, makeFinger(owningFrame, ref hand, ref thumbDigit, Finger.FingerType.TYPE_THUMB));

      LEAP_DIGIT indexDigit = StructMarshal<LEAP_DIGIT>.PtrToStruct(hand.index);
      newHand.Fingers.Insert(1, makeFinger(owningFrame, ref hand, ref indexDigit, Finger.FingerType.TYPE_INDEX));

      LEAP_DIGIT middleDigit = StructMarshal<LEAP_DIGIT>.PtrToStruct(hand.middle);
      newHand.Fingers.Insert(2, makeFinger(owningFrame, ref hand, ref middleDigit, Finger.FingerType.TYPE_MIDDLE));

      LEAP_DIGIT ringDigit = StructMarshal<LEAP_DIGIT>.PtrToStruct(hand.ring);
      newHand.Fingers.Insert(3, makeFinger(owningFrame, ref hand, ref ringDigit, Finger.FingerType.TYPE_RING));

      LEAP_DIGIT pinkyDigit = StructMarshal<LEAP_DIGIT>.PtrToStruct(hand.pinky);
      newHand.Fingers.Insert(4, makeFinger(owningFrame, ref hand, ref pinkyDigit, Finger.FingerType.TYPE_PINKY));

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
      Matrix basis = new Matrix(bone.basis.x_basis.x,
                         bone.basis.x_basis.y,
                         bone.basis.x_basis.z,
                         bone.basis.y_basis.x,
                         bone.basis.y_basis.y,
                         bone.basis.y_basis.z,
                         bone.basis.z_basis.x,
                         bone.basis.z_basis.y,
                         bone.basis.z_basis.z);
      return new Bone(prevJoint, nextJoint, center, direction, length, bone.width, type, basis);
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
      Matrix basis = new Matrix(bone.basis.x_basis.x,
                         bone.basis.x_basis.y,
                         bone.basis.x_basis.z,
                         bone.basis.y_basis.x,
                         bone.basis.y_basis.y,
                         bone.basis.y_basis.z,
                         bone.basis.z_basis.x,
                         bone.basis.z_basis.y,
                         bone.basis.z_basis.z);
      return new Arm(prevJoint, nextJoint, center, direction, length, bone.width, basis);
    }
  }
}
