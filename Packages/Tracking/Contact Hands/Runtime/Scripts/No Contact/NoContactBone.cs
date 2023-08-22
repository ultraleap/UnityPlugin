using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.ContactHands
{
    public class NoContactBone : ContactBone
    {
        internal override void UpdatePalmBone(Hand hand)
        {
            transform.position = hand.PalmPosition;
            transform.rotation = hand.Rotation;
            width = hand.PalmWidth;
            palmThickness = hand.Fingers[2].Bone(0).Width;
            tipPosition = hand.CalculateAverageKnucklePosition();
            wristPosition = hand.WristPosition;
            length = Vector3.Distance(tipPosition, wristPosition);
            ContactUtils.SetupPalmCollider(palmCollider, hand);
        }

        internal override void UpdateBone(Bone bone)
        {
            transform.position = bone.PrevJoint;
            transform.rotation = bone.Rotation;
            tipPosition = bone.NextJoint;
            width = bone.Width;
            length = bone.Length;
            ContactUtils.SetupBoneCollider(boneCollider, bone);
        }

        internal override void PostFixedUpdateBone()
        {

        }

        private void OnDrawGizmos()
        {
            
            //Debug.DrawLine(transform.position,tipPosition,Color.cyan);
        }
    }
}