/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/


using UnityEngine;
using UnityEngine.XR.Hands;

namespace Leap
{
    public static class XRHandsUtils
    {
        public static float CalculateGrabStrength(this XRHand hand)
        {
            // magic numbers so it approximately lines up with the leap results
            const float bendZero = 0.25f;
            const float bendOne = 0.85f;

            // Find the minimum bend angle for the non-thumb fingers.
            float minBend = float.MaxValue;

            Vector3 handRadialAxis = GetHandRadialAxis(hand);
            Vector3 handDistalAxis = GetHandDistalAxis(hand);

            minBend = Mathf.Min(GetFingerStrength(hand.GetJoint(XRHandJointID.IndexIntermediate), handRadialAxis, handDistalAxis), minBend);
            minBend = Mathf.Min(GetFingerStrength(hand.GetJoint(XRHandJointID.MiddleIntermediate), handRadialAxis, handDistalAxis), minBend);
            minBend = Mathf.Min(GetFingerStrength(hand.GetJoint(XRHandJointID.RingIntermediate), handRadialAxis, handDistalAxis), minBend);
            minBend = Mathf.Min(GetFingerStrength(hand.GetJoint(XRHandJointID.LittleIntermediate), handRadialAxis, handDistalAxis), minBend);

            // Return the grab strength.
            return Mathf.Clamp01((minBend - bendZero) / (bendOne - bendZero));
        }

        public static float GetFingerStrength(XRHandJoint midJoint, Vector3 radialAxis, Vector3 distalAxis)
        {
            if (midJoint.id == XRHandJointID.ThumbProximal)
            {
                return Vector3.Dot(FingerDirection(midJoint), -radialAxis).Map(-1, 1, 0, 1);
            }

            return Vector3.Dot(FingerDirection(midJoint), -distalAxis).Map(-1, 1, 0, 1);
        }

        public static Vector3 FingerDirection(XRHandJoint midJoint)
        {
            if (midJoint.TryGetPose(out Pose midJointPose))
            {
                return midJointPose.forward;
            }
            else
            {
                return Vector3.zero;
            }
        }

        public static Vector3 GetHandRadialAxis(this XRHand hand)
        {
            Quaternion rotation = hand.rootPose.rotation;
            float d = rotation.x * rotation.x + rotation.y * rotation.y + rotation.z * rotation.z + rotation.w * rotation.w;
            float s = 2.0f / d;
            float ys = rotation.y * s, zs = rotation.z * s;
            float wy = rotation.w * ys, wz = rotation.w * zs;
            float xy = rotation.x * ys, xz = rotation.x * zs;
            float yy = rotation.y * ys, zz = rotation.z * zs;

            Vector3 xBasis = new Vector3(1.0f - (yy + zz), xy + wz, xz - wy);

            if (hand.handedness == Handedness.Left)
            {
                return xBasis;
            }
            else
            {
                return -xBasis;
            }
        }

        public static Vector3 GetHandDistalAxis(this XRHand hand)
        {
            Quaternion rotation = hand.rootPose.rotation;
            float d = rotation.x * rotation.x + rotation.y * rotation.y + rotation.z * rotation.z + rotation.w * rotation.w;
            float s = 2.0f / d;
            float xs = rotation.x * s, ys = rotation.y * s, zs = rotation.z * s;
            float wx = rotation.w * xs, wy = rotation.w * ys;
            float xx = rotation.x * xs, xz = rotation.x * zs;
            float yy = rotation.y * ys, yz = rotation.y * zs;

            Vector3 zBasis = new Vector3(xz + wy, yz - wx, 1.0f - (xx + yy));
            return zBasis;
        }

        public static float CalculatePinchStrength(this XRHand hand, XRHandJointID jointToCompare = XRHandJointID.IndexTip)
        {
            float handScale = CalculateHandScale(hand);

            // Get the thumb position.
            Vector3 thumbTip = Vector3.zero;
            if (hand.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out Pose thumbTipPose)) thumbTip = thumbTipPose.position;

