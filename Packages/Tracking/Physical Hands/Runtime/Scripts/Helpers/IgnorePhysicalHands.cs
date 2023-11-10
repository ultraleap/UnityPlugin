using System.Collections.Generic;

using UnityEngine;

using Leap.Unity.Attributes;

namespace Leap.Unity.PhysicalHands
{
    public class IgnorePhysicalHands: MonoBehaviour
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

        private PhysicalHandsManager _physicalHandsManager = null;

        List<ContactHand> contactHands = new List<ContactHand>();

        internal void AddToHands(ContactHand contactHand)
        {
            if (!contactHands.Contains(contactHand))
            {
                contactHands.Add(contactHand);
                SetCollisionHand(_disableAllHandCollisions, contactHand);
            }
        }

        private void SetAllHandCollisions() 
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

        private void HandsInitiated()
        {
            AddToHands(_physicalHandsManager.ContactParent.LeftHand);
            AddToHands(_physicalHandsManager.ContactParent.RightHand);

            SetAllHandCollisions();
        }

        private void Start()
        {
            //_physicalHandsManager = (PhysicalHandsManager)FindAnyObjectByType(typeof(PhysicalHandsManager));
            //if( _physicalHandsManager != null )
            //{
            //    if (_physicalHandsManager.onHandsInitiated == null)
            //    {
            //        _physicalHandsManager.onHandsInitiated = new UnityEngine.Events.UnityEvent();
            //    }
            //    _physicalHandsManager.onHandsInitiated.AddListener(HandsInitiated);
            //}
            //else
            //{
            //    Debug.Log("did not find physical hands manager");
            //}
        }
    }
}
