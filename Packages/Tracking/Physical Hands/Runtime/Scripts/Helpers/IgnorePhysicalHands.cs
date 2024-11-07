/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections.Generic;
using UnityEngine;

namespace Leap.PhysicalHands
{
    public class IgnorePhysicalHands : MonoBehaviour
    {
        [Header("Grabs")]
        public ChiralitySelection HandToIgnoreGrabs = ChiralitySelection.BOTH;

        [SerializeField, Tooltip("Prevents the object from being grabbed by chosen Contact Hands." +
            "\n\nNote: This only applies if IgnorePhysicalHands is applied to a GameObject with a Rigidbody component."),
            Attributes.InspectorName("Ignore Grabbing")]
        private bool _disableAllGrabbing = true;

        /// <summary>
        /// Prevents the object from being grabbed by chosen Contact Hands.
        /// 
        /// Note: This only applies if IgnorePhysicalHands is applied to a GameObject with a Rigidbody component.
        /// </summary>
        public bool DisableAllGrabbing
        {
            get { return _disableAllGrabbing; }
            set
            {
                _disableAllGrabbing = value;
                SetAllHandGrabbing();
            }
        }

        [Header("Collisions")]
        public ChiralitySelection HandToIgnoreCollisions = ChiralitySelection.BOTH;

        [SerializeField, Tooltip("Prevents colliders on this gameobject from colliding with Contact Hands."),
            Attributes.InspectorName("Ignore Hand Collisions")]
        private bool _disableAllHandCollisions = true;

        /// <summary>
        /// Prevents the object from being collided with Contact Hands
        /// </summary>
        public bool DisableAllHandCollisions
        {
            get { return _disableAllHandCollisions; }
            set
            {
                _disableAllHandCollisions = value;
                SetAllHandCollisions();
            }
        }

        [SerializeField, Tooltip("Prevents colliders on all child gameobjects of this gameobject from colliding with Contact Hands."),
            Attributes.InspectorName("Ignore Hand Collisions On Children")]
        private bool _disableCollisionOnChildren = true;

        /// <summary>
        /// Prevents child objects from being collided with Contact Hands
        /// </summary>
        public bool DisableCollisionOnChildObjects
        {
            get { return _disableCollisionOnChildren; }
            set
            {
                _disableCollisionOnChildren = value;
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
            }
        }

        private List<ContactHand> contactHands = new List<ContactHand>();

#if UNITY_EDITOR
        private void OnValidate()
        {
            DisableAllGrabbing = _disableAllGrabbing;
            DisableAllHandCollisions = _disableAllHandCollisions;
            DisableCollisionOnChildObjects = _disableCollisionOnChildren;
        }
#endif

        private void Start()
        {
            // Find if we have a GrabHelperObject already, and pass it a reference to us
            //  Note: this will only happen when this IgnorePhysicalHands is created at runtime
            if (GrabHelper.Instance != null)
            {
                Rigidbody rbody = GetComponent<Rigidbody>();

                if (rbody != null)
                {
                    if (GrabHelper.Instance.TryGetGrabHelperObjectFromRigid(rbody, out _grabHelperObject))
                    {
                        _grabHelperObject._ignorePhysicalHands = this;

                        foreach (var hand in _grabHelperObject.GrabbableHands)
                        {
                            AddToHands(hand);
                        }

                        SetAllHandCollisions();
                        SetAllHandGrabbing();
                    }
                }
            }
        }

        private void OnEnable()
        {
            PhysicalHandsManager.OnHandsInitialized -= HandsInitialized;
            PhysicalHandsManager.OnHandsInitialized += HandsInitialized;

            SetAllHandCollisions();
            SetAllHandGrabbing();
        }

        /// <summary>
        /// Clean up code, re-enables all collision and grabbing on this object and all children
        /// </summary>
        void OnDisable()
        {
            SetAllHandCollisions(forceEnable: true);
        }

        private void OnDestroy()
        {
            PhysicalHandsManager.OnHandsInitialized -= HandsInitialized;
        }

        /// <summary>
        /// Initialize this component with alternative values to the defaults
        /// 
        /// Call Init on this component immediately after adding it to a gameobject if you wish to override the default values
        /// 
        /// Note: Consider adding this component to the GameObject before runtime begins and simply toggle the variables at runtime instead
        /// </summary>
        public void Init(bool ignoreGrabbing = true, bool ignoreCollisions = true, bool ignoreChildCollisions = true, ChiralitySelection handToIgnoreGrabs = ChiralitySelection.BOTH, ChiralitySelection handToIgnoreCollisions = ChiralitySelection.BOTH)
        {
            this.HandToIgnoreGrabs = handToIgnoreGrabs;
            this.HandToIgnoreCollisions = handToIgnoreCollisions;
            this.DisableAllGrabbing = ignoreGrabbing;
            this.DisableAllHandCollisions = ignoreCollisions;
            this.DisableCollisionOnChildObjects = ignoreChildCollisions;
        }

