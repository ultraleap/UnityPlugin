using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.ContactHands
{
    public class NoContactParent : ContactParent
    {
        internal override void GenerateHands()
        {
            GameObject handObject = new GameObject("Left No Contact Hand", typeof(NoContactHand));
            handObject.transform.parent = transform;
            leftHand = handObject.GetComponent<NoContactHand>();
            leftHand.handedness = Chirality.Left;
            leftHand.GenerateHand();

            handObject = new GameObject("Right No Contact Hand", typeof(NoContactHand));
            handObject.transform.parent = transform;
            rightHand = handObject.GetComponent<NoContactHand>();
            rightHand.handedness = Chirality.Right;
            rightHand.GenerateHand();
        }

        internal override void PostFixedUpdateFrame()
        {
        }
    }
}