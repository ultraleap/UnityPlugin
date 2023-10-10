using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using UnityEngine;
using Leap.Unity.Attributes;

namespace Leap.Unity.ContactHands
{
    public class IgnoreContactHelper : MonoBehaviour
    {

        [Tooltip("This prevents the object from being grabbed by all Contact Hands.")]
        [SerializeProperty("DisableAllGrabbing"), SerializeField]
        private bool _disableAllGrabbing = false;
        public bool DisableAllGrabbing 
        { 
            get { return _disableAllGrabbing; } 
            private set 
            { 
                _disableAllGrabbing = value; 
                grabHelperObject.Ignored = _disableAllGrabbing;

            } 
        }

        [Tooltip("This prevents the object from being collided with all Contact Hands."), UnityEngine.Serialization.FormerlySerializedAsAttribute("DisableHandCollisions")]
        [SerializeProperty("DisableAllHandCollisions"), SerializeField]
        public bool _disableAllHandCollisions = false;
        public bool DisableAllHandCollisions
        {
            get { return _disableAllHandCollisions; }
            private set 
            { 
                _disableAllHandCollisions = value;
                SetAllHandCollisions();
            }
        }

        [Tooltip("This prevents the object from being collided with a specific Contact Hands.")]
        public bool DisableSpecificHandCollisions = false;
        [Tooltip("Which hand should be ignored.")]
        public Chirality DisabledHandedness = Chirality.Left;

        public GrabHelperObject grabHelperObject = null;
        public ContactManager contactManager = null;

        private void Start()
        {
            contactManager = FindFirstObjectByType<ContactManager>();
            Invoke("SetAllHandCollisions", 0.1f);
        }

        public void SetAllHandCollisions() 
        {
            if (contactManager != null)
            {
                SetCollisionHand(DisableAllHandCollisions, contactManager.contactHands.LeftHand);
                SetCollisionHand(DisableAllHandCollisions, contactManager.contactHands.RightHand);
            }
        }

        private void SetCollisionHand(bool collisionDisabled, ContactHand contactHand)
        {
            foreach (var objectCollider in GetComponentsInChildren<Collider>())
            {
                foreach (var bone in contactHand.bones)
                {
                    Physics.IgnoreCollision(bone.Collider, objectCollider, collisionDisabled);

                }
                foreach (var palmCollider in contactHand.palmBone.palmEdgeColliders)
                {
                    Physics.IgnoreCollision(palmCollider, objectCollider, collisionDisabled);
                }
                Physics.IgnoreCollision(contactHand.palmBone.Collider, objectCollider, collisionDisabled);
            }
        }
    }
}
