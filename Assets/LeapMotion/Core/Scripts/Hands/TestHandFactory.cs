/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

namespace Leap {

  using System;
  using System.Collections.Generic;
  using Leap.Unity;
  using UnityEngine;

  public class TestHandFactory {

    public static Frame MakeTestFrame(int frameId, bool leftHandIncluded, bool rightHandIncluded) {
      Frame testFrame = new Frame(frameId, 0, 120.0f, new InteractionBox(), new List<Hand>());
      if (leftHandIncluded)
        testFrame.Hands.Add(MakeTestHand(frameId, 10, true));
      if (rightHandIncluded)
        testFrame.Hands.Add(MakeTestHand(frameId, 20, false));
      return testFrame;
    }

    public static Hand MakeTestHand(bool isLeft, int frameId = 0, int handId = 0) {
      return MakeTestHand(frameId, handId, isLeft);
    }

    /// <summary>
    /// Returns a test Leap Hand object transformed by the leftHandTransform argument. If the Leap hand is
    /// a right hand, the position and rotation of the Hand will be mirrored along the X axis (so you can provide
    /// LeapTransform to construct both left and right hands.
    /// </summary>
    public static Hand MakeTestHand(bool isLeft, LeapTransform leftHandTransform, int frameId = 0, int handId = 0) {
      if (!isLeft) {
        leftHandTransform.translation = new Vector(-leftHandTransform.translation.x, leftHandTransform.translation.y, leftHandTransform.translation.z);

        leftHandTransform.rotation = new LeapQuaternion(-leftHandTransform.rotation.x,
                                                         leftHandTransform.rotation.y,
                                                         leftHandTransform.rotation.z,
                                                        -leftHandTransform.rotation.w);

        leftHandTransform.MirrorX();
      }
      return MakeTestHand(frameId, handId, isLeft).Transform(leftHandTransform);
    }

    /// <summary>
    /// Returns a test Leap Hand object in the argument pose position and rotation from the hand controller.
    /// </summary>
    public static Hand MakeTestHand(bool isLeft, TestHandPose pose, int frameId = 0, int handId = 0) {
      return MakeTestHand(isLeft, GetTestPoseLeftHandTransform(pose), frameId, handId);
    }

    public enum TestHandPose {
      PoseA,
      PoseB
    }

    public static LeapTransform GetTestPoseLeftHandTransform(TestHandPose pose) {
      LeapTransform transform = LeapTransform.Identity;
      switch (pose) {
        case TestHandPose.PoseA:
          transform.rotation = AngleAxis(180 * Constants.DEG_TO_RAD, Vector.Forward);
          transform.translation = new Vector(80f, 120f, 0f);
          break;
        case TestHandPose.PoseB:
          transform.rotation = Quaternion.Euler(30F, -10F, -20F).ToLeapQuaternion();
          transform.translation = new Vector(220f, 270f, 130f);
          break;
      }
      return transform;
    }

    public static Hand MakeTestHand(int frameId, int handId, bool isLeft) {
      List<Finger> fingers = new List<Finger>(5);
      fingers.Add(MakeThumb(frameId, handId, isLeft));
      fingers.Add(MakeIndexFinger(frameId, handId, isLeft));
      fingers.Add(MakeMiddleFinger(frameId, handId, isLeft));
      fingers.Add(MakeRingFinger(frameId, handId, isLeft));
      fingers.Add(MakePinky(frameId, handId, isLeft));

      Vector armWrist = new Vector(-7.05809944059f, 4.0f, 50.0f);
      Vector elbow = armWrist + 250f * Vector.Backward;

      // Adrian: The previous "armBasis" used "elbow" as a translation component.
      Arm arm = new Arm(elbow, armWrist,(elbow + armWrist)/2, Vector.Forward, 250f, 41f, LeapQuaternion.Identity);
      Hand testHand = new Hand(frameId,
                               handId,
                               1.0f,
                               0.0f,
                               0.0f,
                               0.0f,
                               0.0f,
                               85f,
                               isLeft,
                               0.0f,
                               arm,
                               fingers,
                               new Vector (0,0,0),
                               new Vector(0,0,0),
                               new Vector(0,0,0),
                               Vector.Down,
                               LeapQuaternion.Identity,
                               Vector.Forward,
                               new Vector(-4.36385750984f, 6.5f, 31.0111342526f));

      return testHand;
    }

    static LeapQuaternion AngleAxis(float angle, Vector axis) {
      if (!axis.MagnitudeSquared.NearlyEquals(1.0f)) {
        throw new ArgumentException("Axis must be a unit vector.");
      }
      float sineHalfAngle = Mathf.Sin(angle/2.0f);
      LeapQuaternion q = new LeapQuaternion(sineHalfAngle * axis.x,
                                                sineHalfAngle * axis.y,
                                                sineHalfAngle * axis.z,
                                                Mathf.Cos(angle/2.0f));
      return q.Normalized;
    }

    static LeapQuaternion RotationBetween(Vector fromDirection, Vector toDirection) {
      float m = Mathf.Sqrt(2.0f + 2.0f * fromDirection.Dot(toDirection));
      Vector w = (1.0f / m) * fromDirection.Cross(toDirection);
      return new LeapQuaternion(w.x, w.y, w.z, 0.5f * m);
    }

