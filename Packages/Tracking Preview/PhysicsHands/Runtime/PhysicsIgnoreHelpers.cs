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
        [Tooltip("This prevents the object from being collided with Physics Hands.")]
        public bool DisableHandCollisions = false;

        private void OnCollisionEnter(Collision collision)
        {
            if (DisableHandCollisions)
            {
                if (collision.gameObject != null && collision.gameObject.TryGetComponent<PhysicsBone>(out var temp))
                {
                    foreach (var contact in collision.contacts)
                    {
                        Physics.IgnoreCollision(temp.Collider, contact.thisCollider);
                    }
                }
            }
        }

    }
}