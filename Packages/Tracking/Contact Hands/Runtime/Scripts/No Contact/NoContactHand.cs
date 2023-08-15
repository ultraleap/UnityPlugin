using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.ContactHands
{
    public class NoContactHand : ContactHand
    {
        internal override void GenerateHand()
        {
            GenerateHandObjects(typeof(NoContactBone));
        }

        internal override void PostFixedUpdateHand()
        {
        }

        internal override void UpdateHand(Hand hand)
        {
            bones[bones.Length - 1].UpdatePalmBone(hand);
            ContactBone tempBone;
            for (int i = 0; i < bones.Length - 1; i++)
            {
                tempBone = bones[i];
                tempBone.UpdateBone(hand.Fingers[tempBone.Finger].bones[tempBone.joint + 1]);
            }
        }
    }
}
