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
    public class PhysicalHandEvents : MonoBehaviour, IPhysicalHandHover, IPhysicalHandContact, IPhysicalHandGrab
    {
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

        public bool handHovering { get { return leftHandHovering || rightHandHovering; } }
        public bool handContacting { get { return leftHandContacting || rightHandContacting; } }
        public bool handGrabbing { get { return leftHandGrabbing || rightHandGrabbing; } }

        public bool leftHandHovering { get; private set; }
        public bool leftHandContacting { get; private set; }
        public bool leftHandGrabbing { get; private set; }

        public bool rightHandHovering { get; private set; }
        public bool rightHandContacting { get; private set; }
        public bool rightHandGrabbing { get; private set; }

        public Rigidbody rigidBody { get; private set; }

        private void Awake()
        {
            rigidBody = GetComponent<Rigidbody>();
        }

        public void OnHandHover(ContactHand hand)
        {

            if (hand.Handedness == Chirality.Left)
            {
                leftHandHovering = true;

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

                if (!rightHandHovering)
                {
                    onHoverEnter?.Invoke(hand);
                    onRightHandHoverEnter?.Invoke(hand);
                }

                onRightHandHover?.Invoke(hand);
            }

            onHover?.Invoke(hand);
        }

        public void OnHandHoverExit(ContactHand hand)
        {
            if (hand.Handedness == Chirality.Left)
            {
                leftHandHovering = false;
                onLeftHandHoverExit?.Invoke(hand);
            }
            else
            {
                rightHandHovering = false;
                onRightHandHoverExit?.Invoke(hand);
            }

            onHoverExit?.Invoke(hand);
        }

        public void OnHandContact(ContactHand hand)
        {
            if (hand.Handedness == Chirality.Left)
            {
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
                leftHandContacting = false;
                onLeftHandContactExit?.Invoke(hand);
            }
            else
            {
                rightHandContacting = false;
                onRightHandContactExit?.Invoke(hand);
            }

            onContactExit?.Invoke(hand);
        }

        public void OnHandGrab(ContactHand hand)
        {
            if (hand.Handedness == Chirality.Left)
            {
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
                leftHandGrabbing = false;
                onLeftHandGrabExit?.Invoke(hand);
            }
            else
            {
                rightHandGrabbing = false;
                onRightHandGrabExit?.Invoke(hand);
            }

            onGrabExit?.Invoke(hand);
        }
    }
}