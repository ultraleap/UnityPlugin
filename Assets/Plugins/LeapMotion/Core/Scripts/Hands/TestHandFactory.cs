/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

namespace Leap {

  using System;
  using System.Collections.Generic;
  using Leap.Unity;
  using UnityEngine;

  public static class TestHandFactory {

    #region Test Frame / Hand API

    public enum UnitType {
      LeapUnits,
      UnityUnits
    }

    /// <summary>
    /// Creates a test Frame that contains two Hands (by default). You can also
    /// optionally specify a TestHandPose to produce a frame with a different test pose.
    /// </summary>
    public static Frame MakeTestFrame(int frameId,
                                      bool includeLeftHand = true,
                                      bool includeRightHand = true,
                                      TestHandPose handPose = TestHandPose.HeadMountedA,
                                      UnitType unitType = UnitType.LeapUnits) {

      var testFrame = new Frame(frameId, 0, 120.0f,
                                new List<Hand>());

      if (includeLeftHand)
        testFrame.Hands.Add(MakeTestHand(true, handPose, frameId, 10, unitType));
      if (includeRightHand)
        testFrame.Hands.Add(MakeTestHand(false, handPose, frameId, 20, unitType));

      return testFrame;
    }

    /// <summary>
    /// Returns a test Leap Hand object transformed by the leftHandTransform argument.
    /// If the Leap hand is a right hand, the position and rotation of the Hand will be
    /// mirrored along the X axis (so you can provide LeapTransform to construct both
    /// left and right hands.
    /// </summary>
    public static Hand MakeTestHand(bool isLeft, LeapTransform leftHandTransform,
                                    int frameId = 0, int handId = 0,
                                    UnitType unitType = UnitType.LeapUnits) {

      // Apply the appropriate mirroring if this is a right hand.
      if (!isLeft) {
        leftHandTransform.translation = new Vector(-leftHandTransform.translation.x,
          leftHandTransform.translation.y, leftHandTransform.translation.z);

        leftHandTransform.rotation = new LeapQuaternion(-leftHandTransform.rotation.x,
                                                         leftHandTransform.rotation.y,
                                                         leftHandTransform.rotation.z,
                                                        -leftHandTransform.rotation.w);

        leftHandTransform.MirrorX();
      }

      // Leap space is oriented differently than Unity space, so correct for this here.
      var hand = makeLeapSpaceTestHand(frameId, handId, isLeft)
                   .Transform(leftHandTransform);
      var correctingQuaternion = Quaternion.Euler(90f, 0f, 180f);
      var correctingLeapQuaternion = new LeapQuaternion(correctingQuaternion.x,
                                                        correctingQuaternion.y,
                                                        correctingQuaternion.z,
                                                        correctingQuaternion.w);

      var transformedHand = hand.Transform(new LeapTransform(Vector.Zero,
                                                             correctingLeapQuaternion));

      if (unitType == UnitType.UnityUnits) {
        transformedHand.TransformToUnityUnits();
      }

      return transformedHand;
    }

    /// <summary>
    /// Returns a test Hand object.
    /// </summary>
    public static Hand MakeTestHand(bool isLeft,
                                    int frameId = 0, int handId = 0,
                                    UnitType unitType = UnitType.LeapUnits) {
      return MakeTestHand(isLeft, LeapTransform.Identity, frameId, handId, unitType);
    }

    /// <summary>
    /// Returns a test Leap Hand object in the argument TestHandPose.
    /// </summary>
    public static Hand MakeTestHand(bool isLeft, TestHandPose pose,
                                    int frameId = 0, int handId = 0,
                                    UnitType unitType = UnitType.LeapUnits) {
      return MakeTestHand(isLeft, GetTestPoseLeftHandTransform(pose),
                          frameId, handId,
                          unitType);
    }

    #endregion

    #region Test Hand Poses

    public enum TestHandPose {
      HeadMountedA,
      HeadMountedB,
      DesktopModeA,
      ScreenTop
    }

