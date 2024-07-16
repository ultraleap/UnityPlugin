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
    public class NoContactHand : ContactHand
    {
        protected override void ProcessOutputHand(ref Hand modifiedHand)
        {
            modifiedHand.CopyFrom(dataHand);
        }

        internal override void BeginHand(Hand hand)
        {
            gameObject.SetActive(true);
            _oldDataPosition = hand.PalmPosition;
            _oldDataRotation = hand.Rotation;
            _velocity = Vector3.zero;
            _angularVelocity = Vector3.zero;
            tracked = true;
        }

        internal override void FinishHand()
        {
            tracked = false;
            _velocity = Vector3.zero;
            _angularVelocity = Vector3.zero;
            gameObject.SetActive(false);
        }

        internal override void GenerateHandLogic()
        {
            GenerateHandObjects(typeof(NoContactBone));
            isHandPhysical = false;

            foreach (var bone in bones)
            {
                bone.boneCollider.isTrigger = true;
            }

            palmBone.palmCollider.isTrigger = true;
        }

        internal override void PostFixedUpdateHandLogic()
        {
            // Don't need to do anything here
        }

        protected override void UpdateHandLogic(Hand hand)
        {
            _velocity = ContactUtils.ToLinearVelocity(_oldDataPosition, hand.PalmPosition, Time.fixedDeltaTime);
            _angularVelocity = ContactUtils.ToAngularVelocity(_oldDataRotation, hand.Rotation, Time.fixedDeltaTime);
        }
    }
}