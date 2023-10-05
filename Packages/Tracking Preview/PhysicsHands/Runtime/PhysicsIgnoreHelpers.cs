/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity.Interaction.PhysicsHands
{
    // This script will prevent the physics hands helpers from being applied to your object.
    // This allows you to easily prevent important objects from being affected by the player.

    public class PhysicsIgnoreHelpers : MonoBehaviour
    {
        [Tooltip("This prevents the object from being collided with all Physics Hands."), UnityEngine.Serialization.FormerlySerializedAsAttribute("DisableHandCollisions")]
        public bool DisableAllHandCollisions = false;
        [Tooltip("This prevents the object from being collided with a specific Physics Hands.")]
        public bool DisableSpecificHandCollisions = false;
        [Tooltip("Which hand should be ignored.")]
        public Chirality DisabledHandedness = Chirality.Left;

        public bool IsThisBoneIgnored(PhysicsBone bone)
        {
            if (bone == null)
                return false;
            if (DisableAllHandCollisions)
                return true;
            if (DisableSpecificHandCollisions && DisabledHandedness == bone.Hand.Handedness)
                return true;
            return false;
        }

        public bool IsThisHandIgnored(PhysicsHand hand)
        {
            if (hand == null)
                return false;
            if (DisableAllHandCollisions)
                return true;
            if (DisableSpecificHandCollisions && DisabledHandedness == hand.Handedness)
                return true;
            return false;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (DisableAllHandCollisions || DisableSpecificHandCollisions)
            {
                DisableBoneCollisions(collision);
            }
        }

        private void DisableBoneCollisions(Collision collision)
        {
            if (collision.gameObject != null && collision.gameObject.TryGetComponent<PhysicsBone>(out var temp))
            {
                if (!IsThisBoneIgnored(temp))
                {
                    return;
                }
                foreach (var contact in collision.contacts)
                {
                    Physics.IgnoreCollision(temp.Collider, contact.thisCollider);
                }
            }
        }

    }
}