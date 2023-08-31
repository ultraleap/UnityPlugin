using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.ContactHands
{
    public class NoContactHand : ContactHand
    {
        private Vector3 _oldPosition;
        private Quaternion _oldRotation;

        protected override void ProcessOutputHand()
        {
            // Don't need to modify the hand
        }

        internal override void BeginHand(Hand hand)
        {
            gameObject.SetActive(true);
            modifiedHand.CopyFrom(hand);
            _oldPosition = hand.PalmPosition;
            _oldRotation = hand.Rotation;
            _velocity = Vector3.zero;
            _angularVelocity = Vector3.zero;
            tracked = true;
        }

        internal override void FinishHand()
        {
            tracked = false;
            gameObject.SetActive(false);
        }

        internal override void GenerateHandLogic()
        {
            GenerateHandObjects(typeof(NoContactBone));
            foreach(Transform t in transform)
            {
                // We don't want to be in contact with anything
                t.gameObject.layer = contactParent.contactManager.HandsResetLayer;
            }
            gameObject.SetActive(false);
            isHandPhysical = false;
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
            _velocity = ContactUtils.ToLinearVelocity(_oldPosition, hand.PalmPosition, Time.fixedDeltaTime);
            _angularVelocity = ContactUtils.ToAngularVelocity(_oldRotation, hand.Rotation, Time.fixedDeltaTime);
            _oldPosition = hand.PalmPosition;
            _oldRotation = hand.Rotation;
        }
    }
}
