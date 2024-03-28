using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Leap.Unity.PhysicalHands
{
    public class PhysicalHandsButtonHelper : MonoBehaviour, IPhysicalHandContact, IPhysicalHandHover
    {

        internal Action<ContactHand> _onHandContact;
        internal Action<ContactHand> _onHandContactExit;
        internal Action<ContactHand> _onHandHover;
        internal Action<ContactHand> _onHandHoverExit;
        internal Action<Collision> _onCollisionEnter;
        internal Action<Collision> _onCollisionExit;

        public void OnHandContact(ContactHand hand)
        {
            _onHandContact?.Invoke(hand);
        }

        public void OnHandContactExit(ContactHand hand)
        {
            _onHandContactExit?.Invoke(hand);
        }

        public void OnHandHover(ContactHand hand)
        {
            _onHandHover?.Invoke(hand);
        }

        public void OnHandHoverExit(ContactHand hand)
        {
            _onHandHoverExit?.Invoke(hand);
        }

        private void OnCollisionEnter(Collision collision)
        {
            _onCollisionEnter?.Invoke(collision);
        }

        private void OnCollisionExit(Collision collision)
        {
            _onCollisionExit?.Invoke(collision);
        }
    }
}
