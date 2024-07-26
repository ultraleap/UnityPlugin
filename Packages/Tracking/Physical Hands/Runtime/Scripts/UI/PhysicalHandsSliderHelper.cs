/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using UnityEngine;


namespace Leap.PhysicalHands
{
    /// <summary>
    /// This class is purely to get events from the slideable object.
    /// You do not need to add this class to your object, it will be added automatically by the slider.
    /// </summary>
    public class PhysicalHandsSliderHelper : MonoBehaviour, IPhysicalHandGrab, IPhysicalHandContact
    {
        public Action<ContactHand> _onHandGrab;
        public Action<ContactHand> _onHandGrabExit;
        public Action<ContactHand> _onHandContact;
        public Action<ContactHand> _onHandContactExit;

        public void OnHandContact(ContactHand hand)
        {
            _onHandContact.Invoke(hand);
        }

        public void OnHandContactExit(ContactHand hand)
        {
            _onHandContactExit.Invoke(hand);
        }

        public void OnHandGrab(ContactHand hand)
        {
            _onHandGrab.Invoke(hand);
        }

        public void OnHandGrabExit(ContactHand hand)
        {
            _onHandGrabExit.Invoke(hand);
        }
    }
}