            // Compute the distance midpoints between the thumb and thejointToCompare.
            if (hand.GetJoint(jointToCompare).TryGetPose(out var fingerTipPose))
            {
                float distance = (fingerTipPose.position - thumbTip).magnitude;

                // Compute the pinch strength. Magic values taken from existing LeapC implementation (scaled to metres)
                float distanceZero = 0.0600f * handScale;
                float distanceOne = 0.0220f * handScale;
                return Mathf.Clamp01((distance - distanceZero) / (distanceOne - distanceZero));
            }

            return 0;
        }

        public static float CalculatePinchDistance(this XRHand hand, XRHandJointID jointToCompare = XRHandJointID.IndexTip)
        {
            // Get the thumb position.
            Vector3 thumbTip = Vector3.zero;
            if (hand.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out Pose thumbTipPose)) thumbTip = thumbTipPose.position;

            // Compute the distance midpoints between the thumb and thejointToCompare.
            if (hand.GetJoint(jointToCompare).TryGetPose(out var fingerTipPose))
            {
                float distance = (fingerTipPose.position - thumbTip).magnitude;
                return distance;
            }

            return 0;
        }

        public static Vector3 GetStablePinchPosition(this XRHand hand)
        {
            Vector3 indexTip = Vector3.zero;
            Vector3 thumbTip = Vector3.zero;

            if (hand.GetJoint(XRHandJointID.IndexDistal).TryGetPose(out Pose indexTipPose)) indexTip = indexTipPose.position;
            if (hand.GetJoint(XRHandJointID.ThumbDistal).TryGetPose(out Pose thumbTipPose)) thumbTip = thumbTipPose.position;
            return Vector3.Lerp(indexTip, thumbTip, 0.75f);
        }

        public static float GetMetacarpalLength(this XRHand hand, int fingerIndex)
        {
            float length = 0;

            Pose metacarpal = new Pose();
            Pose proximal = new Pose();

            bool positionsValid = false;

            switch (fingerIndex)
            {
                case 0:
                    if (hand.GetJoint(XRHandJointID.ThumbProximal).TryGetPose(out proximal) &&
                        hand.GetJoint(XRHandJointID.ThumbMetacarpal).TryGetPose(out metacarpal))
                    {
                        positionsValid = true;
                    }
                    break;
                case 1:
                    if (hand.GetJoint(XRHandJointID.IndexProximal).TryGetPose(out proximal) &&
                        hand.GetJoint(XRHandJointID.IndexMetacarpal).TryGetPose(out metacarpal))
                    {
                        positionsValid = true;
                    }
                    break;
                case 2:
                    if (hand.GetJoint(XRHandJointID.MiddleProximal).TryGetPose(out proximal) &&
                        hand.GetJoint(XRHandJointID.MiddleMetacarpal).TryGetPose(out metacarpal))
                    {
                        positionsValid = true;
                    }
                    break;
                case 3:
                    if (hand.GetJoint(XRHandJointID.RingProximal).TryGetPose(out proximal) &&
                        hand.GetJoint(XRHandJointID.RingMetacarpal).TryGetPose(out metacarpal))
                    {
                        positionsValid = true;
                    }
                    break;
                case 4:
                    if (hand.GetJoint(XRHandJointID.LittleProximal).TryGetPose(out proximal) &&
                        hand.GetJoint(XRHandJointID.LittleMetacarpal).TryGetPose(out metacarpal))
                    {
                        positionsValid = true;
                    }
                    break;
                default:
                    break;
            }

            if (positionsValid)
            {
                length += (metacarpal.position - proximal.position).magnitude;
            }

            return length;
        }

        public static float CalculateHandScale(this XRHand hand)
        {
            // Iterate through the fingers, skipping the thumb and accumulate the scale.
            float scale = 0.0f;

            // Magic numbers for default metacarpal lengths
            //{ 0, 0.06812f, 0.06460f, 0.05800f, 0.05369f }
            scale += (GetMetacarpalLength(hand, 1) / 0.06812f) / 4.0f;
            scale += (GetMetacarpalLength(hand, 2) / 0.06460f) / 4.0f;
            scale += (GetMetacarpalLength(hand, 3) / 0.05800f) / 4.0f;
            scale += (GetMetacarpalLength(hand, 4) / 0.05369f) / 4.0f;

            return scale;
        }

    }
}