/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Leap
{
    public enum ChiralitySelection
    {
        LEFT = 0,
        RIGHT = 1,
        BOTH = 2,
        NONE = 3
    }

    /// <summary>
    /// Static convenience methods and extension methods for getting useful Hand data.
    /// </summary>
    public static class Hands
    {
        private static LeapProvider s_provider;

        /// <summary>
        /// Assign a static reference to the most suitable provider in the scene.
        /// 
        /// Order:
        /// - First PostProcessProvider found
        /// - First XRLeapProviderManager found
        /// - First LeapProvider found
        /// </summary>
        private static void AssignBestLeapProvider()
        {
            // Fall through to the best available Leap Provider if none is assigned
            if (s_provider == null)
            {
                s_provider = UnityEngine.Object.FindAnyObjectByType<PostProcessProvider>();

                if (s_provider == null)
                {
                    s_provider = UnityEngine.Object.FindAnyObjectByType<XRLeapProviderManager>();
                    if (s_provider == null)
                    {
                        s_provider = UnityEngine.Object.FindAnyObjectByType<LeapProvider>();
                        if (s_provider == null)
                        {
                            Debug.Log("There are no Leap Providers in the scene, please assign one manually." +
                                "Alternatively, use Hands.CreateXRLeapProvider() to automatically create an XRLeapProvider");
                            return;
                        }
                    }
                }
            }

            Debug.Log("LeapProvider was not assigned. Auto assigning: " + s_provider);
        }

        /// <summary>
        /// Assign a static reference to the most suitable provider in the scene.
        /// 
        /// Order:
        /// - First PostProcessProvider found
        /// - First XRLeapProviderManager found
        /// - First LeapProvider found
        /// </summary>
        public static LeapXRServiceProvider CreateXRLeapProviderManager()
        {
            GameObject leapProviderGO = new GameObject("Leap XR Service Provider");
            LeapXRServiceProvider leapXRServiceProvider = leapProviderGO.AddComponent<LeapXRServiceProvider>();
            return leapXRServiceProvider;
        }

        /// <summary>
        /// Static convenience accessor for a LeapProvider in the scene. Preference is given
        /// to a LeapServiceProvider if there is one.
        /// 
        /// If static memory currently has no reference for the provider (or if it was
        /// destroyed), this call will search the scene for a LeapProvider and cache it to be
        /// returned next time.
        /// 
        /// If there is no LeapProvider in your scene, this getter
        /// will return null. Be warned that calling this regularly can be expensive if
        /// LeapProviders often don't exist in your scene or are frequently destroyed.
        /// </summary>
        public static LeapProvider Provider
        {
            get
            {
                if (s_provider == null)
                {
                    AssignBestLeapProvider();
                }
                return s_provider;
            }
            set { s_provider = value; }
        }

        /// <summary>
        /// Try to get the LeapProvider and Chirality of the HandModelBase component attached to this GameObject
        /// </summary>
        /// <returns>True if a HandModelBase exists. This can still return a null _provider if the HandModelBase has no LeapProvider</returns>
        public static bool TryGetProviderAndChiralityFromHandModel(GameObject _handModelGameObject, out LeapProvider _provider, out Chirality _chirality)
        {
            if (_handModelGameObject == null)
            {
                _provider = null;
                _chirality = default;
                return false;
            }

            var _handModelBase = _handModelGameObject.GetComponent<HandModelBase>();

            if (_handModelBase != null)
            {
                _provider = _handModelBase.leapProvider;
                _chirality = _handModelBase.Handedness;
                return true;
            }
            else
            {
                _provider = null;
                _chirality = default;
                return false;
            }
        }

        /// <summary>
        /// Return the specified bone of the specified finger
        /// 
        /// Shorthand for finger.bones[(int)boneType],
        /// </summary>
        public static Bone GetBone(this Finger finger, Bone.BoneType boneType)
        {
            return finger.bones[(int)boneType];
        }

        /// <summary>
        /// Return the specified bone of the specified finger of the specificed hand
        /// 
        /// Shorthand for hand.Fingers[(int)fingerType].bones[(int)boneType],
        /// </summary>
        public static Bone GetBone(this Hand hand, Finger.FingerType fingerType, Bone.BoneType boneType)
        {
            return hand.fingers[(int)fingerType].bones[(int)boneType];
        }

        /// <summary>
        /// Return the specified finger of the specificed hand
        /// 
        /// Shorthand for hand.Fingers[(int)fingerType].Bones[],
        /// </summary>
        public static Finger GetFinger(this Hand hand, Finger.FingerType fingerType)
        {
            return hand.fingers[(int)fingerType];
        }

        /// <summary>
        /// Returns a Pose consisting of the tracked hand's palm position and rotation.
        /// </summary>
        public static Pose GetPalmPose(this Hand hand)
        {
            return new Pose(hand.PalmPosition, hand.Rotation);
        }

        /// <summary>
        /// As Hand.SetTransform(), but takes a Pose as input for convenience.
        /// </summary>
        public static void SetPalmPose(this Hand hand, Pose newPalmPose)
        {
            hand.SetTransform(newPalmPose.position, newPalmPose.rotation);
        }

        /// <summary>
        /// Returns the direction the Hand's palm is facing. For the  other two palm-basis
        /// directions, see RadialAxis and DistalAxis.
        /// 
        /// The direction out of the back of the hand would be called the dorsal axis.
        /// </summary>
        public static Vector3 PalmarAxis(this Hand hand)
        {
            return -hand.Basis.yBasis;
        }

        /// <summary>
        /// Returns the the direction towards the thumb that is perpendicular to the palmar
        /// and distal axes. Left and right hands will return opposing directions.
        /// 
        /// The direction away from the thumb would be called the ulnar axis.
        /// </summary>
        public static Vector3 RadialAxis(this Hand hand)
        {
            if (hand.IsRight)
            {
                return -hand.Basis.xBasis;
            }
            else
            {
                return hand.Basis.xBasis;
            }
        }

        /// <summary>
        /// Returns the direction towards the fingers that is perpendicular to the palmar
        /// and radial axes.
        /// 
        /// The direction towards the wrist would be called the proximal axis.
        /// </summary>
        public static Vector3 DistalAxis(this Hand hand)
        {
            return hand.Basis.zBasis;
        }

        /// <summary>
        /// Returns whether the pinch strength for the hand is greater than 0.8.
        /// For more reliable pinch behavior, try applying hysteresis to the PinchDistance property.
        /// </summary>
        public static bool IsPinching(this Hand hand)
        {
            return hand.PinchDistance < 0.03f;
        }

        /// <summary>
        /// Returns approximately where the thumb and index finger will be if they are pinched together.
        /// </summary>
        public static Vector3 GetPinchPosition(this Hand hand)
        {
            Vector3 indexPosition = hand.fingers[(int)Finger.FingerType.INDEX].TipPosition;
            Vector3 thumbPosition = hand.fingers[(int)Finger.FingerType.THUMB].TipPosition;
            return (2 * thumbPosition + indexPosition) * 0.333333F;
        }

        /// <summary>
        /// Returns a decent approximation of where the hand is pinching, or where it will pinch,
        /// even if the index and thumb tips are far apart.
        /// 
        /// In general, this will be more stable than GetPinchPosition().
        /// </summary>
        public static Vector3 GetPredictedPinchPosition(this Hand hand)
        {
            Vector3 indexTip = hand.Index.TipPosition;
            Vector3 thumbTip = hand.Thumb.TipPosition;

            // The predicted pinch point is a rigid point in hand-space linearly offset by the
            // index finger knuckle position, scaled by the index finger's length, and lightly
            // influenced by the actual thumb and index tip positions.
            Vector3 indexKnuckle = hand.fingers[1].bones[1].PrevJoint;
            float indexLength = hand.fingers[1].Length;
            Vector3 radialAxis = hand.RadialAxis();
            float thumbInfluence = Vector3.Dot((thumbTip - indexKnuckle).normalized, radialAxis).Map(0F, 1F, 0.5F, 0F);
            Vector3 predictedPinchPoint = indexKnuckle + hand.PalmarAxis() * indexLength * 0.85F
                                                       + hand.DistalAxis() * indexLength * 0.20F
                                                       + radialAxis * indexLength * 0.20F;
            predictedPinchPoint = Vector3.Lerp(predictedPinchPoint, thumbTip, thumbInfluence);
            predictedPinchPoint = Vector3.Lerp(predictedPinchPoint, indexTip, 0.15F);

            return predictedPinchPoint;
        }

        /// <summary>
        /// Predicted Pinch Position without influence from the thumb or index tip.
        /// Useful for calculating extremely stable pinch calculations.
        /// Not good for visualising the pinch point - recommended to use PredictedPinchPosition instead
        /// </summary>
        public static Vector3 GetStablePinchPosition(this Hand hand)
        {
            // The stable pinch point is a rigid point in hand-space linearly offset by the
            // index finger knuckle position and scaled by the index finger's length

            Vector3 indexKnuckle = hand.fingers[1].bones[1].PrevJoint;
            float indexLength = hand.fingers[1].Length;
            Vector3 radialAxis = hand.RadialAxis();
            Vector3 stablePinchPoint = indexKnuckle + hand.PalmarAxis() * indexLength * 0.85F
                                                       + hand.DistalAxis() * indexLength * 0.20F
                                                       + radialAxis * indexLength * 0.20F;
            return stablePinchPoint;
        }

        /// <summary>
        /// Returns whether this vector faces from a given world position towards another world position within a maximum angle of error.
        /// </summary>
        public static bool IsFacing(this Vector3 facingVector, Vector3 fromWorldPosition, Vector3 towardsWorldPosition, float maxOffsetAngleAllowed)
        {
            Vector3 actualVectorTowardsWorldPosition = (towardsWorldPosition - fromWorldPosition).normalized;
            return Vector3.Angle(facingVector, actualVectorTowardsWorldPosition) <= maxOffsetAngleAllowed;
        }

        /// <summary>
        /// Returns a confidence value from 0 to 1 indicating how strongly the Hand is making a fist.
        /// </summary>
        public static float GetFistStrength(this Hand hand)
        {
            if (hand == null)
            {
                return 0;
            }

            return (Vector3.Dot(hand.fingers[1].Direction, -hand.DistalAxis())
                    + Vector3.Dot(hand.fingers[2].Direction, -hand.DistalAxis())
                    + Vector3.Dot(hand.fingers[3].Direction, -hand.DistalAxis())
                    + Vector3.Dot(hand.fingers[4].Direction, -hand.DistalAxis())
                    + Vector3.Dot(hand.fingers[0].Direction, -hand.RadialAxis())
                    ).Map(-5, 5, 0, 1);
        }

        /// <summary>
        /// Returns a confidence value from 0 to 1 indicating how strongly a finger is curled.
        /// </summary>
        public static float GetFingerStrength(this Hand hand, int finger)
        {
            if (hand == null)
            {
                return 0;
            }

            if (finger == 0)
            {
                return Vector3.Dot(hand.fingers[finger].Direction, -hand.RadialAxis()).Map(-1, 1, 0, 1);
            }

            return Vector3.Dot(hand.fingers[finger].Direction, -hand.DistalAxis()).Map(-1, 1, 0, 1);
        }

        /// <summary>
        /// Returns the distance between the tip of the finger and the tip of the thumb.
        /// Finger 0 (thumb) will always return float.MaxValue.
        /// </summary>
        public static float GetFingerPinchDistance(this Hand hand, int finger)
        {
            if (hand == null || finger == 0)
            {
                return float.MaxValue;
            }

            return Vector3.Distance(hand.fingers[0].TipPosition, hand.fingers[finger].TipPosition);
        }

        /// <summary>
        /// Returns the Chirality of the hand
        /// </summary>
        public static Chirality GetChirality(this Hand hand)
        {
            return hand.IsLeft ? Chirality.Left : Chirality.Right;
        }

        // Magic numbers for palm width and PinchStrength calculation
        private static readonly float[] DefaultMetacarpalLengths = { 0, 0.06812f, 0.06460f, 0.05800f, 0.05369f };

        /// <summary>
        /// Returns a relative scale to a default scale. Can be used to calculate palm width and pinch strength
        /// </summary>
        public static float CalculateHandScale(ref Hand hand)
        {
            // Iterate through the fingers, skipping the thumb and accumulate the scale.
            float scale = 0.0f;
            for (var i = 1; i < hand.fingers.Length; ++i)
            {
                scale += (hand.fingers[i].GetBone(Bone.BoneType.METACARPAL).Length / DefaultMetacarpalLengths[i]) / 4.0f;
            }

            return scale;
        }

        /// <summary>
        /// Returns a pinch strength for the hand based on the provided joint data. Value ranges from 0 to 1 where 1 is fully pinched.
        /// 
        /// Only use this where the pinch strength has not already been provided. Alternatively, use the provided Hand.PinchStrength.
        /// </summary>
        public static float CalculatePinchStrength(ref Hand hand)
        {
            // Get the thumb position.
            Vector3 thumbTipPosition = hand.Thumb.TipPosition;

            // Compute the distance midpoints between the thumb and the each finger and find the smallest.
            float minDistanceSquared = float.MaxValue;

            // Iterate through the fingers, skipping the thumb.
            for (var i = 1; i < hand.fingers.Length; ++i)
            {
                float distanceSquared = (hand.fingers[i].TipPosition - thumbTipPosition).sqrMagnitude;
                minDistanceSquared = Mathf.Min(distanceSquared, minDistanceSquared);
            }

            float scale = CalculateHandScale(ref hand);

            // Compute the pinch strength. Magic values taken from existing LeapC implementation (scaled to metres)
            float distanceZero = 0.0600f * scale;
            float distanceOne = 0.0220f * scale;
            return Mathf.Clamp01((Mathf.Sqrt(minDistanceSquared) - distanceZero) / (distanceOne - distanceZero));
        }

        /// <summary>
        /// Returns a pinch distance (in m) for the hand based on the provided joint data.
        /// 
        /// Only use this where the pinch distance has not already been provided. Alternatively, use the provided Hand.PinchDistance.
        /// </summary>
        public static float CalculatePinchDistance(ref Hand hand)
        {
            // Get the farthest 2 segments of thumb and index finger, respectively, and compute distances.
            float minDistanceSquared = float.MaxValue;
            for (var thumbBoneIndex = 2; thumbBoneIndex < hand.Thumb.bones.Length; ++thumbBoneIndex)
            {
                for (var indexBoneIndex = 2; indexBoneIndex < hand.Index.bones.Length; ++indexBoneIndex)
                {
                    var distanceSquared = CalculateBoneDistanceSquared(
                        hand.Thumb.bones[thumbBoneIndex],
                        hand.Index.bones[indexBoneIndex]);
                    minDistanceSquared = Mathf.Min(distanceSquared, minDistanceSquared);
                }
            }

            // Return the pinch distance, converted to millimeters to match other providers.
            return Mathf.Sqrt(minDistanceSquared);
        }

        static float CalculateBoneDistanceSquared(Bone boneA, Bone boneB)
        {
            // Denormalize directions to bone length.
            Vector3 boneAJoint = boneA.PrevJoint;
            Vector3 boneBJoint = boneB.PrevJoint;
            Vector3 boneADirection = boneA.Direction * boneA.Length;
            Vector3 boneBDirection = boneB.Direction * boneB.Length;

            // Compute the minimum (squared) distance between two bones.
            Vector3 diff = boneBJoint - boneAJoint;
            float d1 = Vector3.Dot(boneADirection, diff);
            float d2 = Vector3.Dot(boneBDirection, diff);
            float a = boneADirection.sqrMagnitude;
            float b = Vector3.Dot(boneADirection, boneBDirection);
            float c = boneBDirection.sqrMagnitude;
            float det = b * b - a * c;
            float t1 = Mathf.Clamp01((b * d2 - c * d1) / det);
            float t2 = Mathf.Clamp01((a * d2 - b * d1) / det);
            Vector3 pa = boneAJoint + t1 * boneADirection;
            Vector3 pb = boneBJoint + t2 * boneBDirection;
            return (pa - pb).sqrMagnitude;
        }

        /// <summary>
        /// Returns a grab strength for the hand based on the provided joint data. Value ranges from 0 to 1 where 1 is fully grabbed.
        /// 
        /// Only use this where the grab strength has not already been provided. Alternatively, use the provided Hand.GrabStrength.
        /// </summary>
        public static float CalculateGrabStrength(ref Hand hand)
        {
            // magic numbers so it approximately lines up with the leap results
            const float bendZero = 0.25f;
            const float bendOne = 0.85f;

            // Find the minimum bend angle for the non-thumb fingers.
            float minBend = float.MaxValue;
            for (int finger_idx = 1; finger_idx < 5; finger_idx++)
            {
                minBend = Mathf.Min(hand.GetFingerStrength(finger_idx), minBend);
            }

            // Return the grab strength.
            return Mathf.Clamp01((minBend - bendZero) / (bendOne - bendZero));
        }

        /// <summary>
        /// Returns an unsmoothed ray representing the general reaching/interaction intent direction.
        /// </summary>
        public static Ray HandRay(this Hand hand, Transform headTransform)
        {
            Quaternion shoulderYaw = Quaternion.Euler(0f, headTransform.rotation.eulerAngles.y, 0f);
            // Approximate shoulder position with magic values.
            Vector3 ProjectionOrigin = headTransform.position
                                        + (shoulderYaw * (new Vector3(0f, -0.13f, -0.1f)
                                        + Vector3.left * 0.15f * (hand.IsLeft ? 1f : -1f)));
            // Compare against this
            //Vector3 ProjectionOrigin    = headTransform.position + shoulderYaw * 
            //                                new Vector3(0.15f * (hand.IsLeft ? -1f : 1f), -0.13f, 0.05f);
            Vector3 ProjectionDirection = hand.fingers[1].bones[0].NextJoint - ProjectionOrigin;
            return new Ray(ProjectionOrigin, ProjectionDirection);
        }

        /// <summary>
        /// Transforms a bone by a position and rotation.
        /// </summary>
        public static void Transform(this Bone bone, Vector3 position, Quaternion rotation)
        {
            bone.Transform(new LeapTransform(position, rotation));
        }

        /// <summary>
        /// Transforms a finger by a position and rotation.
        /// </summary>
        public static void Transform(this Finger finger, Vector3 position, Quaternion rotation)
        {
            finger.Transform(new LeapTransform(position, rotation));
        }

        /// <summary>
        /// Transforms a hand by a position and rotation.
        /// </summary>
        public static void Transform(this Hand hand, Vector3 position, Quaternion rotation)
        {
            hand.Transform(new LeapTransform(position, rotation));
        }

        /// <summary>
        /// Transforms a frame by a position and rotation.
        /// </summary>
        public static void Transform(this Frame frame, Vector3 position, Quaternion rotation)
        {
            frame.Transform(new LeapTransform(position, rotation));
        }

        /// <summary>
        /// Transforms a bone to a position and rotation.
        /// </summary>
        public static void SetTransform(this Bone bone, Vector3 position, Quaternion rotation)
        {
            bone.Transform(Vector3.zero, (rotation * Quaternion.Inverse(bone.Rotation)));
            bone.Transform(position - bone.PrevJoint, Quaternion.identity);
        }

        /// <summary>
        /// Transforms a finger to a position and rotation by its fingertip.
        /// </summary>
        public static void SetTipTransform(this Finger finger, Vector3 position, Quaternion rotation)
        {
            finger.Transform(Vector3.zero, (rotation * Quaternion.Inverse(finger.bones[3].Rotation)));
            finger.Transform(position - finger.bones[3].NextJoint, Quaternion.identity);
        }

        /// <summary>
        /// Transforms a hand to a position and rotation.
        /// </summary>
        public static void SetTransform(this Hand hand, Vector3 position, Quaternion rotation)
        {
            hand.Transform(Vector3.zero, Quaternion.Slerp((rotation * Quaternion.Inverse(hand.Rotation)), Quaternion.identity, 0f));
            hand.Transform(position - hand.PalmPosition, Quaternion.identity);
        }

    }

    /// <summary>
    /// Utility methods for constructing and manipulating Leap hand object data.
    /// </summary>
    public static class HandUtils
    {
        /// <summary>
        /// Fills the Hand object with the provided hand data. You can pass null for the
        /// fingers input; this will leave the hand's finger data unmodified.
        /// </summary>
        public static void Fill(this Hand toFill,
                                long frameID,
                                int id,
                                float confidence,
                                float grabStrength,
                                float pinchStrength,
                                float pinchDistance,
                                float palmWidth,
                                bool isLeft,
                                float timeVisible,
                                /* Arm arm,*/
                                Finger[] fingers,
                                Vector3 palmPosition,
                                Vector3 stabilizedPalmPosition,
                                Vector3 palmVelocity,
                                Vector3 palmNormal,
                                Quaternion rotation,
                                Vector3 direction,
                                Vector3 wristPosition)
        {
            toFill.FrameId = frameID;
            toFill.Id = id;
            toFill.Confidence = confidence;
            toFill.GrabStrength = grabStrength;
            toFill.PinchStrength = pinchStrength;
            toFill.PinchDistance = pinchDistance;
            toFill.PalmWidth = palmWidth;
            toFill.IsLeft = isLeft;
            toFill.TimeVisible = timeVisible;
            if (fingers != null) toFill.fingers = fingers;
            toFill.PalmPosition = palmPosition;
            toFill.StabilizedPalmPosition = stabilizedPalmPosition;
            toFill.PalmVelocity = palmVelocity;
            toFill.PalmNormal = palmNormal;
            toFill.Rotation = rotation;
            toFill.Direction = direction;
            toFill.WristPosition = wristPosition;
        }

        /// <summary>
        /// Fills the Bone object with the provided bone data.
        /// </summary>
        public static void Fill(this Bone toFill,
                                Vector3 prevJoint,
                                Vector3 nextJoint,
                                Vector3 center,
                                Vector3 direction,
                                float length,
                                float width,
                                Bone.BoneType type,
                                Quaternion rotation)
        {
            toFill.PrevJoint = prevJoint;
            toFill.NextJoint = nextJoint;
            toFill.Center = center;
            toFill.Direction = direction;
            toFill.Length = length;
            toFill.Width = width;
            toFill.Type = type;
            toFill.Rotation = rotation;
        }

        /// <summary>
        /// Fills the Finger object with the provided finger data. You can pass null for
        /// bones; A null bone will not modify the underlying hand's data for that bone.
        /// </summary>
        public static void Fill(this Finger toFill,
                                long frameId,
                                int handId,
                                int fingerId,
                                float timeVisible,
                                Vector3 tipPosition,
                                Vector3 direction,
                                float width,
                                float length,
                                bool isExtended,
                                Finger.FingerType type,
                                Bone metacarpal = null,
                                Bone proximal = null,
                                Bone intermediate = null,
                                Bone distal = null)
        {
            toFill.Id = handId;
            toFill.HandId = handId;
            toFill.TimeVisible = timeVisible;
            toFill.TipPosition = tipPosition;
            toFill.Direction = direction;
            toFill.Width = width;
            toFill.Length = length;
            toFill.IsExtended = isExtended;
            toFill.Type = type;

            if (metacarpal != null) toFill.bones[0] = metacarpal;
            if (proximal != null) toFill.bones[1] = proximal;
            if (intermediate != null) toFill.bones[2] = intermediate;
            if (distal != null) toFill.bones[3] = distal;
        }

        /// <summary>
        /// Fills the Arm object with the provided arm data.
        /// </summary>
        public static void Fill(this Arm toFill,
                                Vector3 elbow,
                                Vector3 wrist,
                                Vector3 center,
                                Vector3 direction,
                                float length,
                                float width,
                                Quaternion rotation)
        {
            toFill.PrevJoint = elbow;
            toFill.NextJoint = wrist;
            toFill.Center = center;
            toFill.Direction = direction;
            toFill.Length = length;
            toFill.Width = width;
            toFill.Rotation = rotation;
        }

        /// <summary>
        /// Fills the hand's PalmVelocity data based on the
        /// previous hand object and the provided delta time between the two hands.
        /// </summary>
        public static void FillTemporalData(this Hand toFill,
                                            Hand previousHand, float deltaTime)
        {
            toFill.PalmVelocity = (toFill.PalmPosition - previousHand.PalmPosition)
                                   / deltaTime;
        }

        #region Frame Utils

        /// <summary>
        /// Finds a hand in the given frame.
        /// </summary>
        /// <returns>The first hand of the argument whichHand found in the argument frame.</returns>
        public static Hand GetHand(this Frame frame, Chirality whichHand)
        {
            if (frame == null || frame.Hands == null)
            {
                return null;
            }

            foreach (var hand in frame.Hands)
            {
                if (hand.IsLeft && whichHand == Chirality.Left || hand.IsRight && whichHand == Chirality.Right)
                {
                    return hand;
                }
            }

            return null;
        }

        #endregion

        #region Provider Utils

        /// <summary>
        /// Finds a hand in the current frame.
        /// </summary>
        /// <returns>The first hand of the argument whichHand found in the current frame of 
        /// the argument provider.</returns>
        [Obsolete("Naming updated. Use LeapProvider.GetHand() instead")]
        public static Hand Get(this LeapProvider provider, Chirality whichHand)
        {
            Frame frame;
            if (Time.inFixedTimeStep)
            {
                frame = provider.CurrentFixedFrame;
            }
            else
            {
                frame = provider.CurrentFrame;
            }

            return frame.GetHand(whichHand);
        }

        /// <summary>
        /// Finds a hand in the current frame.
        /// </summary>
        /// <returns>The first hand of the argument whichHand found in the current frame of 
        /// the argument provider.</returns>
        public static Hand GetHand(this LeapProvider provider, Chirality whichHand)
        {
            if (Time.inFixedTimeStep)
            {
                return provider.CurrentFixedFrame.GetHand(whichHand);
            }
            else
            {
                return provider.CurrentFrame.GetHand(whichHand);
            }
        }

        #endregion

        #region Misc Utils

        public static string FingerIndexToName(int fingerIndex)
        {
            switch (fingerIndex)
            {
                case 0:
                    return "Thumb";
                case 1:
                    return "Index";
                case 2:
                    return "Middle";
                case 3:
                    return "Ring";
                case 4:
                    return "Pinky";
            }
            return "";
        }

        public static string JointIndexToName(int jointIndex)
        {
            switch (jointIndex)
            {
                case 0:
                    return "Metacarpal";
                case 1:
                    return "Proximal";
                case 2:
                    return "Intermediate";
                case 3:
                    return "Distal";
            }
            return "";
        }

        #endregion
    }
}