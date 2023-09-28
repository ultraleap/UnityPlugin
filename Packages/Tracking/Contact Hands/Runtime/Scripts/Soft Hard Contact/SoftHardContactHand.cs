using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Leap.Unity.ContactHands
{
    public class SoftHardContactHand : HardContactHand
    {
        protected override void Awake()
        {
            base.Awake();

            RESET_FRAME_COUNT = 1;
            TELEPORT_FRAME_COUNT = 2;
            RESET_FRAME_TELEPORT_COUNT = 2;
        }

        protected override void UpdateHandLogic(Hand hand)
        {
            if (!IsGrabbing && _wasGrabbing)
            {
                Collider[] colliders = GetComponentsInChildren<Collider>();

                foreach (var col in colliders)
                    col.isTrigger = false;

                ResetHand();
            }

            if (isGrabbing && !_wasGrabbing)
            {
                Collider[] colliders = GetComponentsInChildren<Collider>();

                foreach (var col in colliders)
                    col.isTrigger = true;
            }
            base.UpdateHandLogic(hand);
        }

        private void ResetHand()
        {
            ResetHardContactHand();
            // Don't need to wait for the hand to reset as much here
            _teleportFrameCount = 0;

            ghosted = true;
            _justGhosted = true;
        }

        protected override void ProcessOutputHand(ref Hand modifiedHand)
        {
            modifiedHand = modifiedHand.CopyFrom(dataHand);
        }
    }
}
