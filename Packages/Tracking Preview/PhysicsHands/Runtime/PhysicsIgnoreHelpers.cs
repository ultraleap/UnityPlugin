using UnityEngine;

namespace Leap.Unity.Interaction.PhysicsHands
{
    // This script will prevent the physics hands helpers from being applied to your object.
    // This allows you to easily prevent important objects from being affected by the player.

    public class PhysicsIgnoreHelpers : MonoBehaviour
    {
        [Tooltip("This prevents the object from being collided with Physics Hands.")]
        public bool DisableHandCollisions = true;

        private void OnCollisionEnter(Collision collision)
        {
            if(!DisableHandCollisions)
            {
                return;
            }

            if(collision.gameObject != null && collision.gameObject.TryGetComponent<PhysicsBone>(out var temp))
            {
                foreach (var contact in collision.contacts)
                {
                    Physics.IgnoreCollision(temp.Collider, contact.thisCollider);
                }
            }
        }

    }
}