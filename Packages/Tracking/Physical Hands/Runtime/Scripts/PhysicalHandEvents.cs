/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;
using UnityEngine.Events;

namespace Ultraleap.PhysicalHands
{
    [RequireComponent(typeof(Rigidbody))]
    public class PhysicalHandEvents : MonoBehaviour, IPhysicalHandHover, IPhysicalHandContact, IPhysicalHandGrab, IPhysicalHandPrimaryHover
    {
        [Header("Configuration")]
        [Tooltip("When ticked, the object will only register events when it is primary hovered. Only one object can be primary hovered at a time by either hand.")]
        [SerializeField]
        protected bool _usePrimaryHover = false;

        #region UnityEvents

        [Space, Header("Hover Events"), Space]
        [Tooltip("Invoked once when a given ContactHand begins to hover this GameObject's RigidBody")]
        public UnityEvent<ContactHand> onHoverEnter;
        [Tooltip("Invoked every frame that a given ContactHand is hovering this GameObject's RigidBody")]
        public UnityEvent<ContactHand> onHover;
        [Tooltip("Invoked once when a given ContactHand stops hovering this GameObject's RigidBody")]
        public UnityEvent<ContactHand> onHoverExit;

        [Space, Header("Contact Events"), Space]
        [Tooltip("Invoked once when a given ContactHand begins to contact this GameObject's RigidBody")]
        public UnityEvent<ContactHand> onContactEnter;
        [Tooltip("Invoked every frame that a given ContactHand is contacting this GameObject's RigidBody")]
        public UnityEvent<ContactHand> onContact;
        [Tooltip("Invoked once when a given ContactHand stops contacting this GameObject's RigidBody")]
        public UnityEvent<ContactHand> onContactExit;

        [Space, Header("Grab Events"), Space]
        [Tooltip("Invoked once when a given ContactHand begins to grab this GameObject's RigidBody")]
        public UnityEvent<ContactHand> onGrabEnter;
        [Tooltip("Invoked every frame that a given ContactHand is grabbing this GameObject's RigidBody")]
        public UnityEvent<ContactHand> onGrab;
        [Tooltip("Invoked once when a given ContactHand stops grabbing this GameObject's RigidBody")]
        public UnityEvent<ContactHand> onGrabExit;

        //

        [Space, Header("Left Hover Events"), Space]
        [Tooltip("Invoked once when a Left ContactHand begins to hover this GameObject's RigidBody")]
        public UnityEvent<ContactHand> onLeftHandHoverEnter;
        [Tooltip("Invoked every frame that a Left ContactHand is hovering this GameObject's RigidBody")]
        public UnityEvent<ContactHand> onLeftHandHover;
        [Tooltip("Invoked once when a Left ContactHand stops hovering this GameObject's RigidBody")]
        public UnityEvent<ContactHand> onLeftHandHoverExit;

        [Space, Header("Left Contact Events"), Space]
        [Tooltip("Invoked once when a Left ContactHand begins to contact this GameObject's RigidBody")]
        public UnityEvent<ContactHand> onLeftHandContactEnter;
        [Tooltip("Invoked every frame that a Left ContactHand is contacting this GameObject's RigidBody")]
        public UnityEvent<ContactHand> onLeftHandContact;
        [Tooltip("Invoked once when a Left ContactHand stops contacting this GameObject's RigidBody")]
        public UnityEvent<ContactHand> onLeftHandContactExit;

        [Space, Header("Left Grab Events"), Space]
        [Tooltip("Invoked once when a Left ContactHand begins to grab this GameObject's RigidBody")]
        public UnityEvent<ContactHand> onLeftHandGrabEnter;
        [Tooltip("Invoked every frame that a Left ContactHand is grabing this GameObject's RigidBody")]
        public UnityEvent<ContactHand> onLeftHandGrab;
        [Tooltip("Invoked once when a Left ContactHand stops grabbing this GameObject's RigidBody")]
        public UnityEvent<ContactHand> onLeftHandGrabExit;

        //

        [Space, Header("Right Hover Events"), Space]
        [Tooltip("Invoked once when a Right ContactHand begins to hover this GameObject's RigidBody")]
        public UnityEvent<ContactHand> onRightHandHoverEnter;
        [Tooltip("Invoked every frame that a Right ContactHand is hovering this GameObject's RigidBody")]
        public UnityEvent<ContactHand> onRightHandHover;
        [Tooltip("Invoked once when a Right ContactHand stops hovering this GameObject's RigidBody")]
        public UnityEvent<ContactHand> onRightHandHoverExit;

        [Space, Header("Right Contact Events"), Space]
        [Tooltip("Invoked once when a Right ContactHand begins to contact this GameObject's RigidBody")]
        public UnityEvent<ContactHand> onRightHandContactEnter;
        [Tooltip("Invoked every frame that a Right ContactHand is contacting this GameObject's RigidBody")]
        public UnityEvent<ContactHand> onRightHandContact;
        [Tooltip("Invoked once when a Right ContactHand stops contacting this GameObject's RigidBody")]
        public UnityEvent<ContactHand> onRightHandContactExit;

        [Space, Header("Right Grab Events"), Space]
        [Tooltip("Invoked once when a Right ContactHand begins to grab this GameObject's RigidBody")]
        public UnityEvent<ContactHand> onRightHandGrabEnter;
        [Tooltip("Invoked every frame that a Right ContactHand is grabing this GameObject's RigidBody")]
        public UnityEvent<ContactHand> onRightHandGrab;
        [Tooltip("Invoked once when a Right ContactHand stops grabbing this GameObject's RigidBody")]
        public UnityEvent<ContactHand> onRightHandGrabExit;

        #endregion

