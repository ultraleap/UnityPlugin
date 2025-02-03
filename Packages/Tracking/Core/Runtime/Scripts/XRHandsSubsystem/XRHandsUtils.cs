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
            Vector3 finalPosition = Vector3.zero;
            if (hand.isTracked)
            {
                if (hand.GetJoint(XRHandJointID.Wrist).TryGetPose(out Pose wristPose))
                {
                    //Base our final aiming position on the wrist to remove any offsets while doing the pinch
                    Vector3 wristPosition = wristPose.position;

                    //Shift the wrist position to make it a more natural centered aiming position
                    wristPosition.z -= 0.05f;
                    if (hand.handedness == Handedness.Right)
                        wristPosition.x -= 0.025f;
                    else
                        wristPosition.x += 0.025f;

                    //Offset the wrist position by the expressiveness of the hand, gathered via the middle metacarpal
                    if (hand.GetJoint(XRHandJointID.MiddleMetacarpal).TryGetPose(out Pose middlePose))
                        finalPosition = wristPosition + (middlePose.forward * 0.05f);

                    //Shift the final height in relation to the camera to allow relaxed/gorilla aiming
                    finalPosition.y += (Camera.main.transform.position.y - wristPosition.y - 0.33f) / 2.0f;
                }
            }
            return finalPosition;
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

        /// <summary>
        /// The transform of the hand.
        /// 
        /// Note, in version prior to 3.1, the Basis was a Matrix object.
        /// @since 3.1
        /// </summary>
        public static LeapTransform GetBasis(this XRHand hand)
        {
            LeapTransform basis = new LeapTransform();
            Pose palmPose;
            if (hand.GetJoint(XRHandJointID.Palm).TryGetPose(out palmPose))
            {
                basis.translation = palmPose.position;
                basis.rotation = hand.rootPose.rotation; // ???
            }

            return basis;
        }

        /// <summary>
        /// Returns the direction the Hand's palm is facing. For the other two palm-basis
        /// directions, see RadialAxis and DistalAxis.
        /// 
        /// The direction out of the back of the hand would be called the dorsal axis.
        /// </summary>
        public static Vector3 PalmarAxis(this XRHand hand)
        {
            Pose palmPose;
            if (hand.GetJoint(XRHandJointID.Palm).TryGetPose(out palmPose))
            {
                return palmPose.up; //?
            }

            return Vector3.zero;
        }

        /// <summary>
        /// Returns the the direction towards the thumb that is perpendicular to the palmar
        /// and distal axes. Left and right hands will return opposing directions.
        /// 
        /// The direction away from the thumb would be called the ulnar axis.
        /// </summary>
        public static Vector3 RadialAxis(this XRHand hand)
        {
            if (hand.handedness == Handedness.Right)
            {
                return -hand.GetBasis().xBasis;
            }
            else
            {
                return hand.GetBasis().xBasis;
            }
        }

        /// <summary>
        /// Returns the direction towards the fingers that is perpendicular to the palmar
        /// and radial axes.
        /// 
        /// The direction towards the wrist would be called the proximal axis.
        /// </summary>
        public static Vector3 DistalAxis(this XRHand hand)
        {
            return hand.GetBasis().zBasis;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////
        // From HandUtils

        /// <summary>
        /// Returns a decent approximation of where the hand is pinching, or where it will pinch,
        /// even if the index and thumb tips are far apart.
        /// 
        /// In general, this will be more stable than GetPinchPosition().
        /// </summary>
        public static Vector3 GetPredictedPinchPosition(this XRHand hand)
        {
            Pose indexTipPose, thumbTipPose, indexKnucklePose;

            if (hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out indexTipPose))
            {
                hand.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out thumbTipPose);
                hand.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out indexKnucklePose);

                Vector3 indexTip = indexTipPose.position;
                Vector3 thumbTip = thumbTipPose.position;
                Vector3 indexKnuckle = indexKnucklePose.position;

                // The predicted pinch point is a rigid point in hand-space linearly offset by the
                // index finger knuckle position, scaled by the index finger's length, and lightly
                // influenced by the actual thumb and index tip positions.
                float indexLength = GetIndexFingerLength(hand);
                Vector3 radialAxis = hand.RadialAxis();
                float thumbInfluence = Vector3.Dot((thumbTip - indexKnuckle).normalized, radialAxis).Map(0F, 1F, 0.5F, 0F);
                Vector3 predictedPinchPoint = indexKnuckle + hand.PalmarAxis() * indexLength * 0.85F
                                                           + hand.DistalAxis() * indexLength * 0.20F
                                                           + radialAxis * indexLength * 0.20F;
                predictedPinchPoint = Vector3.Lerp(predictedPinchPoint, thumbTip, thumbInfluence);
                predictedPinchPoint = Vector3.Lerp(predictedPinchPoint, indexTip, 0.15F);

                return predictedPinchPoint;
            }

            return Vector3.zero;
        }

        internal static float GetIndexFingerLength(XRHand hand)
        {
            float indexFingerLength = 0;

            Pose tipPose, distalPose, intermediatePose, proximalPose;

            if (hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out tipPose))
            {
                hand.GetJoint(XRHandJointID.IndexDistal).TryGetPose(out distalPose);
                hand.GetJoint(XRHandJointID.IndexIntermediate).TryGetPose(out intermediatePose);
                hand.GetJoint(XRHandJointID.IndexProximal).TryGetPose(out proximalPose);

                indexFingerLength = (tipPose.position - distalPose.position).magnitude +
                    (distalPose.position - intermediatePose.position).magnitude +
                    (intermediatePose.position - proximalPose.position).magnitude;
            }

            return indexFingerLength;
        }


    }
}