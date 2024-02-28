/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Leap.Unity.PhysicalHands
{
    public class IgnorePhysicalHands : MonoBehaviour
    {
        public ChiralitySelection HandToIgnore = ChiralitySelection.BOTH;

        [SerializeField, Tooltip("Prevents the object from being grabbed by all Contact Hands.")]
        private bool _disableAllGrabbing = true;

        /// <summary>
        /// Prevents the object from being grabbed by all Contact Hands
        /// </summary>
        public bool DisableAllGrabbing
        {
            get { return _disableAllGrabbing; }
            set
            {
                _disableAllGrabbing = value;
            }
        }

        [SerializeField, Tooltip("Prevents colliders on this gameobject from colliding with Contact Hands."), FormerlySerializedAs("_disableAllHandCollisions")]
        private bool _disableHandCollisions = true;

        /// <summary>
        /// Prevents the object from being collided with Contact Hands
        /// </summary>
        public bool DisableAllHandCollisions
        {
            get { return _disableHandCollisions; }
            set
            {
                _disableHandCollisions = value;
                SetAllHandCollisions();
            }
        }

        [SerializeField, Tooltip("Prevents colliders on all child gameobjects of this gameobject from colliding with Contact Hands.")]
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

        private PhysicalHandsManager _physicalHandsManager = null;

        private List<ContactHand> contactHands = new List<ContactHand>();

#if UNITY_EDITOR
        private void OnValidate()
        {
            DisableAllGrabbing = _disableAllGrabbing;
            DisableAllHandCollisions = _disableHandCollisions;
            DisableCollisionOnChildObjects = _disableCollisionOnChildren;
        }
#endif

        /// <summary>
        /// Clean up code, re-enables all collision and grabbing on this object and all children
        /// </summary>
        void OnDisable()
        {
            SetAllHandCollisions(forceEnable : true);
        }

        private void OnEnable()
        {
#if UNITY_2021_3_18_OR_NEWER
            _physicalHandsManager = (PhysicalHandsManager)FindAnyObjectByType(typeof(PhysicalHandsManager));
#else
            _physicalHandsManager = (PhysicalHandsManager)FindObjectOfType<PhysicalHandsManager>();
#endif

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
            contactHands.Add(contactHand);
            SetHandCollision(contactHand);
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

            if(forceEnable) // Force the collision to be enabled
            {
                shouldDisableCollisionWithHand = false;
            }
            else if(forceDisable) // Force the collision to be disabled
            {
                shouldDisableCollisionWithHand = true;
            }
            else if (IsHandIgnored(contactHand)) // Enable/disable based on chosen chirality
            {
                shouldDisableCollisionWithHand = true;
            }

            if (this != null)
            {
                bool disableOnParent = shouldDisableCollisionWithHand && _disableHandCollisions;
                bool disableOnChild = shouldDisableCollisionWithHand && _disableCollisionOnChildren;

                foreach (var objectCollider in GetComponentsInChildren<Collider>(true))
                {
                    if ((disableOnParent && objectCollider.gameObject == gameObject) || 
                        (disableOnChild && objectCollider.gameObject != gameObject))
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

        private void HandsInitialized()
        {
            AddToHands(_physicalHandsManager.ContactParent.LeftHand);
            AddToHands(_physicalHandsManager.ContactParent.RightHand);

            SetAllHandCollisions();
        }

        /// <summary>
        /// Checks whether this hand will be ignored via this component when considering grabbign or collisions
        /// </summary>
        /// <param name="hand"></param>
        /// <returns>true if chirality is correct or Hand to ignore is set to BOTH</returns>
        public bool IsHandIgnored(ContactHand hand)
        {
            if (this.enabled && hand != null &&
                ((int)HandToIgnore == (int)hand.Handedness || HandToIgnore == ChiralitySelection.BOTH))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks whether grabbing is dissabled and weather its chirality match the ignored chirality
        /// </summary>
        /// <param name="hand"></param>
        /// <returns>true if ignore grabbing is true and chirality is correct or Hand to ignore is set to BOTH</returns>
        public bool IsGrabbingIgnoredForHand(ContactHand hand)
        {
            if (this.enabled && hand != null && _disableAllGrabbing && IsHandIgnored(hand))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks whether collisions are dissabled and weather its chirality match the ignored chirality
        /// </summary>
        /// <param name="hand"></param>
        /// <returns>true if ignore collisions is true and chirality is correct or Hand to ignore is set to BOTH</returns>
        public bool IsCollisionIgnoredForHand(ContactHand hand)
        {
            if (this.enabled && hand != null && _disableHandCollisions && IsHandIgnored(hand))
            {
                return true;
            }
            return false;
        }
    }
}