        public bool anyHandHovering { get { return leftHandHovering || rightHandHovering; } }
        public bool anyHandPrimaryHovering { get { return leftHandPrimaryHovering || rightHandPrimaryHovering; } }
        public bool anyHandContacting { get { return leftHandContacting || rightHandContacting; } }
        public bool anyHandGrabbing { get { return leftHandGrabbing || rightHandGrabbing; } }


        public bool leftHandHovering { get; private set; }
        public bool leftHandPrimaryHovering { get; private set; }
        public bool leftHandContacting { get; private set; }
        public bool leftHandGrabbing { get; private set; }

        public bool rightHandHovering { get; private set; }
        public bool rightHandPrimaryHovering { get; private set; }
        public bool rightHandContacting { get; private set; }
        public bool rightHandGrabbing { get; private set; }

        public Rigidbody rigidBody { get; private set; }

        private void Awake()
        {
            rigidBody = GetComponent<Rigidbody>();
        }

        public void OnHandHover(ContactHand hand)
        {
            if (_usePrimaryHover)
            {
                return;
            }

            InvokeOnHandHover(hand);
        }

        public void OnHandHoverExit(ContactHand hand)
        {
            if (_usePrimaryHover)
            {
                return;
            }

            InvokeOnHandHoverExit(hand);
        }

        public void OnHandPrimaryHover(ContactHand hand)
        {
            InvokeOnHandHover(hand);
        }

        public void OnHandPrimaryHoverExit(ContactHand hand)
        {
            InvokeOnHandHoverExit(hand);
        }

        private void InvokeOnHandHover(ContactHand hand)
        {
            if (hand.Handedness == Chirality.Left)
            {
                leftHandHovering = true;
                //We shouldn't reach here if _usePrimaryHover is true & a non-primary hover has occurred
                leftHandPrimaryHovering = _usePrimaryHover;

                if (!leftHandHovering)
                {
                    onHoverEnter?.Invoke(hand);
                    onLeftHandHoverEnter?.Invoke(hand);
                }

                onLeftHandHover?.Invoke(hand);
            }
            else
            {
                rightHandHovering = true;
                //We shouldn't reach here if _usePrimaryHover is true & a non-primary hover has occurred
                rightHandPrimaryHovering = _usePrimaryHover;

                if (!rightHandHovering)
                {
                    onHoverEnter?.Invoke(hand);
                    onRightHandHoverEnter?.Invoke(hand);
                }

                onRightHandHover?.Invoke(hand);
            }
        }

        private void InvokeOnHandHoverExit(ContactHand hand)
        {
            if (hand.Handedness == Chirality.Left)
            {
                leftHandHovering = false;
                leftHandPrimaryHovering = false;
                onLeftHandHoverExit?.Invoke(hand);
            }
            else
            {
                rightHandHovering = false;
                rightHandPrimaryHovering = false;
                onRightHandHoverExit?.Invoke(hand);
            }

            onHoverExit?.Invoke(hand);
        }

        public void OnHandContact(ContactHand hand)
        {
            if (hand.Handedness == Chirality.Left)
            {
                if (_usePrimaryHover && !leftHandPrimaryHovering)
                {
                    return;
                }

                leftHandContacting = true;

                if (!leftHandContacting)
                {
                    onContactEnter?.Invoke(hand);
                    onLeftHandContactEnter?.Invoke(hand);
                }

                onLeftHandContact?.Invoke(hand);
            }
            else
            {
                if (_usePrimaryHover && !rightHandPrimaryHovering)
                {
                    return;
                }

                rightHandContacting = true;

                if (!rightHandContacting)
                {
                    onContactEnter?.Invoke(hand);
                    onRightHandContactEnter?.Invoke(hand);
                }

                onRightHandContact?.Invoke(hand);
            }

            onContact?.Invoke(hand);
        }

        public void OnHandContactExit(ContactHand hand)
        {
            if (hand.Handedness == Chirality.Left)
            {
                if (_usePrimaryHover && !leftHandPrimaryHovering)
                {
                    return;
                }

                leftHandContacting = false;
                onLeftHandContactExit?.Invoke(hand);
            }
            else
            {
                if (_usePrimaryHover && !rightHandPrimaryHovering)
                {
                    return;
                }

                rightHandContacting = false;
                onRightHandContactExit?.Invoke(hand);
            }

            onContactExit?.Invoke(hand);
        }

        public void OnHandGrab(ContactHand hand)
        {
            if (hand.Handedness == Chirality.Left)
            {
                if (_usePrimaryHover && !leftHandPrimaryHovering)
                {
                    return;
                }

                leftHandGrabbing = true;

                if (!leftHandGrabbing)
                {
                    onLeftHandGrabEnter?.Invoke(hand);
                    onGrabEnter?.Invoke(hand);
                }

                onLeftHandGrab?.Invoke(hand);
            }
            else
            {
                if (_usePrimaryHover && !rightHandPrimaryHovering)
                {
                    return;
                }

                rightHandGrabbing = true;

                if (!rightHandGrabbing)
                {
                    onRightHandGrabEnter?.Invoke(hand);
                    onGrabEnter?.Invoke(hand);
                }

                onRightHandGrab?.Invoke(hand);
            }

            onGrab?.Invoke(hand);
        }

        public void OnHandGrabExit(ContactHand hand)
        {
            if (hand.Handedness == Chirality.Left)
            {
                if (_usePrimaryHover && !leftHandPrimaryHovering)
                {
                    return;
                }

                leftHandGrabbing = false;
                onLeftHandGrabExit?.Invoke(hand);
            }
            else
            {
                if (_usePrimaryHover && !rightHandPrimaryHovering)
                {
                    return;
                }

                rightHandGrabbing = false;
                onRightHandGrabExit?.Invoke(hand);
            }

            onGrabExit?.Invoke(hand);
        }
    }
}