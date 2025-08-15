/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2025.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.HandsModule;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.HandsModule
{
    public enum BoundFingerType
    {
        UNKNOWN,
        THUMB,
        INDEX,
        MIDDLE,
        RING,
        LITTLE
    }

    public enum BoundFingerBoneType
    {
        UNKNOWN,
        ELBOW,
        WRIST,
        METACARPAL,
        PROXIMAL,
        INTERMEDIATE,
        DISTAL,
        TIP
    }

    /// <summary>
    /// ENUM types for bones of the hand the hand binder can attach to
    /// </summary>
    public enum BoundBoneType
    {
        UNKNOWN,

        THUMB_METACARPAL,
        THUMB_PROXIMAL,
        THUMB_INTERMEDIATE,
        THUMB_DISTAL,
        THUMB_TIP,

        INDEX_METACARPAL,
        INDEX_PROXIMAL,
        INDEX_INTERMEDIATE,
        INDEX_DISTAL,
        INDEX_TIP,

        MIDDLE_METACARPAL,
        MIDDLE_PROXIMAL,
        MIDDLE_INTERMEDIATE,
        MIDDLE_DISTAL,
        MIDDLE_TIP,

        RING_METACARPAL,
        RING_PROXIMAL,
        RING_INTERMEDIATE,
        RING_DISTAL,
        RING_TIP,

        LITTLE_METACARPAL,
        LITTLE_PROXIMAL,
        LITTLE_INTERMEDIATE,
        LITTLE_DISTAL,
        LITTLE_TIP,

        WRIST,
        ELBOW,
    }

    /// <summary>
    /// Defines how live data maps to bones in a rig (skinned mesh renderer)
    /// </summary>
    public class BindingMap
    {
        public Transform RootHandOrArmJoint;
        public BoundBoneType RootBoneJointType;

        public Dictionary<BoundBoneType, BoundBoneData> BoundTransformMap = new Dictionary<BoundBoneType, BoundBoneData>();

        // Indexer declaration.
        // If index is out of range, the temps array will throw the exception.
        public BoundBoneData this[BoundBoneType index]
        {
            get => BoundTransformMap[index];
            set => BoundTransformMap[index] = value;
        }

        BoundBoneData GetBoundJointData(BoundBoneType targetJoint)
        {
            return BoundTransformMap[targetJoint];
        }
    }

    [Serializable]
    public class BoundBoneData
    {
        public readonly TransformStore OriginalTransform;
        public readonly Transform BoundSkinnedMeshRendererTransform;
        public readonly BoundBoneType BoneType;

        public BoundBoneData(Transform boundSkinnedMeshRendererTransform, BoundBoneType type)
        {
            this.BoundSkinnedMeshRendererTransform = boundSkinnedMeshRendererTransform;
            this.BoneType = type;
        }
    }
}
