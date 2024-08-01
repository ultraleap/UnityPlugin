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
    public class SoftContactHand : ContactHand
    {
        protected override void ProcessOutputHand(ref Hand modifiedHand)
        {
            modifiedHand.CopyFrom(dataHand);

            // return here to not send the collider positions
            return;
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
            GenerateHandObjects(typeof(SoftContactBone));
            isHandPhysical = false;

            ((SoftContactBone)palmBone).SetupBone();
            var rbody = palmBone.gameObject.AddComponent<Rigidbody>();
            rbody.useGravity = false;
            rbody.collisionDetectionMode = CollisionDetectionMode.Continuous;

            palmBone.rigid = rbody;

            // Set the colliders to ignore eachother
            foreach (var bone in bones)
            {
                ((SoftContactBone)bone).SetupBone();
                Physics.IgnoreCollision(palmBone.palmCollider, bone.boneCollider);

                rbody = bone.gameObject.AddComponent<Rigidbody>();
                rbody.useGravity = false;
                rbody.collisionDetectionMode = CollisionDetectionMode.Continuous;

                foreach (var bone2 in bones)
                {
                    if (bone != bone2)
                    {
                        Physics.IgnoreCollision(bone.boneCollider, bone2.boneCollider);
                    }
                }
            }

            ChangeHandLayer(physicalHandsManager.HandsLayer);
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