using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using UnityEngine;
using Leap.Unity.Attributes;
using UnityEngine.UIElements;

namespace Leap.Unity.PhysicsHands
{
    public class IgnorePhysicsHands: MonoBehaviour
    {

        [Tooltip("This prevents the object from being grabbed by all Contact Hands.")]
        [SerializeProperty("DisableAllGrabbing"), SerializeField]
        private bool _disableAllGrabbing = true;
        public bool DisableAllGrabbing 
        { 
            get { return _disableAllGrabbing; }
            set
            {
                _disableAllGrabbing = value;

                if (GrabHelperObject != null)
                {
                    GrabHelperObject.Ignored = _disableAllGrabbing;
                }
            }
        }

        [Tooltip("This prevents the object from being collided with all Contact Hands."), UnityEngine.Serialization.FormerlySerializedAsAttribute("DisableHandCollisions")]
        [SerializeProperty("DisableAllHandCollisions"), SerializeField]
        private bool _disableAllHandCollisions = true;
        public bool DisableAllHandCollisions
        {
            get { return _disableAllHandCollisions; }
            set 
            { 
                _disableAllHandCollisions = value;
                SetAllHandCollisions();
            }
        }

        [HideInInspector]
        private GrabHelperObject _grabHelperObject = null;
        public GrabHelperObject GrabHelperObject 
        { 
            get { return _grabHelperObject; }
            set 
            { 
                _grabHelperObject = value;
                _grabHelperObject.Ignored = _disableAllGrabbing;
            } 
        }

        List<ContactHand> contactHands = new List<ContactHand>();

        public void AddToHands(ContactHand contactHand)
        {
            if (!contactHands.Contains(contactHand))
            {
                contactHands.Add(contactHand);
                SetCollisionHand(_disableAllHandCollisions, contactHand);
            }
        }

        public void SetAllHandCollisions() 
        {
            for (int i = 0; i < contactHands.Count; i++)
            {
                if (contactHands[i] != null)
                {
                    SetCollisionHand(_disableAllHandCollisions, contactHands[i]);
                }
                else
                {
                    contactHands.RemoveAt(i);
                    i--;
                }

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
