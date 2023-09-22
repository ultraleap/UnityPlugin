using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Leap.Unity.ContactHands
{
    public class SoftHardContactHand : HardContactHand
    {
        protected override void UpdateHandLogic(Hand hand)
        {
            if (!IsGrabbing && _wasGrabbing)
            {
                Collider[] colliders = GetComponentsInChildren<Collider>();

                foreach (var col in colliders)
                    col.isTrigger = false;

                // TODO: do teleporting here because the collider might be inside the object
            }

            if (isGrabbing && !_wasGrabbing)
            {
                Collider[] colliders = GetComponentsInChildren<Collider>();

                foreach (var col in colliders)
                    col.isTrigger = true;
            }

            base.UpdateHandLogic(hand);
        }

        protected override void ProcessOutputHand(ref Hand modifiedHand)
        {
            modifiedHand = modifiedHand.CopyFrom(dataHand);
            return;
        }
    }
}
