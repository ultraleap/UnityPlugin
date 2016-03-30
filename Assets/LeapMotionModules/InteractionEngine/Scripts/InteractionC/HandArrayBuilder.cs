using System;
using LeapInternal;

namespace Leap.Unity.Interaction.CApi {

  public static class HandArrayBuilder {

    public static IntPtr CreateHandArray(Frame frame) {
      var hands = frame.Hands;
      IntPtr handArray = StructAllocator.AllocateArray<LEAP_HAND>(hands.Count);
      for (int i = 0; i < hands.Count; i++) {
        StructMarshal<LEAP_HAND>.CopyIntoArray(handArray, CreateHand(hands[i]), i);
      }
      return handArray;
    }

    public static LEAP_HAND CreateHand(Hand hand) {
      LEAP_HAND leapHand = new LEAP_HAND();
      leapHand.id = (uint)hand.Id;
      leapHand.type = hand.IsLeft ? eLeapHandType.eLeapHandType_Left : eLeapHandType.eLeapHandType_Right;
      leapHand.confidence = hand.Confidence;
      leapHand.visible_time = (uint)(hand.TimeVisible * 1000);

      LEAP_PALM palm = new LEAP_PALM();
      palm.position = new LEAP_VECTOR(hand.PalmPosition);
      palm.stabilized_position = new LEAP_VECTOR(hand.StabilizedPalmPosition);
      palm.velocity = new LEAP_VECTOR(hand.PalmVelocity);
      palm.normal = new LEAP_VECTOR(hand.PalmNormal);
      palm.width = hand.PalmWidth;
      palm.direction = new LEAP_VECTOR(hand.Direction);

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
      digit.stabilized_tip_position = new LEAP_VECTOR(finger.StabilizedTipPosition);
      digit.is_extended = finger.IsExtended ? 1 : 0;
      return digit;
    }

    public static LEAP_BONE CreateBone(Bone bone) {
      LEAP_BONE leapBone = new LEAP_BONE();
      leapBone.prev_joint = new LEAP_VECTOR(bone.PrevJoint);
      leapBone.next_joint = new LEAP_VECTOR(bone.NextJoint);
      leapBone.width = bone.Width;
      leapBone.basis = new LEAP_MATRIX(bone.Basis);
      return leapBone;
    }
  }
}