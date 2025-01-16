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
    public class SoftContactBone : ContactBone
    {
        internal void SetupBone()
        {
            Collider.material = ((SoftContactParent)contactHand.contactParent).PhysicsMaterial;
        }

        #region Updating
        internal override void UpdatePalmBone(Hand hand)
        {
            width = hand.PalmWidth;
            tipPosition = hand.CalculateAverageKnucklePosition();
            length = Vector3.Distance(tipPosition, hand.WristPosition);

            UpdateColliderVelocities(hand.PalmPosition, hand.Rotation);

            // May need to interpolate this if objects are jittery.
            ContactUtils.SetupPalmCollider(palmCollider, palmEdgeColliders, hand);
        }

        internal override void UpdateBone(Bone prevBone, Bone bone)
        {
            tipPosition = bone.NextJoint;
            width = bone.Width;
            length = bone.Length;
            UpdateColliderVelocities(bone.PrevJoint, bone.Rotation);
            // May need to interpolate this if objects are jittery.
            ContactUtils.SetupBoneCollider(boneCollider, bone);
        }

        Vector3 lastTargetPosition;
        float softContactDislocationDistance = 0.03F;
        float teleportDistance = 0.05F;

        private const float DEAD_ZONE = 0.0005F;
        private float scale { get { return this.transform.lossyScale.x; } }

        internal void UpdateColliderVelocities(Vector3 targetPosition, Quaternion targetRotation)
        {
            // Calculate how far off its target the contact bone is.
            float errorDistance = Vector3.Distance(lastTargetPosition, Collider.attachedRigidbody.position);

            if (errorDistance > teleportDistance)
            {
                Collider.attachedRigidbody.position = targetPosition;
                Collider.attachedRigidbody.rotation = targetRotation;
                gameObject.layer = contactParent.physicalHandsManager.HandsResetLayer;

#if UNITY_6000_0_OR_NEWER
                Collider.attachedRigidbody.linearVelocity = Vector3.zero;
#else
                Collider.attachedRigidbody.velocity = Vector3.zero;
#endif 
                Collider.attachedRigidbody.angularVelocity = Vector3.zero;
                lastTargetPosition = Collider.attachedRigidbody.position;
                return;
            }

            // Potentially enable Soft Contact if our error is too large.
            if (contactHand.IsGrabbing || (errorDistance >= softContactDislocationDistance))
            {
                gameObject.layer = contactParent.physicalHandsManager.HandsResetLayer;
            }
            else if (!IsBoneContacting)
            {
                gameObject.layer = contactParent.physicalHandsManager.HandsLayer;
            }

            // Attempt to move the contact bone to its target position and rotation
            // by setting its target velocity and angular velocity. Include a "deadzone"
            // for position to avoid tiny vibrations.
            Vector3 delta = (targetPosition - Collider.attachedRigidbody.position);
            float deltaMag = delta.magnitude;

            if (deltaMag <= DEAD_ZONE)
            {
#if UNITY_6000_0_OR_NEWER
                Collider.attachedRigidbody.linearVelocity = Vector3.zero;
#else
                Collider.attachedRigidbody.velocity = Vector3.zero;
#endif
                lastTargetPosition = Collider.attachedRigidbody.position;
            }
            else
            {
                delta *= (deltaMag - DEAD_ZONE) / deltaMag;
                lastTargetPosition = Collider.attachedRigidbody.position + delta;

                Vector3 targetVelocity = delta / Time.fixedDeltaTime;

#if UNITY_6000_0_OR_NEWER
                Collider.attachedRigidbody.linearVelocity = targetVelocity;
#else
                Collider.attachedRigidbody.velocity = targetVelocity;
#endif
            }

            Quaternion deltaRot = targetRotation * Quaternion.Inverse(Collider.attachedRigidbody.rotation);
            Collider.attachedRigidbody.angularVelocity = ContactUtils.ToAngularVelocity(deltaRot, Time.fixedDeltaTime);
        }

        #endregion
    }
}