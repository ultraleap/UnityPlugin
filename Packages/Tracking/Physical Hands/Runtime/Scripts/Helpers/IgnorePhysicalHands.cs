/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.PhysicalHands
{
    public class IgnorePhysicalHands : MonoBehaviour
    {
        [SerializeField, Tooltip("Prevents the object from being grabbed by all Contact Hands.")]
        private bool disableAllGrabbing = true;

        /// <summary>
        /// Prevents the object from being grabbed by all Contact Hands
        /// </summary>
        public bool DisableAllGrabbing
        {
            get { return disableAllGrabbing; }
            set
            {
                disableAllGrabbing = value;

                if (GrabHelperObject != null)
                {
                    GrabHelperObject._grabbingIgnored = disableAllGrabbing;
                }
            }
        }

        [SerializeField, Tooltip("Prevents the object from being collided with all Contact Hands.")]
        private bool disableAllHandCollisions = true;

        /// <summary>
        /// Prevents the object from being collided with all Contact Hands
        /// </summary>
        public bool DisableAllHandCollisions
        {
            get { return disableAllHandCollisions; }
            set
            {
                disableAllHandCollisions = value;
                SetAllHandCollisions();
            }
        }

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

        private List<ContactHand> contactHands = new List<ContactHand>();

        private void Start()
        {
            _physicalHandsManager = (PhysicalHandsManager)FindAnyObjectByType(typeof(PhysicalHandsManager));

            if (_physicalHandsManager != null)
            {
                PhysicalHandsManager.OnHandsInitialized -= HandsInitialized;
                PhysicalHandsManager.OnHandsInitialized += HandsInitialized;
            }
            else
            {
                Debug.Log("No Physical Hands Manager found. Ignore Physical Hands can not initialize");
            }
        }

        internal void AddToHands(ContactHand contactHand)
        {
            if (!contactHands.Contains(contactHand))
            {
                contactHands.Add(contactHand);
                SetHandCollision(disableAllHandCollisions, contactHand);
            }
        }

        private void SetAllHandCollisions()
        {
            for (int i = 0; i < contactHands.Count; i++)
            {
                if (contactHands[i] != null)
                {
                    SetHandCollision(disableAllHandCollisions, contactHands[i]);
                }
                else
                {
                    contactHands.RemoveAt(i);
                    i--;
                }
            }
        }

        private void SetHandCollision(bool collisionDisabled, ContactHand contactHand)
        {
            if (this != null)
            {
                foreach (var objectCollider in GetComponentsInChildren<Collider>(true))
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

        private void HandsInitialized()
        {
            AddToHands(_physicalHandsManager.ContactParent.LeftHand);
            AddToHands(_physicalHandsManager.ContactParent.RightHand);

            SetAllHandCollisions();
        }
    }
}