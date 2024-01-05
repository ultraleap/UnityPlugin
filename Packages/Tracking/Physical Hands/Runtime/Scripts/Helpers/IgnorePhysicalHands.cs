using System.Collections.Generic;

using UnityEngine;

namespace Leap.Unity.PhysicalHands
{
    public class IgnorePhysicalHands: MonoBehaviour
    {
        [SerializeField, Tooltip("Prevents the object from being grabbed by all Contact Hands.")]
        internal bool disableAllGrabbing = true;

        [SerializeField, Tooltip("Prevents the object from being collided with all Contact Hands.")]
        internal bool disableAllHandCollisions = true;

        private GrabHelperObject _grabHelperObject = null;
        public GrabHelperObject GrabHelperObject
        { 
            get { return _grabHelperObject; }
            set 
            { 
                _grabHelperObject = value;
                _grabHelperObject._grabbingIgnored = disableAllGrabbing;
            } 
        }

        private PhysicalHandsManager _physicalHandsManager = null;

        List<ContactHand> contactHands = new List<ContactHand>();

        public void SetDisableGrabbing(bool setTo = false)
        {
            disableAllGrabbing = setTo;

            if (GrabHelperObject != null)
            {
                GrabHelperObject._grabbingIgnored = disableAllGrabbing;
            }
        }

        public void SetDisableCollisions(bool setTo = false)
        {
            disableAllHandCollisions = setTo;
            SetAllHandCollisions();
        }

        internal void AddToHands(ContactHand contactHand)
        {
            if (!contactHands.Contains(contactHand))
            {
                contactHands.Add(contactHand);
                SetCollisionHand(disableAllHandCollisions, contactHand);
            }
        }

        private void SetAllHandCollisions() 
        {
            for (int i = 0; i < contactHands.Count; i++)
            {
                if (contactHands[i] != null)
                {
                    SetCollisionHand(disableAllHandCollisions, contactHands[i]);
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
            if (this != null)
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

        private void HandsInitiated()
        {
            AddToHands(_physicalHandsManager.ContactParent.LeftHand);
            AddToHands(_physicalHandsManager.ContactParent.RightHand);

            SetAllHandCollisions();
        }

        private void Start()
        {
            _physicalHandsManager = (PhysicalHandsManager)FindAnyObjectByType(typeof(PhysicalHandsManager));
            if (_physicalHandsManager != null)
            {
                PhysicalHandsManager.onHandsInitiated += HandsInitiated;
            }
            else
            {
                Debug.Log("did not find physical hands manager");
            }
        }
    }
}