    public static LeapTransform GetTestPoseLeftHandTransform(TestHandPose pose) {
      LeapTransform transform = LeapTransform.Identity;
      switch (pose) {
        case TestHandPose.HeadMountedA:
          transform.rotation = angleAxis(180 * Constants.DEG_TO_RAD, Vector.Forward);
          transform.translation = new Vector(80f, 120f, 0f);
          break;
        case TestHandPose.HeadMountedB:
          transform.rotation = Quaternion.Euler(30F, -10F, -20F).ToLeapQuaternion();
          transform.translation = new Vector(220f, 270f, 130f);
          break;
        case TestHandPose.DesktopModeA:
          transform.rotation = angleAxis(0f * Constants.DEG_TO_RAD, Vector.Forward)
                                .Multiply(angleAxis(-90f * Constants.DEG_TO_RAD, Vector.Right))
                                .Multiply(angleAxis(180f * Constants.DEG_TO_RAD, Vector.Up));
          transform.translation = new Vector(120f, 0f, -170f);
          break;
        case TestHandPose.ScreenTop:
          transform.rotation = angleAxis(0 * Constants.DEG_TO_RAD, Vector.Forward)
                                .Multiply(angleAxis(140 * Constants.DEG_TO_RAD, Vector.Right))
                                .Multiply(angleAxis(0 * Constants.DEG_TO_RAD, Vector.Up));
          transform.translation = new Vector(-120f, 20f, -380f);
          transform.scale = new Vector(1, 1, 1);
          break;

      }
      return transform;
    }

    #endregion

    #region Leap Space Hand Generation

    public static Vector PepperWristOffset = new Vector(-8.87f, -0.5f, 85.12f);

    private static Hand makeLeapSpaceTestHand(int frameId, int handId, bool isLeft) {
      List<Finger> fingers = new List<Finger>(5);
      fingers.Add(makeThumb(frameId, handId, isLeft));
      fingers.Add(makeIndexFinger(frameId, handId, isLeft));
      fingers.Add(makeMiddleFinger(frameId, handId, isLeft));
      fingers.Add(makeRingFinger(frameId, handId, isLeft));
      fingers.Add(makePinky(frameId, handId, isLeft));

      Vector armWrist = new Vector(-7.05809944059f, 4.0f, 50.0f);
      Vector elbow = armWrist + 250f * Vector.Backward;

      // Adrian: The previous "armBasis" used "elbow" as a translation component.
      Arm arm = new Arm(elbow, armWrist,(elbow + armWrist)/2, Vector.Forward,
        250f, 41f, LeapQuaternion.Identity);
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
                               PepperWristOffset);
                              //  new Vector(-12.36385750984f, -6.5f, 81.0111342526f));

      return testHand;
    }