    static Finger MakeThumb(int frameId, int handId, bool isLeft) {
      //Thumb
      Vector position = new Vector(19.3382610281f, -6.0f, 53.168484654f);
      Vector forward = new Vector(0.636329113772f, -0.5f, -0.899787143982f);
      Vector up = new Vector(0.804793943718f, 0.447213915513f, 0.390264553767f);
      float[] jointLengths = {0.0f, 46.22f, 31.57f, 21.67f};
      return MakeFinger(Finger.FingerType.TYPE_THUMB, position, forward, up, jointLengths, frameId, handId, handId + 0, isLeft);
    }

    static Finger MakeIndexFinger(int frameId, int handId, bool isLeft) {
      //Index Finger
      Vector position = new Vector(23.1812851873f, 2.0f, -23.1493459317f);
      Vector forward = new Vector(0.166044313785f, -0.14834045293f, -0.974897120667f);
      Vector up = new Vector(0.0249066470677f, 0.988936352868f, -0.1462345681f);
      float[]  jointLengths = {68.12f, 39.78f, 22.38f, 15.82f};
      return MakeFinger(Finger.FingerType.TYPE_INDEX, position, forward, up, jointLengths, frameId, handId, handId + 1, isLeft);
    }

    static Finger MakeMiddleFinger(int frameId, int handId, bool isLeft) {
      //Middle Finger
      Vector position = new Vector(2.78877821918f, 4.0f, -23.252105626f);
      Vector forward = new Vector(0.0295207858556f, -0.148340452932f, -0.988495641481f);
      Vector up = new Vector(-0.145765270107f, 0.977715980076f, -0.151075968756f);
      float[]  jointLengths = {64.60f, 44.63f, 26.33f, 17.40f};
      return MakeFinger(Finger.FingerType.TYPE_MIDDLE, position, forward, up, jointLengths, frameId, handId, handId + 2, isLeft);
    }

    static Finger MakeRingFinger(int frameId, int handId, bool isLeft) {
      //Ring Finger
      Vector position = new Vector(-17.447168266f, 4.0f, -17.2791440615f);
      Vector forward = new Vector(-0.121317937368f, -0.148340347175f, -0.981466810174f);
      Vector up = new Vector(-0.216910468316f, 0.968834928679f, -0.119619102602f);
      float[]  jointLengths = {58.00f, 41.37f, 25.65f, 17.30f};
      return MakeFinger(Finger.FingerType.TYPE_RING, position, forward, up, jointLengths, frameId, handId, handId + 3, isLeft);
    }

    static Finger MakePinky(int frameId, int handId, bool isLeft) {
      //Pinky Finger
      Vector position = new Vector(-35.3374394559f, 0.0f, -9.72871382551f);
      Vector forward = new Vector(-0.259328923438f, -0.105851224797f, -0.959970847306f);
      Vector up = new Vector(-0.353350220937f, 0.935459475557f, -0.00769356576168f);
      float[]  jointLengths = {53.69f, 32.74f, 18.11f, 15.96f};
      return MakeFinger(Finger.FingerType.TYPE_PINKY, position, forward, up, jointLengths, frameId, handId, handId + 4, isLeft);
    }


    static Finger MakeFinger(Finger.FingerType name, Vector position, Vector forward, Vector up, float[] jointLengths,
       int frameId, int handId, int fingerId, bool isLeft) {

      forward = forward.Normalized;
      up = up.Normalized;

      Bone[] bones = new Bone[5];
      float proximalDistance = -jointLengths[0];
      Bone metacarpal = MakeBone (Bone.BoneType.TYPE_METACARPAL, position + forward * proximalDistance, jointLengths[0], 8f, forward, up, isLeft);
      proximalDistance += jointLengths[0];
      bones[0] = metacarpal;

      Bone proximal = MakeBone (Bone.BoneType.TYPE_PROXIMAL,  position + forward * proximalDistance, jointLengths[1], 8f, forward, up, isLeft);
      proximalDistance += jointLengths[1];
      bones[1] = proximal;

      Bone intermediate = MakeBone (Bone.BoneType.TYPE_INTERMEDIATE,  position + forward * proximalDistance, jointLengths[2], 8f, forward, up, isLeft);
      proximalDistance += jointLengths[2];
      bones[2] = intermediate;

      Bone distal = MakeBone (Bone.BoneType.TYPE_DISTAL,  position + forward * proximalDistance, jointLengths[3], 8f, forward, up, isLeft);
      bones[3] = distal;

      return new Finger(frameId,
      handId,
      fingerId,
      0.0f,
      distal.NextJoint,
      new Vector(0, 0, 0),
      forward,
      position,
      8f,
      jointLengths[1] + jointLengths[2] + jointLengths[3],
      true,
      name,
      bones[0],
      bones[1],
      bones[2],
      bones[3]);
    }

    static Bone MakeBone(Bone.BoneType name, Vector proximalPosition, float length, float width, Vector direction, Vector up, bool isLeft) {

      LeapQuaternion rotation = UnityEngine.Quaternion.LookRotation(-direction.ToVector3(), up.ToVector3()).ToLeapQuaternion();

      return new Bone(
           proximalPosition,
           proximalPosition + direction * length,
           Vector.Lerp(proximalPosition, proximalPosition + direction * length, .5f),
           direction,
           length,
           width,
           name,
           rotation);
    }
  }
}
