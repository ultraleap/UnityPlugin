/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Leap.PhysicalHands
{
    public class PhysicalHandsButtonHelper : MonoBehaviour, IPhysicalHandContact, IPhysicalHandHover, IPhysicalHandPrimaryHover
    {

        internal Action<ContactHand> _onHandContact;
        internal Action<ContactHand> _onHandContactExit;
        internal Action<ContactHand> _onHandHover;
        internal Action<ContactHand> _onHandHoverExit;
        internal Action<ContactHand> _onHandPrimaryHover;
        internal Action<ContactHand> _onHandPrimaryHoverExit;
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

        public void OnHandPrimaryHover(ContactHand hand)
        {
            _onHandPrimaryHover?.Invoke(hand);
        }

        public void OnHandPrimaryHoverExit(ContactHand hand)
        {
            _onHandPrimaryHoverExit?.Invoke(hand);
        }
    }
}