    private static LeapQuaternion angleAxis(float angle, Vector axis) {
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

    private static LeapQuaternion rotationBetween(Vector fromDirection, Vector toDirection) {
      float m = Mathf.Sqrt(2.0f + 2.0f * fromDirection.Dot(toDirection));
      Vector w = (1.0f / m) * fromDirection.Cross(toDirection);
      return new LeapQuaternion(w.x, w.y, w.z, 0.5f * m);
    }

    private static Finger makeThumb(int frameId, int handId, bool isLeft) {
      Vector position = new Vector(19.3382610281f, -6.0f, 53.168484654f);
      Vector forward = new Vector(0.636329113772f, -0.5f, -0.899787143982f);
      Vector up = new Vector(0.804793943718f, 0.447213915513f, 0.390264553767f);
      float[] jointLengths = {0.0f, 46.22f, 31.57f, 21.67f};
      return makeFinger(Finger.FingerType.TYPE_THUMB, position, forward, up, jointLengths, frameId, handId, handId + 0, isLeft);
    }

    private static Finger makeIndexFinger(int frameId, int handId, bool isLeft) {
      Vector position = new Vector(23.1812851873f, 2.0f, -23.1493459317f);
      Vector forward = new Vector(0.166044313785f, -0.14834045293f, -0.974897120667f);
      Vector up = new Vector(0.0249066470677f, 0.988936352868f, -0.1462345681f);
      float[]  jointLengths = {68.12f, 39.78f, 22.38f, 15.82f};
      return makeFinger(Finger.FingerType.TYPE_INDEX, position, forward, up, jointLengths, frameId, handId, handId + 1, isLeft);
    }

    private static Finger makeMiddleFinger(int frameId, int handId, bool isLeft) {
      Vector position = new Vector(2.78877821918f, 4.0f, -23.252105626f);
      Vector forward = new Vector(0.0295207858556f, -0.148340452932f, -0.988495641481f);
      Vector up = new Vector(-0.145765270107f, 0.977715980076f, -0.151075968756f);
      float[]  jointLengths = {64.60f, 44.63f, 26.33f, 17.40f};
      return makeFinger(Finger.FingerType.TYPE_MIDDLE, position, forward, up, jointLengths, frameId, handId, handId + 2, isLeft);
    }

    private static Finger makeRingFinger(int frameId, int handId, bool isLeft) {
      Vector position = new Vector(-17.447168266f, 4.0f, -17.2791440615f);
      Vector forward = new Vector(-0.121317937368f, -0.148340347175f, -0.981466810174f);
      Vector up = new Vector(-0.216910468316f, 0.968834928679f, -0.119619102602f);
      float[]  jointLengths = {58.00f, 41.37f, 25.65f, 17.30f};
      return makeFinger(Finger.FingerType.TYPE_RING, position, forward, up, jointLengths, frameId, handId, handId + 3, isLeft);
    }

    private static Finger makePinky(int frameId, int handId, bool isLeft) {
      Vector position = new Vector(-35.3374394559f, 0.0f, -9.72871382551f);
      Vector forward = new Vector(-0.259328923438f, -0.105851224797f, -0.959970847306f);
      Vector up = new Vector(-0.353350220937f, 0.935459475557f, -0.00769356576168f);
      float[]  jointLengths = {53.69f, 32.74f, 18.11f, 15.96f};
      return makeFinger(Finger.FingerType.TYPE_PINKY, position, forward, up, jointLengths, frameId, handId, handId + 4, isLeft);
    }


    private static Finger makeFinger(Finger.FingerType name, Vector position, Vector forward, Vector up, float[] jointLengths,
       int frameId, int handId, int fingerId, bool isLeft) {

      forward = forward.Normalized;
      up = up.Normalized;

      Bone[] bones = new Bone[5];
      float proximalDistance = -jointLengths[0];
      Bone metacarpal = makeBone (Bone.BoneType.TYPE_METACARPAL, position + forward * proximalDistance, jointLengths[0], 8f, forward, up, isLeft);
      proximalDistance += jointLengths[0];
      bones[0] = metacarpal;

      Bone proximal = makeBone (Bone.BoneType.TYPE_PROXIMAL,  position + forward * proximalDistance, jointLengths[1], 8f, forward, up, isLeft);
      proximalDistance += jointLengths[1];
      bones[1] = proximal;

      Bone intermediate = makeBone (Bone.BoneType.TYPE_INTERMEDIATE,  position + forward * proximalDistance, jointLengths[2], 8f, forward, up, isLeft);
      proximalDistance += jointLengths[2];
      bones[2] = intermediate;

      Bone distal = makeBone (Bone.BoneType.TYPE_DISTAL,  position + forward * proximalDistance, jointLengths[3], 8f, forward, up, isLeft);
      bones[3] = distal;

      return new Finger(frameId,
      handId,
      fingerId,
      0.0f,
      distal.NextJoint,
      forward,
      8f,
      jointLengths[1] + jointLengths[2] + jointLengths[3],
      true,
      name,
      bones[0],
      bones[1],
      bones[2],
      bones[3]);
    }

    private static Bone makeBone(Bone.BoneType name, Vector proximalPosition, float length, float width, Vector direction, Vector up, bool isLeft) {

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

    #endregion

  }

  // Note: The fact that this class needs to exist is ridiculous
  // TODO: Look into automatically returning things in Unity units? Would require changes
  // for everything that uses the TestHandFactory.
  public static class LeapTestProviderExtensions {

    public static readonly float MM_TO_M = 1e-3f;

    public static LeapTransform GetLeapTransform(Vector3 position, Quaternion rotation) {
      Vector scale = new Vector(MM_TO_M, MM_TO_M, MM_TO_M); // Leap units -> Unity units.
      LeapTransform transform = new LeapTransform(position.ToVector(), rotation.ToLeapQuaternion(), scale);
      transform.MirrorZ(); // Unity is left handed.
      return transform;
    }

    public static void TransformToUnityUnits(this Hand hand) {
      hand.Transform(GetLeapTransform(Vector3.zero, Quaternion.identity));
    }

  }

}
