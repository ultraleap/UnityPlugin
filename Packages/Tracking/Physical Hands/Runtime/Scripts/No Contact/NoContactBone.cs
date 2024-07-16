/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.PhysicalHands
{
    public class NoContactBone : ContactBone
    {
        internal override void UpdatePalmBone(Hand hand)
        {
            transform.position = hand.PalmPosition;
            transform.rotation = hand.Rotation;
            width = hand.PalmWidth;
            tipPosition = hand.CalculateAverageKnucklePosition();
            length = Vector3.Distance(tipPosition, hand.WristPosition);
            ContactUtils.SetupPalmCollider(palmCollider, palmEdgeColliders, hand);
        }

        internal override void UpdateBone(Bone prevBone, Bone bone)
        {
            transform.position = bone.PrevJoint;
            transform.rotation = bone.Rotation;
            tipPosition = bone.NextJoint;
            width = bone.Width;
            length = bone.Length;
            ContactUtils.SetupBoneCollider(boneCollider, bone);
        }

        private void OnDrawGizmos()
        {
            Debug.DrawLine(transform.position, tipPosition, Color.cyan);
        }
    }
}