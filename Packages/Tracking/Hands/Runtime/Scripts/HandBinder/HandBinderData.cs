/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2021.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.HandsModule
{
    /// <summary>
    /// A data structure to define all the fingers in a hand, the wrist and elbow
    /// </summary>
    [System.Serializable]
    public class BoundHand
    {
        public BoundFinger[] fingers = new BoundFinger[5];
        public BoundBone wrist = new BoundBone();
        public BoundBone elbow = new BoundBone();
        public float baseScale;
        public Vector3 startScale;
        [Range(-1, 3)] public float scaleOffset = 1;
        [Range(-1, 3)] public float elbowOffset = 1;
    }

    /// <summary>
    /// A data structure to define a finger
    /// </summary>
    [System.Serializable]
    public class BoundFinger
    {
        public BoundBone[] boundBones = new BoundBone[4];
        public float fingerTipBaseLength;
        [Range(-1, 3)] public float fingerTipScaleOffset = 1;
    }

    /// <summary>
    /// A data structure to define starting position, an offset and the Transform reference found in the scene
    /// </summary>
    [System.Serializable]
    public class BoundBone
    {
        public Transform boundTransform;
        public TransformStore startTransform = new TransformStore();
        public TransformStore offset = new TransformStore();
    }

    /// <summary>
    /// A data structure to store a transforms position and rotation
    /// </summary>
    [System.Serializable]
    public class TransformStore
    {
        public Vector3 position = Vector3.zero;
        public Vector3 rotation = Vector3.zero;
        public Vector3 scale = Vector3.zero;
    }

    /// <summary>
    /// A data structure to store information about a transform and a Gameobject
    /// </summary>
    [System.Serializable]
    public class SerializedTransform
    {
        public TransformStore transform;
        public GameObject reference;
    }

    /// <summary>
    /// ENUM types for bones of the hand the hand binder can attach to
    /// </summary>
    public enum BoundTypes
    {
        THUMB_METACARPAL,
        THUMB_PROXIMAL,
        THUMB_INTERMEDIATE,
        THUMB_DISTAL,

        INDEX_METACARPAL,
        INDEX_PROXIMAL,
        INDEX_INTERMEDIATE,
        INDEX_DISTAL,

        MIDDLE_METACARPAL,
        MIDDLE_PROXIMAL,
        MIDDLE_INTERMEDIATE,
        MIDDLE_DISTAL,

        RING_METACARPAL,
        RING_PROXIMAL,
        RING_INTERMEDIATE,
        RING_DISTAL,

        PINKY_METACARPAL,
        PINKY_PROXIMAL,
        PINKY_INTERMEDIATE,
        PINKY_DISTAL,

        WRIST,
        ELBOW,
    }

    public static class HandBinderUtilities
    {

        /// <summary>
        /// The mapping that allows a BoundType and Leap FingerType/BoneType to map back to the HandBinders Data structure
        /// </summary>
        public readonly static Dictionary<BoundTypes, (Finger.FingerType, Bone.BoneType)> boundTypeMapping = new Dictionary<BoundTypes, (Finger.FingerType, Bone.BoneType)>
            {
            {BoundTypes.THUMB_METACARPAL, (Finger.FingerType.TYPE_THUMB, Bone.BoneType.TYPE_METACARPAL)},
            {BoundTypes.THUMB_PROXIMAL, (Finger.FingerType.TYPE_THUMB, Bone.BoneType.TYPE_PROXIMAL)},
            {BoundTypes.THUMB_INTERMEDIATE, (Finger.FingerType.TYPE_THUMB, Bone.BoneType.TYPE_INTERMEDIATE)},
            {BoundTypes.THUMB_DISTAL, (Finger.FingerType.TYPE_THUMB, Bone.BoneType.TYPE_DISTAL)},
            {BoundTypes.INDEX_METACARPAL, (Finger.FingerType.TYPE_INDEX, Bone.BoneType.TYPE_METACARPAL)},
            {BoundTypes.INDEX_PROXIMAL, (Finger.FingerType.TYPE_INDEX, Bone.BoneType.TYPE_PROXIMAL)},
            {BoundTypes.INDEX_INTERMEDIATE, (Finger.FingerType.TYPE_INDEX, Bone.BoneType.TYPE_INTERMEDIATE)},
            {BoundTypes.INDEX_DISTAL, (Finger.FingerType.TYPE_INDEX, Bone.BoneType.TYPE_DISTAL)},
            {BoundTypes.MIDDLE_METACARPAL, (Finger.FingerType.TYPE_MIDDLE, Bone.BoneType.TYPE_METACARPAL)},
            {BoundTypes.MIDDLE_PROXIMAL, (Finger.FingerType.TYPE_MIDDLE, Bone.BoneType.TYPE_PROXIMAL)},
            {BoundTypes.MIDDLE_INTERMEDIATE, (Finger.FingerType.TYPE_MIDDLE, Bone.BoneType.TYPE_INTERMEDIATE)},
            {BoundTypes.MIDDLE_DISTAL, (Finger.FingerType.TYPE_MIDDLE, Bone.BoneType.TYPE_DISTAL)},
            {BoundTypes.RING_METACARPAL, (Finger.FingerType.TYPE_RING, Bone.BoneType.TYPE_METACARPAL)},
            {BoundTypes.RING_PROXIMAL, (Finger.FingerType.TYPE_RING, Bone.BoneType.TYPE_PROXIMAL)},
            {BoundTypes.RING_INTERMEDIATE, (Finger.FingerType.TYPE_RING, Bone.BoneType.TYPE_INTERMEDIATE)},
            {BoundTypes.RING_DISTAL, (Finger.FingerType.TYPE_RING, Bone.BoneType.TYPE_DISTAL)},
            {BoundTypes.PINKY_METACARPAL, (Finger.FingerType.TYPE_PINKY, Bone.BoneType.TYPE_METACARPAL)},
            {BoundTypes.PINKY_PROXIMAL, (Finger.FingerType.TYPE_PINKY, Bone.BoneType.TYPE_PROXIMAL)},
            {BoundTypes.PINKY_INTERMEDIATE, (Finger.FingerType.TYPE_PINKY, Bone.BoneType.TYPE_INTERMEDIATE)},
            {BoundTypes.PINKY_DISTAL, (Finger.FingerType.TYPE_PINKY, Bone.BoneType.TYPE_DISTAL)},
        };


        public static Hand GenerateLeapHand(this BoundHand boundHand, Hand leapHand, float fingerTipScale = 0.8f)
        {
            if (leapHand == null)
                return null;

            //Loop through all the fingers of the hand to calculate where the leap data should be in relation to the Bound Hand
            for (int fingerID = 0; fingerID < leapHand.Fingers.Count; fingerID++)
            {
                var finger = leapHand.Fingers[fingerID];

                for (int boneID = 0; boneID < finger.bones.Length; boneID++)
                {
                    var leapBone = finger.bones[boneID];

                    //If this bone is the distal bone, calculate a finger tip position
                    if (boneID == (int)Bone.BoneType.TYPE_DISTAL)
                    {
                        var thisBone = boundHand.fingers[fingerID].boundBones[boneID];
                        var prevBone = boundHand.fingers[fingerID].boundBones[boneID - 1];

                        if (prevBone.boundTransform && thisBone.boundTransform)
                        {
                            var nextJoint = thisBone.boundTransform.position.ToVector();
                            var prevJoint = prevBone.boundTransform.position.ToVector();

                            var dir = (prevJoint - nextJoint);
                            var length = dir.Magnitude;

                            prevJoint += -dir.Normalized * (length * fingerTipScale);
                            nextJoint += -dir.Normalized * (length * fingerTipScale);

                            var center = Vector.Lerp(prevJoint, nextJoint, 0.5f);

                            finger.bones[boneID] = new Bone(prevJoint, nextJoint, center, dir, length, leapBone.Width, leapBone.Type, leapBone.Rotation);
                            finger.TipPosition = nextJoint;
                        }
                    }
                    else
                    {
                        var prevBone = boundHand.fingers[fingerID].boundBones[boneID];
                        var nextBone = boundHand.fingers[fingerID].boundBones[boneID + 1];

                        if (prevBone.boundTransform && nextBone.boundTransform)
                        {
                            var prevJoint = prevBone.boundTransform.position.ToVector();
                            var nextJoint = nextBone.boundTransform.position.ToVector();
                            var dir = (prevJoint - nextJoint);
                            var length = dir.Magnitude;
                            var center = Vector.Lerp(prevJoint, nextJoint, 0.5f);

                            finger.bones[boneID] = new Bone(prevJoint, nextJoint, center, dir, length, leapBone.Width, leapBone.Type, leapBone.Rotation);
                        }

                        else if (boneID == (int)Bone.BoneType.TYPE_METACARPAL)
                        {
                            var proximal  = boundHand.fingers[fingerID].boundBones[(int)Bone.BoneType.TYPE_PROXIMAL];
                            var wristBone = boundHand.wrist.boundTransform;

                            if (proximal.boundTransform && wristBone)
                            {
                                var nextJoint = proximal.boundTransform.position.ToVector();
                                var prevJoint = Vector.Lerp(wristBone.position.ToVector(), nextJoint, 0.5f);
                                var dir = (prevJoint - nextJoint);
                                var length = dir.Magnitude;
                                var center = Vector.Lerp(prevJoint, nextJoint, 0.5f);
                                finger.bones[boneID] = new Bone(prevJoint, nextJoint, center, dir, length, leapBone.Width, leapBone.Type, leapBone.Rotation);
                            }
                        }
                    }
                }
            }

            if (boundHand.wrist.boundTransform != null)
            {
                leapHand.WristPosition = boundHand.wrist.boundTransform.position.ToVector();

                if (boundHand.elbow.boundTransform != null)
                {
                    var elbowPos = boundHand.elbow.boundTransform.position.ToVector();
                    var wristPos = boundHand.wrist.boundTransform.position.ToVector();
                    var center = Vector.Lerp(elbowPos, wristPos, 0.5f);
                    var dir = (elbowPos - wristPos);
                    var length = dir.Magnitude;

                    leapHand.Arm = new Arm(elbowPos, wristPos, center, dir, length, leapHand.Arm.Width, leapHand.Arm.Rotation);
                    leapHand.Arm.PrevJoint = elbowPos;
                }
            }

            var palmPos = Vector.Lerp(leapHand.WristPosition, leapHand.GetMiddle().bones[(int)Bone.BoneType.TYPE_PROXIMAL].PrevJoint, 0.5f);
            leapHand.PalmPosition = palmPos;
            leapHand.StabilizedPalmPosition = leapHand.PalmPosition;
            leapHand.PalmWidth = (leapHand.GetPinky().bones[1].PrevJoint - leapHand.GetIndex().bones[1].PrevJoint).Magnitude;
            leapHand.Arm.NextJoint = leapHand.WristPosition;

            return leapHand;
        }
    }
}