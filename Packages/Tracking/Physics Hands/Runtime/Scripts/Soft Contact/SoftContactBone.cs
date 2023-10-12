using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.PhysicsHands
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
            palmThickness = hand.Fingers[2].Bone(0).Width;
            tipPosition = hand.CalculateAverageKnucklePosition();
            wristPosition = hand.WristPosition;
            length = Vector3.Distance(tipPosition, wristPosition);

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

        protected const float DEAD_ZONE_FRACTION = 0.04F;
        public float scale { get { return this.transform.lossyScale.x; } }

        internal void UpdateWithInteractionEngineLogic(Vector3 targetPosition, Quaternion targetRotation)
        {
            // Calculate how far off its target the contact bone is.
            float errorDistance = Vector3.Distance(lastTargetPosition, Collider.attachedRigidbody.position);

            if(errorDistance > teleportDistance)
            {
                Collider.attachedRigidbody.position = targetPosition;
                Collider.attachedRigidbody.rotation = targetRotation;
                gameObject.layer = contactParent.physicsHandsManager.HandsResetLayer;

                Collider.attachedRigidbody.velocity = Vector3.zero;
                Collider.attachedRigidbody.angularVelocity = Vector3.zero;
                lastTargetPosition = Collider.attachedRigidbody.position;
                return;
            }

            // Potentially enable Soft Contact if our error is too large.
            if (contactHand.IsGrabbing || (errorDistance >= softContactDislocationDistance))
            {
                gameObject.layer = contactParent.physicsHandsManager.HandsResetLayer;
            }
            else if (!IsBoneContacting)
            {
                gameObject.layer = contactParent.physicsHandsManager.HandsLayer;
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

        public static Vector3 ToAngularVelocity(Quaternion deltaRotation, float deltaTime)
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

        internal override void PostFixedUpdateBone()
        {
            UpdateBoneWorldSpace();
        }

        private void UpdateBoneWorldSpace()
        {
            if (isPalm)
            {
                PhysExts.ToWorldSpaceBox(palmCollider, out Vector3 center, out Vector3 halfExtents, out Quaternion orientation);
                this.center = center;
                this.tipPosition = center + (orientation * (Vector3.forward * halfExtents.z));
                this.palmThickness = halfExtents.y * 2f;
                this.wristPosition = transform.position - (transform.rotation * Quaternion.Inverse(contactHand.dataHand.Rotation) * (contactHand.dataHand.PalmPosition - contactHand.dataHand.WristPosition));
            }
            else
            {
                PhysExts.ToWorldSpaceCapsule(boneCollider, out Vector3 tip, out Vector3 bottom, out float radius);
                this.tipPosition = tip;
                this.width = radius;
                this.length = Vector3.Distance(bottom, tip);
                this.center = Vector3.Lerp(bottom, tip, 0.5f);
            }
        }

        #endregion
    }
}
