/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;
using LeapInternal;

namespace Leap.Unity.Interaction {

  public static class HandArrayBuilder {
    private static HashSet<int> _ids = new HashSet<int>();

    public static IntPtr CreateHandArray(Frame frame) {
      _ids.Clear();

      var hands = frame.Hands;
      IntPtr handArray = StructAllocator.AllocateArray<LEAP_HAND>(hands.Count);
      for (int i = 0; i < hands.Count; i++) {
        if (_ids.Contains(hands[i].Id)) {
          Debug.LogWarning("Found multiple hands with the same id");
          continue;
        }

        LEAP_HAND hand = CreateHand(hands[i]);
        StructMarshal<LEAP_HAND>.CopyIntoArray(handArray, ref hand, i);
        _ids.Add(hands[i].Id);
      }

      return handArray;
    }

    public static LEAP_HAND CreateHand(Hand hand) {
      LEAP_HAND leapHand = new LEAP_HAND();
      leapHand.id = (uint)hand.Id;
      leapHand.flags = 0;
      leapHand.type = hand.IsLeft ? eLeapHandType.eLeapHandType_Right : eLeapHandType.eLeapHandType_Left; //HACK: flip due to coordinate space handedness
      leapHand.confidence = hand.Confidence;
      leapHand.visible_time = (uint)(hand.TimeVisible * 1000);
      leapHand.grab_angle = hand.GrabAngle;
      leapHand.grab_strength = hand.GrabStrength;
      leapHand.pinch_distance = hand.PinchDistance;
      leapHand.pinch_strength = hand.PinchStrength;

      LEAP_PALM palm = new LEAP_PALM();
      palm.position = new LEAP_VECTOR(hand.PalmPosition);
      palm.stabilized_position = Vector3.zero.ToCVector(); //HACK: stabilized position is sometimes NaN, ignore it
      palm.velocity = new LEAP_VECTOR(hand.PalmVelocity);
      palm.normal = new LEAP_VECTOR(hand.PalmNormal);
      palm.width = hand.PalmWidth;
      palm.direction = new LEAP_VECTOR(hand.Direction);
      palm.orientation = new LEAP_QUATERNION(hand.Rotation);

      leapHand.palm = palm;
      leapHand.arm = CreateBone(hand.Arm);

      for (int i = 0; i < hand.Fingers.Count; i++) {
        Finger finger = hand.Fingers[i];
        switch (finger.Type) {
          case Finger.FingerType.TYPE_THUMB:
            leapHand.thumb = CreateDigit(finger);
            break;
          case Finger.FingerType.TYPE_INDEX:
            leapHand.index = CreateDigit(finger);
            break;
          case Finger.FingerType.TYPE_MIDDLE:
            leapHand.middle = CreateDigit(finger);
            break;
          case Finger.FingerType.TYPE_RING:
            leapHand.ring = CreateDigit(finger);
            break;
          case Finger.FingerType.TYPE_PINKY:
            leapHand.pinky = CreateDigit(finger);
            break;
          default:
            throw new Exception("Unexpected Finger Type " + finger.Type);
        }
      }

      return leapHand;
    }

    public static LEAP_DIGIT CreateDigit(Finger finger) {
      LEAP_DIGIT digit = new LEAP_DIGIT();
      digit.finger_id = finger.Id;
      digit.metacarpal = CreateBone(finger.Bone(Bone.BoneType.TYPE_METACARPAL));
      digit.proximal = CreateBone(finger.Bone(Bone.BoneType.TYPE_PROXIMAL));
      digit.intermediate = CreateBone(finger.Bone(Bone.BoneType.TYPE_INTERMEDIATE));
      digit.distal = CreateBone(finger.Bone(Bone.BoneType.TYPE_DISTAL));
      digit.tip_velocity = new LEAP_VECTOR(finger.TipVelocity);
      digit.stabilized_tip_position = Vector3.zero.ToCVector(); //HACK: stabilized position is sometimes NaN, ignore it
      digit.is_extended = finger.IsExtended ? 1 : 0;
      return digit;
    }

    public static LEAP_BONE CreateBone(Bone bone) {
      LEAP_BONE leapBone = new LEAP_BONE();
      leapBone.prev_joint = new LEAP_VECTOR(bone.PrevJoint);
      leapBone.next_joint = new LEAP_VECTOR(bone.NextJoint);
      leapBone.width = bone.Width;
      leapBone.rotation = new LEAP_QUATERNION(bone.Rotation);
      return leapBone;
    }
  }
}