        internal void AddToHands(ContactHand contactHand)
        {
            contactHands.Add(contactHand);
            SetHandCollision(contactHand);
        }

        private void SetAllHandGrabbing(bool forceDisable = false)
        {
            if (DisableAllGrabbing || forceDisable)
            {
                switch (HandToIgnoreGrabs)
                {
                    case ChiralitySelection.LEFT:
                        _grabHelperObject?.UnregisterGrabbingHand(Chirality.Left);
                        break;
                    case ChiralitySelection.RIGHT:
                        _grabHelperObject?.UnregisterGrabbingHand(Chirality.Right);
                        break;
                    case ChiralitySelection.BOTH:
                        _grabHelperObject?.UnregisterGrabbingHand(Chirality.Left);
                        _grabHelperObject?.UnregisterGrabbingHand(Chirality.Right);
                        break;
                }
            }
        }

        private void SetAllHandCollisions(bool forceEnable = false, bool forceDisable = false)
        {
            for (int i = 0; i < contactHands.Count; i++)
            {
                if (contactHands[i] != null)
                {
                    SetHandCollision(contactHands[i], forceEnable, forceDisable);
                }
                else
                {
                    contactHands.RemoveAt(i);
                    i--;
                }
            }
        }

        private void SetHandCollision(ContactHand contactHand, bool forceEnable = false, bool forceDisable = false)
        {
            bool shouldDisableCollisionWithHand = false;

            if (forceEnable) // Force the collision to be enabled
            {
                shouldDisableCollisionWithHand = false;
            }
            else if (forceDisable) // Force the collision to be disabled
            {
                shouldDisableCollisionWithHand = true;
            }
            else if (IsCollisionIgnoredForHand(contactHand)) // Enable/disable based on chosen chirality
            {
                shouldDisableCollisionWithHand = true;
            }

            if (this != null)
            {
                bool disableOnParent = shouldDisableCollisionWithHand && _disableAllHandCollisions;
                bool disableOnChild = shouldDisableCollisionWithHand && _disableCollisionOnChildren;

                foreach (var objectCollider in GetComponentsInChildren<Collider>(true))
                {
                    if ((disableOnParent && objectCollider.gameObject == gameObject) ||
                        (disableOnChild && objectCollider.gameObject != gameObject && !objectCollider.TryGetComponent<IgnorePhysicalHands>(out _)))
                    {
                        IgnoreCollisionOnAllHandBones(contactHand, objectCollider, true);
                    }
                    else
                    {
                        IgnoreCollisionOnAllHandBones(contactHand, objectCollider, false);
                    }
                }
            }
        }

        private void IgnoreCollisionOnAllHandBones(ContactHand contactHand, Collider colliderToIgnore, bool collisionDisabled)
        {
            foreach (var bone in contactHand.bones)
            {
                Physics.IgnoreCollision(bone.Collider, colliderToIgnore, collisionDisabled);
            }

            foreach (var palmCollider in contactHand.palmBone.palmEdgeColliders)
            {
                Physics.IgnoreCollision(palmCollider, colliderToIgnore, collisionDisabled);
            }

            Physics.IgnoreCollision(contactHand.palmBone.Collider, colliderToIgnore, collisionDisabled);
        }

        private void HandsInitialized(ContactParent contactParent)
        {
            AddToHands(contactParent.LeftHand);
            AddToHands(contactParent.RightHand);

            SetAllHandCollisions();
        }

        /// <summary>
        /// Checks whether this hand will be ignored via this component when considering grabbing
        /// </summary>
        /// <param name="hand"></param>
        /// <returns>true if chirality is correct or Hand to ignore is set to BOTH</returns>
        public bool IsGrabbingIgnoredForHand(ContactHand hand)
        {
            if (this.enabled &&
                hand != null &&
                _disableAllGrabbing &&
                ((int)HandToIgnoreGrabs == (int)hand.Handedness || HandToIgnoreGrabs == ChiralitySelection.BOTH))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks whether this hand will be ignored via this component when considering collisions
        /// </summary>
        /// <param name="hand"></param>
        /// <returns>true if chirality is correct or Hand to ignore is set to BOTH</returns>
        public bool IsCollisionIgnoredForHand(ContactHand hand)
        {
            if (this.enabled &&
                hand != null &&
                _disableAllHandCollisions &&
                ((int)HandToIgnoreCollisions == (int)hand.Handedness || HandToIgnoreCollisions == ChiralitySelection.BOTH))
            {
                return true;
            }

            return false;
        }
    }
}