using Leap;
using System;
using UnityEngine;

namespace Leap.Tracking.OpenXR
{
    public static class MetaCompatibility
    {
        private enum ConversionType { MetaToLeap, LeapToMeta };

        // These constants encode the Leap -> Meta scaling / offsets.
        // They have been dervied from reviewing the differences in the joint positions and rotations between LeapC and Meta OpenXR data when viewing hands in the same pose
        private static readonly float[] MetacarpalOutRotations = { 0.0f, 0.0f, 0.0f, 6.0f, 4.0f };
        private static readonly float[] MetacarpalDownRotations = { 0.0f, 12.0f, 14.0f, 12.0f, 10.0f };

        public static Hand ToMetaLayout(this Hand hand) => hand.ConvertHand(ConversionType.LeapToMeta);

        public static Hand FromMetaLayout(this Hand hand) => hand.ConvertHand(ConversionType.MetaToLeap);

        private static Hand ConvertHand(this Hand hand, ConversionType type)
        {
            // Scale and re-orientate all the metacarpal joints from Meta's layout.
            foreach (var finger in hand.fingers)
            {
                var proximal = finger.Proximal;
                var metacarpal = finger.Metacarpal;

                // Thumb is adjusted differently.
                if (finger.Type == Finger.FingerType.THUMB)
                {
                    var thumbUp = metacarpal.Rotation * Vector3.up;
                    var thumbAdjustment = (type == ConversionType.LeapToMeta ? 1.0f : -1.0f) *
                                          new Vector3(hand.IsLeft ? 0.005f : -0.005f, 0f, 0.012f);

                    // Adjust Metacarpal
                    metacarpal.PrevJoint += thumbAdjustment;
                    metacarpal.NextJoint += thumbAdjustment;
                    metacarpal.Center += thumbAdjustment;

                    // Adjust proximal (requires recalculating direction properties).
                    proximal.PrevJoint += thumbAdjustment;
                    proximal.Center = Vector3.Lerp(proximal.PrevJoint, proximal.NextJoint, 0.5f);
                    proximal.Direction = (proximal.NextJoint - proximal.PrevJoint).normalized;
                    proximal.Rotation = Quaternion.LookRotation(proximal.Direction, thumbUp);
                }
                else
                {
                    // Recalculate the required rotation and adjust the root bone position.
                    metacarpal.Rotation *= Quaternion.Euler(
                        (type == ConversionType.LeapToMeta ? -1.0f : 1.0f) * MetacarpalDownRotations[(int)finger.Type],
                        (type == ConversionType.LeapToMeta ? 1.0f : -1.0f) * (hand.IsLeft
                            ? MetacarpalOutRotations[(int)finger.Type]
                            : -MetacarpalOutRotations[(int)finger.Type]),
                        0);
                    var adjustedReverseDirection = metacarpal.Rotation * Vector3.back;
                    metacarpal.PrevJoint = metacarpal.NextJoint + (adjustedReverseDirection * metacarpal.Length);

                    // Recalculate the other properties to be consistent.
                    metacarpal.Direction = metacarpal.Rotation * Vector3.forward;
                    metacarpal.Center = Vector3.Lerp(metacarpal.PrevJoint, metacarpal.NextJoint, 0.5f);
                }
            }

            // Adjust the wrist and the arm joints.
            hand.WristPosition += hand.Direction * (type == ConversionType.LeapToMeta ? -0.005f : 0.005f);
            var arm = hand.Arm;
            var armUp = arm.Rotation * Vector3.up;
            arm.NextJoint = hand.WristPosition;
            arm.Center = Vector3.Lerp(arm.PrevJoint, arm.NextJoint, 0.5f);
            arm.Direction = (arm.NextJoint - arm.PrevJoint).normalized;
            arm.Rotation = Quaternion.LookRotation(arm.Direction, armUp);

            return hand;
        }
    }
}
