using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Interaction.PhysicsHands
{
    [RequireComponent(typeof(PhysicsIgnoreHelpers))]
    public class PhysicsButtonElement : MonoBehaviour
    {
        private PhysicsButton _parentButton = null;

        public void Awake()
        {
            _parentButton = GetComponentInParent<PhysicsButton>();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if(_parentButton != null)
            {
                _parentButton.TrySetDepressor(collision.collider);
            }
        }

        private void OnCollisionStay(Collision collision)
        {
            if(_parentButton != null)
            {
                _parentButton.TrySetDepressor(collision.collider);
            }
        }
    }
}