using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.ContactHands
{
    public class NoContactHand : ContactHand
    {
        protected override void ProcessOutputHand()
        {
            // Don't need to modify the hand
        }

        internal override void BeginHand(Hand hand)
        {
            modifiedHand.CopyFrom(hand);
            tracked = true;
        }

        internal override void FinishHand()
        {
            tracked = false;
        }

        internal override void GenerateHand()
        {
            GenerateHandObjects(typeof(NoContactBone));
        }

        internal override void PostFixedUpdateHand()
        {
        }

        internal override void UpdateHand(Hand hand)
        {
            modifiedHand.CopyFrom(hand);
            palmBone.UpdatePalmBone(hand);
            ContactBone tempBone;
            for (int i = 0; i < bones.Length - 1; i++)
            {
                tempBone = bones[i];
                tempBone.UpdateBone(hand.Fingers[tempBone.Finger].bones[tempBone.joint + 1]);
            }
        }
    }
}
