using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Leap.Unity.PhysicalHands
{
    public class PhysicalHandEvents : MonoBehaviour, IPhysicalHandHover, IPhysicalHandContact, IPhysicalHandGrab
    {
        [Space, Header("Hover Events"), Space]
        public UnityEvent<ContactHand> onHoverEnter;
        public UnityEvent<ContactHand> onHover;
        public UnityEvent<ContactHand> onHoverExit;

        [Space, Header("Contact Events"), Space]
        public UnityEvent<ContactHand> onContactEnter;
        public UnityEvent<ContactHand> onContact;
        public UnityEvent<ContactHand> onContactExit;

        [Space, Header("Grab Events"), Space]
        public UnityEvent<ContactHand> onGrabEnter;
        public UnityEvent<ContactHand> onGrab;
        public UnityEvent<ContactHand> onGrabExit;

        //

        [Space, Header("Left Hover Events"), Space]
        public UnityEvent<ContactHand> onLeftHandHoverEnter;
        public UnityEvent<ContactHand> onLeftHandHover;
        public UnityEvent<ContactHand> onLeftHandHoverExit;

        [Space, Header("Left Contact Events"), Space]
        public UnityEvent<ContactHand> onLeftHandContactEnter;
        public UnityEvent<ContactHand> onLeftHandContact;
        public UnityEvent<ContactHand> onLeftHandContactExit;

        [Space, Header("Left Grab Events"), Space]
        public UnityEvent<ContactHand> onLeftHandGrabEnter;
        public UnityEvent<ContactHand> onLeftHandGrab;
        public UnityEvent<ContactHand> onLeftHandGrabExit;

        //

        [Space, Header("Right Hover Events"), Space]
        public UnityEvent<ContactHand> onRightHandHoverEnter;
        public UnityEvent<ContactHand> onRightHandHover;
        public UnityEvent<ContactHand> onRightHandHoverExit;

        [Space, Header("Right Contact Events"), Space]
        public UnityEvent<ContactHand> onRightHandContactEnter;
        public UnityEvent<ContactHand> onRightHandContact;
        public UnityEvent<ContactHand> onRightHandContactExit;

        [Space, Header("Right Grab Events"), Space]
        public UnityEvent<ContactHand> onRightHandGrabEnter;
        public UnityEvent<ContactHand> onRightHandGrab;
        public UnityEvent<ContactHand> onRightHandGrabExit;

        public bool leftHandHovering { get; private set; }
        public bool leftHandContacting { get; private set; }
        public bool leftHandGrabbing { get; private set; }

        public bool rightHandHovering { get; private set; }
        public bool rightHandContacting { get; private set; }
        public bool rightHandGrabbing { get; private set; }

        public void OnHandHover(ContactHand hand)
        {
            if(!leftHandHovering && !rightHandHovering)
                onHoverEnter?.Invoke(hand);

            onHover?.Invoke(hand);

            if (hand.Handedness == Chirality.Left)
            {
                if (!leftHandHovering)
                    onLeftHandHoverEnter?.Invoke(hand);

                onLeftHandHover?.Invoke(hand);
                leftHandHovering = true;
            }
            else
            {
                if (!rightHandHovering)
                    onRightHandHoverEnter?.Invoke(hand);

                onRightHandHover?.Invoke(hand);
                rightHandHovering = true;
            }
        }

        public void OnHandHoverExit(ContactHand hand)
        {
            onHoverExit?.Invoke(hand);

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
        }

        public void OnHandContact(ContactHand hand)
        {
            if (!leftHandContacting && !rightHandContacting)
                onContactEnter?.Invoke(hand);

            onContact?.Invoke(hand);

            if (hand.Handedness == Chirality.Left)
            {
                if (!leftHandContacting)
                    onLeftHandContactEnter?.Invoke(hand);

                onLeftHandContact?.Invoke(hand);
            }
            else
            {
                if (!rightHandContacting)
                    onRightHandContactEnter?.Invoke(hand);

                onRightHandContact?.Invoke(hand);
            }
        }

        public void OnHandContactExit(ContactHand hand)
        {
            onContactExit?.Invoke(hand);

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
        }

        public void OnHandGrab(ContactHand hand)
        {
            if (!leftHandGrabbing && !rightHandGrabbing)
                onGrabEnter?.Invoke(hand);

            onGrab?.Invoke(hand);

            if (hand.Handedness == Chirality.Left)
            {
                if (!leftHandGrabbing)
                    onLeftHandGrabEnter?.Invoke(hand);

                onLeftHandGrab?.Invoke(hand);
            }
            else
            {
                if (!rightHandGrabbing)
                    onRightHandGrabEnter?.Invoke(hand);

                onRightHandGrab?.Invoke(hand);
            }
        }

        public void OnHandGrabExit(ContactHand hand)
        {
            onGrabExit?.Invoke(hand);

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
        }
    }
}