/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap
{
    using System;
    public static class TransformExtensions
    {

        /**
         * Does an in-place rigid transformation of a Frame.
         *
         * @param transform A LeapTransform containing the desired translation, rotation, and scale
         * to be applied to the Frame.
         */
        public static Frame Transform(this Frame frame, LeapTransform transform)
        {
            for (int i = frame.Hands.Count; i-- != 0;)
            {
                frame.Hands[i].Transform(transform);
            }

            return frame;
        }

        /**
         * Returns a new frame that is a copy of a frame, with an additional rigid
         * transformation applied to it.
         *
         * @param transform The transformation to be applied to the copied frame.
         */
        public static Frame TransformedCopy(this Frame frame, LeapTransform transform)
        {
            return new Frame().CopyFrom(frame).Transform(transform);
        }

        /**
         * Does an in-place rigid transformation of a Hand.
         *
         * @param transform A LeapTransform containing the desired translation, rotation, and scale
         * to be applied to the Hand.
         */
        public static Hand Transform(this Hand hand, LeapTransform transform)
        {
            hand.PalmPosition = transform.TransformPoint(hand.PalmPosition);
            hand.StabilizedPalmPosition = transform.TransformPoint(hand.StabilizedPalmPosition);
            hand.PalmVelocity = transform.TransformVelocity(hand.PalmVelocity);
            hand.PalmNormal = transform.TransformDirection(hand.PalmNormal);
            hand.Direction = transform.TransformDirection(hand.Direction);
            hand.WristPosition = transform.TransformPoint(hand.WristPosition);
            hand.PalmWidth *= Math.Abs(transform.scale.x);
            hand.Rotation = transform.TransformQuaternion(hand.Rotation);

            hand.Arm.Transform(transform);

            for (int i = 5; i-- != 0;)
            {
                hand.Fingers[i].Transform(transform);
            }

            return hand;
        }

        /**
         * Returns a new hand that is a copy of a hand, with an additional rigid
         * transformation applied to it.
         *
         * @param transform The transformation to be applied to the copied hand.
         */
        public static Hand TransformedCopy(this Hand hand, LeapTransform transform)
        {
            return new Hand().CopyFrom(hand).Transform(transform);
        }

        /**
         * Does an in-place rigid transformation of a Finger.
         *
         * @param transform A LeapTransform containing the desired translation, rotation, and scale
         * to be applied to the Finger.
         */
        public static Finger Transform(this Finger finger, LeapTransform transform)
        {
            Bone nextBone = finger.bones[3];
            nextBone.NextJoint = transform.TransformPoint(nextBone.NextJoint);

            finger.TipPosition = nextBone.NextJoint;

            for (int i = 3; i-- != 0;)
            {
                Bone bone = finger.bones[i];

                bone.NextJoint = nextBone.PrevJoint = transform.TransformPoint(bone.NextJoint);

                nextBone.TransformGivenJoints(transform);
                nextBone = bone;
            }

            nextBone.PrevJoint = transform.TransformPoint(nextBone.PrevJoint);
            nextBone.TransformGivenJoints(transform);

            finger.Direction = finger.bones[2].Direction;
            finger.Width *= Math.Abs(transform.scale.x);
            finger.Length *= Math.Abs(transform.scale.z);

            return finger;
        }

        /**
         * Returns a new finger that is a copy of a finger, with an additional rigid
         * transformation applied to it.
         *
         * @param transform The transformation to be applied to the copied finger.
         */
        public static Finger TransformedCopy(this Finger finger, LeapTransform transform)
        {
            return new Finger().CopyFrom(finger).Transform(transform);
        }

        /**
         * Does an in-place rigid transformation of a Bone.
         *
         * @param transform A LeapTransform containing the desired translation, rotation, and scale
-    *  to be applied to the bone.
         */
        public static Bone Transform(this Bone bone, LeapTransform transform)
        {
            bone.PrevJoint = transform.TransformPoint(bone.PrevJoint);
            bone.NextJoint = transform.TransformPoint(bone.NextJoint);

            bone.TransformGivenJoints(transform);

            return bone;
        }

        /**
         * Does an in-place rigid transformation of a Bone, assuming the joints have already been transformed.
         *
         * @param transform A LeapTransform containing the desired translation, rotation, and scale
-    *  to be applied to the bone.
         */
        internal static void TransformGivenJoints(this Bone bone, LeapTransform transform)
        {
            bone.Length *= Math.Abs(transform.scale.z);
            bone.Center = (bone.PrevJoint + bone.NextJoint) / 2.0f;

            if (bone.Length < float.Epsilon)
            {
                bone.Direction = Vector3.zero;
            }
            else
            {
                bone.Direction = (bone.NextJoint - bone.PrevJoint) / bone.Length;
            }

            bone.Width *= Math.Abs(transform.scale.x);
            bone.Rotation = transform.TransformQuaternion(bone.Rotation);
        }

        /**
         * Returns a new bone that is a copy of a bone, with an additional rigid
         * transformation applied to it.
         *
         * @param transform The transformation to be applied to the copied bone.
         */
        public static Bone TransformedCopy(this Bone bone, LeapTransform transform)
        {
            return new Bone().CopyFrom(bone).Transform(transform);
        }
    }
}