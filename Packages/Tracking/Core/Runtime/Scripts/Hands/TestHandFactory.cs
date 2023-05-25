/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

namespace Leap
{
    using LeapInternal;
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public static class TestHandFactory
    {

        #region Test Frame / Hand API

        public enum UnitType
        {
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
                                          TestHandPose handPose = TestHandPose.HeadMountedA)
        {

            var testFrame = new Frame(frameId, 0, 120.0f,
                                      new List<Hand>());

            if (includeLeftHand)
                testFrame.Hands.Add(MakeTestHand(true, handPose, frameId, 10));
            if (includeRightHand)
                testFrame.Hands.Add(MakeTestHand(false, handPose, frameId, 20));

            return testFrame;
        }

        /// <summary>
        /// Returns a test Leap Hand object transformed by the leftHandTransform argument.
        /// If the Leap hand is a right hand, the position and rotation of the Hand will be
        /// mirrored along the X axis (so you can provide LeapTransform to construct both
        /// left and right hands.
        /// </summary>
        public static Hand MakeTestHand(bool isLeft, LeapTransform leftHandTransform,
                                        int frameId = 0, int handId = 0)
        {

            // Apply the appropriate mirroring if this is a right hand.
            if (!isLeft)
            {
                leftHandTransform.translation = new Vector3(-leftHandTransform.translation.x,
                  leftHandTransform.translation.y, leftHandTransform.translation.z);

                leftHandTransform.rotation = new Quaternion(-leftHandTransform.rotation.x,
                                                                 leftHandTransform.rotation.y,
                                                                 leftHandTransform.rotation.z,
                                                                -leftHandTransform.rotation.w);

                leftHandTransform.MirrorX();
            }

            // Leap space is oriented differently than Unity space, so correct for this here.
            var hand = makeLeapSpaceTestHand(frameId, handId, isLeft)
                         .Transform(leftHandTransform);

            var transformedHand = hand.Transform(new LeapTransform(Vector3.zero,
                  Quaternion.Euler(90f, 0f, 180f)));

            transformedHand.TransformToUnityUnits();

            return transformedHand;
        }

        /// <summary>
        /// Returns a test Hand object.
        /// </summary>
        public static Hand MakeTestHand(bool isLeft,
                                        int frameId = 0, int handId = 0)
        {
            return MakeTestHand(isLeft, LeapTransform.Identity, frameId, handId);
        }

        /// <summary>
        /// Returns a test Leap Hand object in the argument TestHandPose.
        /// </summary>
        public static Hand MakeTestHand(bool isLeft, TestHandPose pose,
                                        int frameId = 0, int handId = 0)
        {
            return MakeTestHand(isLeft, GetTestPoseLeftHandTransform(pose),
                                frameId, handId);
        }

        #endregion

        #region Test Hand Poses

        public enum TestHandPose
        {
            HeadMountedA,
            HeadMountedB,
            DesktopModeA,
            Screentop
        }

        public static LeapTransform GetTestPoseLeftHandTransform(TestHandPose pose)
        {
            LeapTransform transform = LeapTransform.Identity;
            switch (pose)
            {
                case TestHandPose.HeadMountedA:
                    transform.rotation = angleAxis(180 * Mathf.Deg2Rad, Vector3.back);
                    transform.translation = new Vector3(80f, 120f, 0f);
                    break;
                case TestHandPose.HeadMountedB:
                    transform.rotation = Quaternion.Euler(30F, -10F, -20F);
                    transform.translation = new Vector3(220f, 270f, 130f);
                    break;
                case TestHandPose.DesktopModeA:
                    transform.rotation = (angleAxis(0f * Mathf.Deg2Rad, Vector3.back)
                                          * angleAxis(-90f * Mathf.Deg2Rad, Vector3.right)
                                          * angleAxis(180f * Mathf.Deg2Rad, Vector3.up));
                    transform.translation = new Vector3(120f, 0f, -170f);
                    break;
                case TestHandPose.Screentop:
                    transform.rotation = (angleAxis(0 * Mathf.Deg2Rad, Vector3.back)
                                          * angleAxis(140 * Mathf.Deg2Rad, Vector3.right)
                                          * angleAxis(0 * Mathf.Deg2Rad, Vector3.up));
                    transform.translation = new Vector3(-120f, 20f, -380f);
                    transform.scale = new Vector3(1, 1, 1);
                    break;

            }
            return transform;
        }

        #endregion

        #region Leap Space Hand Generation

        private static Hand makeLeapSpaceTestHand(int frameId, int handId, bool isLeft)
        {
            List<Finger> fingers = new List<Finger>(5);
            fingers.Add(makeThumb(frameId, handId, isLeft));
            fingers.Add(makeIndexFinger(frameId, handId, isLeft));
            fingers.Add(makeMiddleFinger(frameId, handId, isLeft));
            fingers.Add(makeRingFinger(frameId, handId, isLeft));
            fingers.Add(makePinky(frameId, handId, isLeft));

            Vector3 armWrist = new Vector3(-7.05809944059f, 4.0f, 50.0f);
            Vector3 elbow = armWrist + 250f * Vector3.forward;

            // Adrian: The previous "armBasis" used "elbow" as a translation component.
            Arm arm = new Arm(elbow, armWrist, (elbow + armWrist) / 2, Vector3.back,
              250f, 41f, Quaternion.identity);
            Hand testHand = new Hand(frameId,
                                     handId,
                                     1.0f,
                                     0.0f,
                                     0.0f,
                                     0.0f,
                                     85f,
                                     isLeft,
                                     0.0f,
                                     arm,
                                     fingers,
                                     Vector3.zero,
                                     Vector3.zero,
                                     Vector3.zero,
                                     Vector3.down,
                                     Quaternion.identity,
                                     Vector3.back,
                                     new Vector3(-8.87f, -0.5f, 85.12f));
            //  new Vector3(-12.36385750984f, -6.5f, 81.0111342526f));

            return testHand;
        }

        private static Quaternion angleAxis(float angle, Vector3 axis)
        {
            if (!axis.sqrMagnitude.NearlyEquals(1.0f))
            {
                throw new ArgumentException("Axis must be a unit vector.");
            }
            float sineHalfAngle = Mathf.Sin(angle / 2.0f);
            Quaternion q = new Quaternion(sineHalfAngle * axis.x,
                                                      sineHalfAngle * axis.y,
                                                      sineHalfAngle * axis.z,
                                                      Mathf.Cos(angle / 2.0f));
            return q.normalized;
        }

        private static Quaternion rotationBetween(Vector3 fromDirection, Vector3 toDirection)
        {
            float m = Mathf.Sqrt(2.0f + 2.0f * Vector3.Dot(fromDirection, toDirection));
            Vector3 w = (1.0f / m) * Vector3.Cross(fromDirection, toDirection);
            return new Quaternion(w.x, w.y, w.z, 0.5f * m);
        }

        private static Finger makeThumb(int frameId, int handId, bool isLeft)
        {
            Vector3 position = new Vector3(19.3382610281f, -6.0f, 53.168484654f);
            Vector3 forward = new Vector3(0.636329113772f, -0.5f, -0.899787143982f);
            Vector3 up = new Vector3(0.804793943718f, 0.447213915513f, 0.390264553767f);
            float[] jointLengths = { 0.0f, 46.22f, 31.57f, 21.67f };
            return makeFinger(Finger.FingerType.TYPE_THUMB, position, forward, up, jointLengths, frameId, handId, handId + 0, isLeft);
        }

        private static Finger makeIndexFinger(int frameId, int handId, bool isLeft)
        {
            Vector3 position = new Vector3(23.1812851873f, 2.0f, -23.1493459317f);
            Vector3 forward = new Vector3(0.166044313785f, -0.14834045293f, -0.974897120667f);
            Vector3 up = new Vector3(0.0249066470677f, 0.988936352868f, -0.1462345681f);
            float[] jointLengths = { 68.12f, 39.78f, 22.38f, 15.82f };
            return makeFinger(Finger.FingerType.TYPE_INDEX, position, forward, up, jointLengths, frameId, handId, handId + 1, isLeft);
        }

        private static Finger makeMiddleFinger(int frameId, int handId, bool isLeft)
        {
            Vector3 position = new Vector3(2.78877821918f, 4.0f, -23.252105626f);
            Vector3 forward = new Vector3(0.0295207858556f, -0.148340452932f, -0.988495641481f);
            Vector3 up = new Vector3(-0.145765270107f, 0.977715980076f, -0.151075968756f);
            float[] jointLengths = { 64.60f, 44.63f, 26.33f, 17.40f };
            return makeFinger(Finger.FingerType.TYPE_MIDDLE, position, forward, up, jointLengths, frameId, handId, handId + 2, isLeft);
        }

        private static Finger makeRingFinger(int frameId, int handId, bool isLeft)
        {
            Vector3 position = new Vector3(-17.447168266f, 4.0f, -17.2791440615f);
            Vector3 forward = new Vector3(-0.121317937368f, -0.148340347175f, -0.981466810174f);
            Vector3 up = new Vector3(-0.216910468316f, 0.968834928679f, -0.119619102602f);
            float[] jointLengths = { 58.00f, 41.37f, 25.65f, 17.30f };
            return makeFinger(Finger.FingerType.TYPE_RING, position, forward, up, jointLengths, frameId, handId, handId + 3, isLeft);
        }

        private static Finger makePinky(int frameId, int handId, bool isLeft)
        {
            Vector3 position = new Vector3(-35.3374394559f, 0.0f, -9.72871382551f);
            Vector3 forward = new Vector3(-0.259328923438f, -0.105851224797f, -0.959970847306f);
            Vector3 up = new Vector3(-0.353350220937f, 0.935459475557f, -0.00769356576168f);
            float[] jointLengths = { 53.69f, 32.74f, 18.11f, 15.96f };
            return makeFinger(Finger.FingerType.TYPE_PINKY, position, forward, up, jointLengths, frameId, handId, handId + 4, isLeft);
        }


        private static Finger makeFinger(Finger.FingerType name, Vector3 position, Vector3 forward, Vector3 up, float[] jointLengths,
           int frameId, int handId, int fingerId, bool isLeft)
        {

            forward = forward.normalized;
            up = up.normalized;

            Bone[] bones = new Bone[5];
            float proximalDistance = -jointLengths[0];
            Bone metacarpal = makeBone(Bone.BoneType.TYPE_METACARPAL, position + forward * proximalDistance, jointLengths[0], 8f, forward, up, isLeft);
            proximalDistance += jointLengths[0];
            bones[0] = metacarpal;

            Bone proximal = makeBone(Bone.BoneType.TYPE_PROXIMAL, position + forward * proximalDistance, jointLengths[1], 8f, forward, up, isLeft);
            proximalDistance += jointLengths[1];
            bones[1] = proximal;

            Bone intermediate = makeBone(Bone.BoneType.TYPE_INTERMEDIATE, position + forward * proximalDistance, jointLengths[2], 8f, forward, up, isLeft);
            proximalDistance += jointLengths[2];
            bones[2] = intermediate;

            Bone distal = makeBone(Bone.BoneType.TYPE_DISTAL, position + forward * proximalDistance, jointLengths[3], 8f, forward, up, isLeft);
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

        private static Bone makeBone(Bone.BoneType name, Vector3 proximalPosition, float length, float width, Vector3 direction, Vector3 up, bool isLeft)
        {

            Quaternion rotation = UnityEngine.Quaternion.LookRotation(-direction, up);

            return new Bone(
                 proximalPosition,
                 proximalPosition + direction * length,
                 Vector3.Lerp(proximalPosition, proximalPosition + direction * length, .5f),
                 direction,
                 length,
                 width,
                 name,
                 rotation);
        }

        #endregion

    }
}