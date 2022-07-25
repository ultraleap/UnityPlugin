using UnityEngine;

namespace Leap.Unity.Interaction.PhysicsHands
{
    // This script will prevent the physics hands helpers from being applied to your object.
    // This allows you to easily prevent important objects from being affected by the player.
    // Note that this will not prevent your object from being collided with.
    public class PhysicsIgnoreHelpers : MonoBehaviour
    {
        private void OnCollisionEnter(Collision collision)
        {
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