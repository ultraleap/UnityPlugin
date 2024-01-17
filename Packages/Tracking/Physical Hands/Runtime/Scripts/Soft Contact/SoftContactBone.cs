/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity.PhysicalHands
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

            UpdateWithInteractionEngineLogic(hand.PalmPosition, hand.Rotation);

            // May need to interpolate this if objects are jittery.
            ContactUtils.SetupPalmCollider(palmCollider, palmEdgeColliders, hand);
        }

        internal override void UpdateBone(Bone prevBone, Bone bone)
        {
            tipPosition = bone.NextJoint;
            width = bone.Width;
            length = bone.Length;
            UpdateWithInteractionEngineLogic(bone.PrevJoint, bone.Rotation);
            // May need to interpolate this if objects are jittery.
            ContactUtils.SetupBoneCollider(boneCollider, bone);
        }

        Vector3 lastTargetPosition;
        float softContactDislocationDistance = 0.03F;
        float teleportDistance = 0.05F;

        private const float DEAD_ZONE_FRACTION = 0.04F;
        private float scale { get { return this.transform.lossyScale.x; } }

        internal void UpdateWithInteractionEngineLogic(Vector3 targetPosition, Quaternion targetRotation)
        {
            // Calculate how far off its target the contact bone is.
            float errorDistance = Vector3.Distance(lastTargetPosition, Collider.attachedRigidbody.position);

            if(errorDistance > teleportDistance)
            {
                Collider.attachedRigidbody.position = targetPosition;
                Collider.attachedRigidbody.rotation = targetRotation;
                gameObject.layer = contactParent.PhysicalHandsManager.HandsResetLayer;

                Collider.attachedRigidbody.velocity = Vector3.zero;
                Collider.attachedRigidbody.angularVelocity = Vector3.zero;
                lastTargetPosition = Collider.attachedRigidbody.position;
                return;
            }

            // Potentially enable Soft Contact if our error is too large.
            if (contactHand.IsGrabbing || (errorDistance >= softContactDislocationDistance))
            {
                gameObject.layer = contactParent.PhysicalHandsManager.HandsResetLayer;
            }
            else if (!IsBoneContacting)
            {
                gameObject.layer = contactParent.PhysicalHandsManager.HandsLayer;
            }

            // Attempt to move the contact bone to its target position and rotation
            // by setting its target velocity and angular velocity. Include a "deadzone"
            // for position to avoid tiny vibrations.
            float deadzone = Mathf.Min(DEAD_ZONE_FRACTION * width, 0.01F * scale);
            Vector3 delta = (targetPosition - Collider.attachedRigidbody.position);
            float deltaMag = delta.magnitude;

            if (deltaMag <= deadzone)
            {
                Collider.attachedRigidbody.velocity = Vector3.zero;
                lastTargetPosition = Collider.attachedRigidbody.position;
            }
            else
            {
                delta *= (deltaMag - deadzone) / deltaMag;
                lastTargetPosition = Collider.attachedRigidbody.position + delta;

                Vector3 targetVelocity = delta / Time.fixedDeltaTime;
                Collider.attachedRigidbody.velocity = targetVelocity;
            }

            Quaternion deltaRot = targetRotation * Quaternion.Inverse(Collider.attachedRigidbody.rotation);
            Collider.attachedRigidbody.angularVelocity = ToAngularVelocity(deltaRot, Time.fixedDeltaTime);
        }

        private static Vector3 ToAngularVelocity(Quaternion deltaRotation, float deltaTime)
        {
            Vector3 deltaAxis;
            float deltaAngle;
            deltaRotation.ToAngleAxis(out deltaAngle, out deltaAxis);

            if (float.IsInfinity(deltaAxis.x))
            {
                deltaAxis = Vector3.zero;
                deltaAngle = 0;
            }

            if (deltaAngle > 180)
            {
                deltaAngle -= 360.0f;
            }

            return deltaAxis * deltaAngle * Mathf.Deg2Rad / deltaTime;
        }

        #endregion
    }
}
